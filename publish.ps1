# Scalio — yayınlama ve kurulum dosyası üretimi.
#
# ═══════════════════════════════════════════════════════════════════════════
# ⚠️ TEK DOSYA YAYIN BIRAKILDI — SEBEBİ ÖLÇÜLDÜ
# ═══════════════════════════════════════════════════════════════════════════
# Önce taşınabilir tek bir .exe üretiyorduk. Gerekçe iyiydi (kurtarılacak
# diske program kurmamak) ama BEDELİ ÜRÜNÜ ÖLDÜRÜYORDU: Kaspersky dosyayı
# virüs sayıp SİLİYOR.
#
# Sebep tahmin değil, ölçüm: .NET'in tek dosya paketi ilk çalıştırmada yerel
# DLL'leri `%TEMP%\.net\Scalio\<derleme-özeti>\` altına ÇIKARIP oradan
# yüklüyor. Makinede beş ayrı çıkarma dizini ve 35 DLL bulundu. Bu, sezgisel
# taramada ders kitabı "dropper" davranışıdır: imzasız büyük bir ikili,
# %TEMP%'e çalıştırılabilir dosya yazıyor ve onları çalıştırıyor. Üstüne
# Scalio ham disk açıyor ve kendini yükseltiyor — hepsi meşru, ama birlikte
# bir motoru ikna etmek için fazlasıyla yeterli.
#
# Artık NORMAL BİR PROGRAM gibi yayınlanıyor: klasör hâlinde dosyalar,
# Program Files'a kurulum, %TEMP%'e hiçbir şey çıkarılmıyor.
#
# ⚠️ Bu, imza ihtiyacını ORTADAN KALDIRMAZ. Tek gerçek çözüm hâlâ kod
# imzalama sertifikası; bu değişiklik yalnızca en güçlü sezgisel tetikleyiciyi
# kaldırıyor.

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$stage = Join-Path $root 'dist\app'
$output = Join-Path $root 'dist'

if (Test-Path $output) {
    Remove-Item $output -Recurse -Force
}

Write-Host 'Yayınlanıyor…' -ForegroundColor Cyan

# ⚠️ `PublishSingleFile` YOK ve olmayacak (yukarıdaki gerekçe).
# ⚠️ Bağımsız (self-contained): bu araç yeni kurulmuş, bozulmuş ya da
#    internetsiz bir makinede açılıyor; "önce .NET kurun" demek onu tam
#    ihtiyaç anında kullanılamaz kılar.
# ⚠️ ReadyToRun: soğuk açılış ölçülerek belirgin iyileşti (1360 → 1043 ms).
# ⚠️ Kırpma (trimming) YOK: WPF desteklemiyor.
dotnet publish (Join-Path $root 'src/Scalio.App/Scalio.App.csproj') `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishReadyToRun=true `
    -o $stage `
    --nologo

if ($LASTEXITCODE -ne 0) {
    throw 'Yayınlama başarısız.'
}

# ── Görsel büyütme motoru ─────────────────────────────────────────────────
# ⚠️ Motor olmadan paket AÇILIR ama hiçbir görseli büyütemez; bu yüzden
# indirilemez ya da doğrulanamazsa derleme burada durur.
& (Join-Path $root 'tools\fetch-engine.ps1') -Destination (Join-Path $stage 'engine')

if (-not (Test-Path (Join-Path $stage 'engine\realesrgan-ncnn-vulkan.exe'))) {
    throw 'Motor pakete eklenemedi.'
}

$exe = Join-Path $stage 'Scalio.exe'
$version =(Get-Item $exe).VersionInfo.ProductVersion -replace '\+.*$', ''

Write-Host ''
Write-Host ('Uygulama klasörü : {0:N1} MB, {1} dosya' -f
    ((Get-ChildItem $stage -Recurse -File | Measure-Object Length -Sum).Sum / 1MB),
    (Get-ChildItem $stage -Recurse -File).Count) -ForegroundColor Green
Write-Host ('Sürüm            : ' + $version)

# ── İmzalama ──────────────────────────────────────────────────────────────
#
# ⚠️ TEK GERÇEK ÇÖZÜM SERTİFİKADIR. Betikle, indiriciyle ya da başka bir
# kurnazlıkla dolanılmaz — denendi: PowerShell'le exe indiren bir .bat köprüsü
# Kaspersky tarafından Trojan-Downloader olarak işaretlendi.
#
# ⚠️ KENDİ İMZALI (self-signed) SERTİFİKA İŞE YARAMAZ: Windows tanımaz,
# uyarı aynen çıkar ve "imzalıyoruz" yanılsaması üretir.
#
# Sertifika alındığında: $env:SCALIO_SIGN_THUMBPRINT = '<parmak izi>'
$thumbprint = $env:SCALIO_SIGN_THUMBPRINT
$signtool = $null

if (-not [string]::IsNullOrWhiteSpace($thumbprint)) {
    $signtool = Get-ChildItem 'C:\Program Files (x86)\Windows Kits\10\bin\*\x64\signtool.exe' -ErrorAction SilentlyContinue |
        Sort-Object FullName -Descending | Select-Object -First 1

    if (-not $signtool) {
        throw 'signtool.exe bulunamadı (Windows SDK gerekli).'
    }

    Write-Host 'Uygulama imzalanıyor…' -ForegroundColor Cyan

    # ⚠️ Zaman damgası ŞART: damgasız imza, sertifikanın son kullanma
    # tarihinde çöpe döner.
    & $signtool.FullName sign /sha1 $thumbprint /fd SHA256 `
        /tr http://timestamp.digicert.com /td SHA256 /d 'Scalio' $exe

    if ($LASTEXITCODE -ne 0) { throw 'İmzalama başarısız.' }
}

# ── Kurulum dosyası ───────────────────────────────────────────────────────
# ⚠️ Kullanıcı başına kurulum da aranıyor: winget, Inno Setup'ı yönetici
# istemeden `%LOCALAPPDATA%\Programs` altına kuruyor ve yalnız Program Files'a
# bakan bir arama onu bulamıyordu.
$iscc = @(
    'C:\Program Files (x86)\Inno Setup 6\ISCC.exe',
    'C:\Program Files\Inno Setup 6\ISCC.exe',
    (Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 6\ISCC.exe')
) | Where-Object { Test-Path $_ } | Select-Object -First 1

if (-not $iscc) {
    Write-Host ''
    Write-Host 'Inno Setup bulunamadı; kurulum dosyası üretilmedi.' -ForegroundColor Yellow
    Write-Host 'Kurulum: winget install --id JRSoftware.InnoSetup' -ForegroundColor DarkGray

    return
}

Write-Host ''
Write-Host 'Kurulum dosyası üretiliyor…' -ForegroundColor Cyan

& $iscc "/DAppVersion=$version" (Join-Path $root 'installer\scalio.iss') | Out-Null

if ($LASTEXITCODE -ne 0) {
    throw 'Kurulum dosyası üretilemedi.'
}

$setup = Get-ChildItem (Join-Path $output '*.exe') | Where-Object { $_.Name -like 'Scalio-*-setup.exe' } | Select-Object -First 1

if ($signtool -and $setup) {
    # ⚠️ Kurulum dosyası da AYRICA imzalanır: kullanıcının çift tıkladığı
    # dosya bu; imzasız kalırsa SmartScreen uyarısı yine çıkar.
    Write-Host 'Kurulum dosyası imzalanıyor…' -ForegroundColor Cyan

    & $signtool.FullName sign /sha1 $thumbprint /fd SHA256 `
        /tr http://timestamp.digicert.com /td SHA256 /d 'Scalio Setup' $setup.FullName

    if ($LASTEXITCODE -ne 0) { throw 'Kurulum dosyası imzalanamadı.' }
}

Write-Host ''
Write-Host ('Kurulum dosyası : {0} ({1:N1} MB)' -f $setup.Name, ($setup.Length / 1MB)) -ForegroundColor Green
Write-Host ('SHA-256         : ' + (Get-FileHash $setup.FullName -Algorithm SHA256).Hash.ToLower())

if ([string]::IsNullOrWhiteSpace($thumbprint)) {
    Write-Host ''
    Write-Host 'UYARI: İMZASIZ. SmartScreen uyarısı ve antivirüs sürtünmesi beklenir.' -ForegroundColor Yellow
}
