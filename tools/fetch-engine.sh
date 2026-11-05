#!/usr/bin/env bash
# Scalio — görsel büyütme motorunu indirir (macOS / Linux).
#
# fetch-engine.ps1'in (Windows) karşılığı. Kaynak yine xinntao/Real-ESRGAN
# v0.2.5.0 resmî sürüm paketi; ikili dosyalar repoya KONMAZ. Paket SHA-256 ile
# doğrulanır, özet tutmazsa derleme durur — değiştirilmiş bir ikiliyi sessizce
# kullanıcılara dağıtmaktansa hiç derlememek iyidir.
#
# Özetler 2026-11-05'te resmî sürümden indirilip ölçüldü (GitHub bu sürüm için
# özet yayımlamıyor; boyutlar API'deki boyutlarla aynı).
#
# Pakete yalnız motor ve iki x4 modeli girer; `realesr-animevideov3` modelleri
# ve örnek dosyalar kullanılmıyor.
#
# ⚠️ macOS ikilisi UNIVERSAL (arm64 + x86_64) ve MoltenVK'yı içine gömülü
# taşıyor: kullanıcıdan ayrıca Vulkan kurulumu istenmez. Linux ikilisi yalnız
# x86_64; Vulkan'ı çalışma anında sistemden yükler, `libgomp1` ister.
#
# Kullanım: tools/fetch-engine.sh <macos|linux> <hedef klasör>
#   Hedefte: realesrgan-ncnn-vulkan, models/, lisans metinleri.

set -euo pipefail

platform="${1:?platform: macos | linux}"
destination="${2:?hedef klasör}"

case "$platform" in
    macos)
        name='realesrgan-ncnn-vulkan-20220424-macos.zip'
        expected='e0ad05580abfeb25f8d8fb55aaf7bedf552c375b5b4d9bd3c8d59764d2cc333a'
        ;;
    linux)
        name='realesrgan-ncnn-vulkan-20220424-ubuntu.zip'
        expected='e5aa6eb131234b87c0c51f82b89390f5e3e642b7b70f2b9bbe95b6a285a40c96'
        ;;
    *)
        echo "Bilinmeyen platform: $platform (macos | linux)" >&2
        exit 2
        ;;
esac

url="https://github.com/xinntao/Real-ESRGAN/releases/download/v0.2.5.0/$name"
root="$(cd "$(dirname "$0")/.." && pwd)"
cache="${XDG_CACHE_HOME:-$HOME/.cache}/scalio-build"
zip="$cache/$name"

sha256() {
    if command -v sha256sum >/dev/null; then sha256sum "$1" | cut -d' ' -f1; else shasum -a 256 "$1" | cut -d' ' -f1; fi
}

mkdir -p "$cache"

if [[ ! -f "$zip" || "$(sha256 "$zip")" != "$expected" ]]; then
    echo 'Motor paketi indiriliyor…'
    curl -fsSL --retry 3 -o "$zip" "$url"
fi

actual="$(sha256 "$zip")"

if [[ "$actual" != "$expected" ]]; then
    rm -f "$zip"
    echo "Motor paketinin SHA-256 özeti tutmuyor. Beklenen $expected, gelen $actual." >&2
    exit 1
fi

extract="$(mktemp -d)"
trap 'rm -rf "$extract"' EXIT

unzip -q "$zip" -d "$extract"

mkdir -p "$destination/models"

cp "$extract/realesrgan-ncnn-vulkan" "$destination/"
chmod 755 "$destination/realesrgan-ncnn-vulkan"

for model in realesrgan-x4plus realesrgan-x4plus-anime; do
    cp "$extract/models/$model.bin" "$extract/models/$model.param" "$destination/models/"
done

# Lisans metinleri motorla BİRLİKTE dağıtılır (BSD-3 / MIT şartı).
cp "$root"/engine-licenses/* "$destination/"

echo "Motor hazır      : $destination"
