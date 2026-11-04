# Scalio — görsel büyütme motorunu indirir ve paket klasörüne hazırlar.
#
# Kaynak: xinntao/Real-ESRGAN v0.2.5.0 resmî sürüm paketi. İkili dosyalar
# repoya KONMAZ (~50 MB); her yayında bu betik resmî paketi indirir ve
# SHA-256 ile doğrular. Özet tutmazsa derleme durur — değiştirilmiş bir
# ikiliyi sessizce kullanıcılara dağıtmaktansa hiç derlememek iyidir.
#
# Pakete yalnız gerekenler girer: motor, OpenMP çalışma zamanı (vcomp140.dll)
# ve iki x4 modeli. `vcomp140d.dll` (hata ayıklama sürümü) yeniden
# dağıtılamaz, `realesr-animevideov3` modelleri ve örnek dosyalar kullanılmıyor.
#
# Kullanım: ./tools/fetch-engine.ps1 -Destination <klasör>

param(
    [Parameter(Mandatory = $true)]
    [string] $Destination
)

$ErrorActionPreference = 'Stop'

$url = 'https://github.com/xinntao/Real-ESRGAN/releases/download/v0.2.5.0/realesrgan-ncnn-vulkan-20220424-windows.zip'
$expectedSha256 = 'abc02804e17982a3be33675e4d471e91ea374e65b70167abc09e31acb412802d'

$files = @(
    'realesrgan-ncnn-vulkan.exe',
    'vcomp140.dll',
    'models/realesrgan-x4plus.bin',
    'models/realesrgan-x4plus.param',
    'models/realesrgan-x4plus-anime.bin',
    'models/realesrgan-x4plus-anime.param'
)

$cacheDir = Join-Path $env:LOCALAPPDATA 'Scalio\build-cache'
$zip = Join-Path $cacheDir 'realesrgan-ncnn-vulkan-20220424-windows.zip'

New-Item -ItemType Directory -Force $cacheDir | Out-Null

if (-not (Test-Path $zip) -or (Get-FileHash $zip -Algorithm SHA256).Hash.ToLower() -ne $expectedSha256) {
    Write-Host 'Motor paketi indiriliyor…' -ForegroundColor Cyan
    Invoke-WebRequest -Uri $url -OutFile $zip -UseBasicParsing
}

$actual = (Get-FileHash $zip -Algorithm SHA256).Hash.ToLower()

if ($actual -ne $expectedSha256) {
    Remove-Item $zip -Force
    throw "Motor paketinin SHA-256 özeti tutmuyor. Beklenen $expectedSha256, gelen $actual."
}

$extract = Join-Path $cacheDir 'extract'

if (Test-Path $extract) {
    Remove-Item $extract -Recurse -Force
}

Expand-Archive -Path $zip -DestinationPath $extract

New-Item -ItemType Directory -Force (Join-Path $Destination 'models') | Out-Null

foreach ($file in $files) {
    Copy-Item (Join-Path $extract $file) (Join-Path $Destination $file) -Force
}

# Lisans metinleri motorla BİRLİKTE dağıtılır (BSD-3 / MIT şartı).
Copy-Item (Join-Path $PSScriptRoot '..\engine-licenses\*') (Join-Path $Destination '') -Force

Write-Host ('Motor hazır      : ' + $Destination) -ForegroundColor Green
