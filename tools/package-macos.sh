#!/usr/bin/env bash
# Scalio — macOS paketi: universal Scalio.app ve .dmg.
#
# publish.ps1'in (Windows) macOS karşılığı. Yalnız macOS'ta çalışır (hdiutil,
# codesign, lipo, clang).
#
# Paket düzeni:
#   Scalio.app/Contents/
#     MacOS/Scalio                    universal başlatıcı (packaging/macos/launcher.c)
#     MacOS/arm64/Scalio (+ dylib)    .NET uygulaması, Apple Silicon
#     MacOS/x64/Scalio   (+ dylib)    .NET uygulaması, Intel
#     MacOS/realesrgan-ncnn-vulkan    motor (universal, MoltenVK gömülü)
#     Resources/engine/models/        modeller + lisanslar
#     Resources/Scalio.icns
#
# İmza (isteğe bağlı, ortam değişkenleriyle — değerler repoya YAZILMAZ):
#   SCALIO_MAC_SIGN_IDENTITY  Anahtar zincirindeki "Developer ID Application"
#                             kimliği. Yoksa paket ad-hoc imzalanır: bu Mac'te
#                             çalışır, başka Mac'te Gatekeeper açmaz.
#   SCALIO_NOTARY_PROFILE     `xcrun notarytool store-credentials` ile kaydedilmiş
#                             profil adı. Varsa dmg noter onayına gönderilir ve
#                             onay damgası (staple) basılır.
#
# Kullanım: tools/package-macos.sh

set -euo pipefail

root="$(cd "$(dirname "$0")/.." && pwd)"
dist="$root/dist"
work="$dist/macos"
app="$work/Scalio.app"
contents="$app/Contents"

version="$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' "$root/Directory.Build.props" | head -1)"
identity="${SCALIO_MAC_SIGN_IDENTITY:-}"

rm -rf "$work"
mkdir -p "$contents/MacOS" "$contents/Resources/engine"

echo "Scalio $version — macOS paketi"

# ── 1. .NET uygulaması, iki mimari ─────────────────────────────────────────
for rid in osx-arm64 osx-x64; do
    arch="${rid#osx-}"

    echo "Yayınlanıyor: $rid…"

    dotnet publish "$root/src/Scalio.Desktop/Scalio.Desktop.csproj" \
        -c Release -r "$rid" --self-contained true \
        -p:PublishSingleFile=true \
        -o "$contents/MacOS/$arch" --nologo -v quiet

    # Linux simgesi macOS paketinde gereksiz.
    rm -f "$contents/MacOS/$arch/scalio.png"
done

# ── 2. Universal başlatıcı ──────────────────────────────────────────────────
xcrun clang -O2 -arch arm64 -arch x86_64 -mmacosx-version-min=12.0 \
    -o "$contents/MacOS/Scalio" "$root/packaging/macos/launcher.c"

# ── 3. Motor ───────────────────────────────────────────────────────────────
engine="$work/engine"
"$root/tools/fetch-engine.sh" macos "$engine"

mv "$engine/realesrgan-ncnn-vulkan" "$contents/MacOS/"
cp -R "$engine/"* "$contents/Resources/engine/"

# ── 4. Info.plist ve simge ─────────────────────────────────────────────────
sed "s/__VERSION__/$version/g" "$root/packaging/macos/Info.plist" > "$contents/Info.plist"

iconset="$work/Scalio.iconset"
mkdir -p "$iconset"
for size in 16 32 128 256; do
    sips -z $size $size "$root/src/Scalio.Desktop/scalio.png" --out "$iconset/icon_${size}x${size}.png" >/dev/null
done
for size in 16 32 128; do
    double=$((size * 2))
    sips -z $double $double "$root/src/Scalio.Desktop/scalio.png" --out "$iconset/icon_${size}x${size}@2x.png" >/dev/null
done
iconutil -c icns "$iconset" -o "$contents/Resources/Scalio.icns"
rm -rf "$iconset"

# ── 5. İmza ────────────────────────────────────────────────────────────────
# ⚠️ İÇTEN DIŞA imzalanır: önce her ikili ve kütüphane, en son paketin
# kendisi. `--deep` kullanılmaz (Apple önermiyor; iç içe kodu yanlış
# seçeneklerle imzalayabiliyor).
if [[ -n "$identity" ]]; then
    sign=(codesign --force --timestamp --options runtime --sign "$identity")
    signApp=("${sign[@]}" --entitlements "$root/packaging/macos/Scalio.entitlements")
    echo 'İmzalanıyor (Developer ID)…'
else
    sign=(codesign --force --sign -)
    signApp=("${sign[@]}")
    echo 'İmza kimliği yok: ad-hoc imzalanıyor (yalnız bu Mac için).'
fi

find "$contents/MacOS" -name '*.dylib' -print0 | xargs -0 -n1 "${sign[@]}"
"${sign[@]}" "$contents/MacOS/realesrgan-ncnn-vulkan"
"${signApp[@]}" "$contents/MacOS/arm64/Scalio"
"${signApp[@]}" "$contents/MacOS/x64/Scalio"
"${signApp[@]}" "$app"

codesign --verify --strict --verbose=1 "$app"

# ── 6. dmg ─────────────────────────────────────────────────────────────────
dmg="$dist/Scalio-$version-macos.dmg"
stage="$work/dmg"
rm -f "$dmg"
mkdir -p "$stage"
cp -R "$app" "$stage/"
ln -s /Applications "$stage/Applications"

hdiutil create -quiet -volname "Scalio $version" -srcfolder "$stage" -fs HFS+ -format UDZO -imagekey zlib-level=9 "$dmg"
rm -rf "$stage"

if [[ -n "$identity" ]]; then
    "${sign[@]}" "$dmg"
fi

if [[ -n "${SCALIO_NOTARY_PROFILE:-}" ]]; then
    echo 'Noter onayına gönderiliyor (birkaç dakika sürebilir)…'
    xcrun notarytool submit "$dmg" --keychain-profile "$SCALIO_NOTARY_PROFILE" --wait
    # ⚠️ Onay reddedilirse damga basılamaz ve betik burada durur (set -e).
    xcrun stapler staple "$dmg"
    spctl -a -vv -t exec "$app"
fi

shasum -a 256 "$dmg" | sed "s:  .*/:  :" > "$dmg.sha256"

echo
echo "Paket       : $dmg ($(du -h "$dmg" | cut -f1))"
echo "SHA-256     : $(cut -d' ' -f1 "$dmg.sha256")"

if [[ -z "$identity" ]]; then
    echo
    echo "UYARI: İMZASIZ (ad-hoc). Başka bir Mac'te Gatekeeper 'doğrulanamadı' deyip açmaz;"
    echo 'kullanıcı Sistem Ayarları → Gizlilik ve Güvenlik → "Yine de Aç" yolunu izlemek zorunda kalır.'
elif [[ -z "${SCALIO_NOTARY_PROFILE:-}" ]]; then
    echo
    echo 'UYARI: İmzalı ama noter onaysız. Gatekeeper yine uyarır; SCALIO_NOTARY_PROFILE verin.'
fi
