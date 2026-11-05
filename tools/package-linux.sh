#!/usr/bin/env bash
# Scalio — Linux paketleri: .deb ve AppImage (yalnız x86_64).
#
# publish.ps1'in (Windows) Linux karşılığı. Linux'ta çalışır (dpkg-deb gerekir);
# CI'da ubuntu-22.04 üzerinde koşar (bkz. .github/workflows/release.yml).
#
# ⚠️ WebKitGTK SİSTEMDEN GELİR, pakete gömülmez (Photino, WKWebView'ın Linux
# karşılığı olarak onu kullanıyor). .deb bunu bağımlılık olarak bildirir ve apt
# kendisi kurar; AppImage'da kullanıcının sisteminde `libwebkit2gtk-4.1`
# olmalı (Ubuntu 22.04+, Debian 12+, Fedora 36+ depolarında var).
#
# ⚠️ PHOTINO_NATIVE_SO: NuGet'teki Photino.Native.so (4.0.22) glibc 2.38'e karşı
# derlenmiş — yani Ubuntu 24.04'ten eskide AÇILMAZ (22.04'te glibc 2.35).
# CI aynı sürümü kaynaktan ubuntu-22.04'te derleyip bu değişkenle verir; paket
# böylece 22.04 ve sonrasında çalışır. Değişken yoksa NuGet'teki kullanılır ve
# betik bunu uyarı olarak yazar.
#
# Kullanım: tools/package-linux.sh

set -euo pipefail

root="$(cd "$(dirname "$0")/.." && pwd)"
dist="$root/dist"
work="$dist/linux"
stage="$work/scalio"

version="$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' "$root/Directory.Build.props" | head -1)"

rm -rf "$work"
mkdir -p "$stage"

echo "Scalio $version — Linux paketleri"

# ── 1. Uygulama ve motor ───────────────────────────────────────────────────
dotnet publish "$root/src/Scalio.Desktop/Scalio.Desktop.csproj" \
    -c Release -r linux-x64 --self-contained true \
    -p:PublishSingleFile=true \
    -o "$stage" --nologo -v quiet

if [[ -n "${PHOTINO_NATIVE_SO:-}" ]]; then
    cp "$PHOTINO_NATIVE_SO" "$stage/Photino.Native.so"
    echo "Photino.Native.so: kaynaktan derlenmiş kopya kullanıldı."
else
    echo 'UYARI: Photino.Native.so NuGet sürümü — Ubuntu 24.04 / glibc 2.38 öncesinde açılmaz.'
fi

"$root/tools/fetch-engine.sh" linux "$stage/engine"

# ── 2. .deb ────────────────────────────────────────────────────────────────
deb="$work/deb"
mkdir -p "$deb/DEBIAN" "$deb/opt" "$deb/usr/bin" \
    "$deb/usr/share/applications" "$deb/usr/share/icons/hicolor/256x256/apps"

cp -R "$stage" "$deb/opt/scalio"
ln -s /opt/scalio/Scalio "$deb/usr/bin/scalio"
cp "$root/packaging/linux/scalio.desktop" "$deb/usr/share/applications/"
cp "$root/src/Scalio.Desktop/scalio.png" "$deb/usr/share/icons/hicolor/256x256/apps/scalio.png"

installed_kb="$(du -sk "$deb/opt" | cut -f1)"
sed -e "s/__VERSION__/$version/" -e "s/__SIZE__/$installed_kb/" \
    "$root/packaging/linux/control" > "$deb/DEBIAN/control"

deb_file="$dist/scalio_${version}_amd64.deb"
dpkg-deb --root-owner-group -Zxz --build "$deb" "$deb_file" >/dev/null

# ── 3. AppImage ────────────────────────────────────────────────────────────
# Araç ve çalışma zamanı SÜRÜMÜ SABİT ve SHA-256 ile doğrulanır (özetler
# GitHub'ın yayımladığı özetlerdir). appimagetool varsayılan olarak "son"
# çalışma zamanını internetten çeker; bu, aynı kaynaktan farklı paket demek.
tools="${XDG_CACHE_HOME:-$HOME/.cache}/scalio-build"
mkdir -p "$tools"

fetch() {
    local url="$1" file="$2" expected="$3"

    if [[ ! -f "$file" || "$(sha256sum "$file" | cut -d' ' -f1)" != "$expected" ]]; then
        curl -fsSL --retry 3 -o "$file" "$url"
    fi

    if [[ "$(sha256sum "$file" | cut -d' ' -f1)" != "$expected" ]]; then
        rm -f "$file"
        echo "SHA-256 tutmuyor: $url" >&2
        exit 1
    fi
}

appimagetool="$tools/appimagetool-1.9.1-x86_64.AppImage"
runtime="$tools/type2-runtime-20251108-x86_64"

fetch https://github.com/AppImage/appimagetool/releases/download/1.9.1/appimagetool-x86_64.AppImage \
    "$appimagetool" ed4ce84f0d9caff66f50bcca6ff6f35aae54ce8135408b3fa33abfc3cb384eb0
fetch https://github.com/AppImage/type2-runtime/releases/download/20251108/runtime-x86_64 \
    "$runtime" 2fca8b443c92510f1483a883f60061ad09b46b978b2631c807cd873a47ec260d
chmod +x "$appimagetool"

appdir="$work/Scalio.AppDir"
mkdir -p "$appdir/usr/lib"
cp -R "$stage" "$appdir/usr/lib/scalio"
cp "$root/packaging/linux/AppRun" "$appdir/AppRun"
chmod 755 "$appdir/AppRun"
cp "$root/packaging/linux/scalio.desktop" "$appdir/"
cp "$root/src/Scalio.Desktop/scalio.png" "$appdir/scalio.png"

appimage="$dist/Scalio-$version-x86_64.AppImage"

# FUSE'suz CI'da araç kendini açıp çalışır.
ARCH=x86_64 APPIMAGE_EXTRACT_AND_RUN=1 "$appimagetool" --no-appstream \
    --runtime-file "$runtime" "$appdir" "$appimage" >/dev/null

# ── 4. Özetler ─────────────────────────────────────────────────────────────
for file in "$deb_file" "$appimage"; do
    (cd "$dist" && sha256sum "$(basename "$file")" > "$(basename "$file").sha256")
    echo "Paket   : $(basename "$file") ($(du -h "$file" | cut -f1))"
    echo "SHA-256 : $(cut -d' ' -f1 "$file.sha256")"
done
