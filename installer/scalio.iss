; Scalio kurulum dosyası (Inno Setup 6).
;
; ⚠️ NEDEN KURULUM VAR: taşınabilir tek .exe, Kaspersky tarafından virüs
; sayılıp SİLİNİYORDU. Sebebi ölçüldü — .NET tek dosya paketi yerel DLL'leri
; %TEMP% altına çıkarıp çalıştırıyor ve bu, sezgisel taramada "dropper"
; davranışının ta kendisi. Artık uygulama normal bir program gibi Program
; Files'a kuruluyor; %TEMP%'e hiçbir şey çıkarılmıyor.
;
; ⚠️ SESSİZ/TEK TIK KURULUM YOK. Eski NativePHP sürümü `oneClick: true` ile
; derleniyordu ve kurulum penceresi UYARISIZ KAPANIYORDU — kullanıcı kurulumun
; bitip bitmediğini anlamıyordu, bildirdiği kusurlardan biri buydu. Burada
; normal bir sihirbaz var ve BİTİŞ SAYFASI kaldırılmadı.

#ifndef AppVersion
  #define AppVersion "0.2.0"
#endif

#define AppName "Scalio"
#define AppPublisher "Talivio Technology"
#define AppUrl "https://scalio.talivio.com"
#define AppExe "Scalio.exe"

[Setup]
; ⚠️ AppId SABİT ve DEĞİŞTİRİLMEZ: Windows yükseltmeleri ve kaldırma kaydı
; bu kimliğe bağlı. Değişirse her sürüm ayrı bir program olarak görünür ve
; kullanıcıda yan yana iki Scalio birikir.
AppId={{28087688-645D-40AA-9214-C350CA80F6AE}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppUrl}
AppSupportURL={#AppUrl}
AppUpdatesURL={#AppUrl}
VersionInfoVersion={#AppVersion}
VersionInfoCompany={#AppPublisher}
VersionInfoDescription=Scalio kurulumu

DefaultDirName={autopf}\Talivio\{#AppName}
; UYARI: UsePreviousAppDir VARSAYILANI 'yes'. Program Files\{AppName}'e kurulu
; ESKİ bir sürüm varsa, sessiz yükseltme DefaultDirName'i YOK SAYIP eski yola
; kurardı — Talivio klasörüne taşıma hiç gerçekleşmezdi (gerçek kurulumla
; yakalandı: kurulum logu hâlâ eski "Program Files\Salvio" yolunu gösterdi).
UsePreviousAppDir=no
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes

; ⚠️ Program Files'a kurulum yönetici ister. Bu, kurulum sırasında BİR KEZ
; sorulur; uygulamanın kendisi normal yetkiyle çalışır (bkz. app.manifest).
PrivilegesRequired=admin

; Yalnızca 64-bit: uygulama win-x64 olarak yayınlanıyor.
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

OutputDir=..\dist
OutputBaseFilename={#AppName}-{#AppVersion}-setup
SetupIconFile=..\src\Scalio.App\scalio.ico
UninstallDisplayIcon={app}\{#AppExe}
UninstallDisplayName={#AppName}

; ⚠️ LZMA2/max: paket ~80 MB'lık bir .NET uygulaması taşıyor. Sıkıştırmayı
; kısmak indirmeyi 30 MB kadar büyütüyor ve bu, felaket anında indiren
; kullanıcının beklediği süre demek.
Compression=lzma2/max
SolidCompression=yes

WizardStyle=modern
DisableWelcomePage=no
ShowLanguageDialog=auto

; ⚠️ Uygulama çalışıyorken kurulum yapılamaz: dosyaların üzerine yazmak
; çalışan bir büyütme işini yarıda kesebilirdi. Mutex adı uygulamadaki
; tek-kopya kilidiyle AYNI (bkz. SingleInstance).
AppMutex=Global\Scalio.SingleInstance

[Languages]
; ⚠️ ESTONCA YOK ve bu bilinçli: Inno Setup 6 ile RESMÎ bir Estonca çeviri
; gelmiyor. Üçüncü taraf bir dil dosyasını indirip kurulum programına gömmek,
; tam da antivirüs sürtünmesini azaltmaya çalıştığımız bir üründe denetlemediğimiz
; bir metni pakete koymak olurdu. Estonca kullanıcı kurulumu İngilizce görür;
; UYGULAMANIN KENDİSİ üç dilde (bkz. ui/i18n.js) ve varsayılan zaten İngilizce.
Name: "en"; MessagesFile: "compiler:Default.isl"
Name: "tr"; MessagesFile: "compiler:Languages\Turkish.isl"

[CustomMessages]
en.DesktopIcon=Create a desktop shortcut
tr.DesktopIcon=Masaüstü kısayolu oluştur
en.LaunchApp=Launch Scalio
tr.LaunchApp=Scalio'yu başlat
en.WebView2Missing=Scalio needs the Microsoft Edge WebView2 runtime to draw its interface, and it does not appear to be installed on this computer.%n%nSetup will continue. If the Scalio window opens empty, install "Microsoft Edge WebView2 Runtime" from microsoft.com and start Scalio again.
tr.WebView2Missing=Scalio arayüzünü çizmek için Microsoft Edge WebView2 çalışma zamanını kullanıyor ve bu bilgisayarda kurulu görünmüyor.%n%nKuruluma devam edilecek. Scalio penceresi boş açılırsa, microsoft.com adresinden "Microsoft Edge WebView2 Runtime" kurup Scalio'yu yeniden başlatın.

[Tasks]
; ⚠️ Masaüstü kısayolu VARSAYILAN OLARAK KAPALI: kullanıcının masaüstüne
; sormadan simge koymak, ücretsiz yazılımların en çok şikâyet edilen huyu.
Name: "desktopicon"; Description: "{cm:DesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "..\dist\app\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExe}"
Name: "{group}\{cm:UninstallProgram,{#AppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Run]
; ⚠️ `nowait postinstall` — kurulum bitiş sayfasında bir ONAY KUTUSU olarak
; sunulur, otomatik çalıştırılmaz. Kullanıcı kurulumun bittiğini görmeli;
; eski sürümün sessizce kapanıp uygulamayı da açmaması tam olarak bu yüzden
; şikâyet konusuydu.
;
; ⚠️ `runasoriginaluser`: kurulum yönetici olarak çalışıyor ama UYGULAMA
; normal kullanıcı olarak açılmalı. Aksi hâlde ilk oturum yönetici bağlamında
; açılır ve jetonlar (DPAPI, kullanıcıya bağlı) yanlış profile yazılırdı —
; sonraki normal açılışta kullanıcı oturumunu bulamazdı.
Filename: "{app}\{#AppExe}"; Description: "{cm:LaunchApp}"; Flags: nowait postinstall skipifsilent runasoriginaluser

; ⚠️ UYGULAMA İÇİ GÜNCELLEME (sessiz kurulum): uygulama kendini kapatıp bu
; programı /SILENT ile başlatıyor; bitince Scalio'nun KENDİLİĞİNDEN geri
; gelmesi gerekiyor — yoksa kullanıcı "güncelledim, uygulama kayboldu" görür.
; Yalnız sessiz kipte çalışır (yukarıdaki satır sessiz kipte atlanıyor);
; `--updated` argümanıyla açılır ki uygulama "güncellendi" diyebilsin.
Filename: "{app}\{#AppExe}"; Parameters: "--updated"; Flags: nowait runasoriginaluser; Check: WizardSilent

[UninstallDelete]
; Kullanıcı verisi (oturum, abonelik önbelleği, WebView2 profili) kaldırma
; sırasında SİLİNMEZ: yeniden kurulumda kullanıcı yeniden giriş yapmak
; zorunda kalmasın. Yalnız kurulum klasörü gider.
Type: filesandordirs; Name: "{app}"

[Code]
{
  ⚠️ WEBVIEW2 ÇALIŞMA ZAMANI DENETİMİ.

  Arayüz WebView2 içinde çalışıyor. Çalışma zamanı yoksa uygulama açılıyor ama
  PENCERE BOŞ kalıyordu — kullanıcı için "kurdum, çalışmıyor" demek ve sebebi
  hiçbir yerde görünmüyor. Windows 11'de ve güncel Windows 10'da kurulu gelir,
  ama yeni kurulmuş ya da LTSC/Server sürümlerinde gelmeyebilir. Bu araç tam da
  öyle makinelerde açılıyor.

  ⚠️ ÇALIŞMA ZAMANI İNDİRİLMEZ, YALNIZCA UYARILIR. Kurulum programına indirici
  koymak, bu üründe bedeli ölçülmüş bir hata: PowerShell ile exe indiren .bat
  köprüsü Kaspersky tarafından Trojan-Downloader olarak işaretlenmişti ve
  kaldırıldı. Aynı davranışı kurulum programına taşımak, imzasız bir pakette
  aynı sezgisel kuralı yeniden tetiklerdi.

  ⚠️ Kurulum ENGELLENMEZ: kullanıcı yine de kurabilmeli (çalışma zamanını sonra
  kurabilir, ya da kontrol yanılmış olabilir). Amaç bilgilendirmek.
}
function WebView2Installed(): Boolean;
var
  Value: String;
begin
  Result :=
    RegQueryStringValue(HKLM, 'SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}', 'pv', Value) or
    RegQueryStringValue(HKLM, 'SOFTWARE\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}', 'pv', Value) or
    RegQueryStringValue(HKCU, 'SOFTWARE\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}', 'pv', Value);

  { Boş bir sürüm dizgisi "kurulu" sayılmaz. }
  if Result and (Value = '') then
    Result := False;
end;

{
  ⚠️ ESKİ SÜRÜMÜ KALDIRMA.

  Bu kurulum dosya kümesini kurulum klasörüne `ignoreversion` ile YAZAR — eski
  sürümde olup yeni sürümde OLMAYAN bir dosya (silinen/adı değişen bir DLL,
  eski bir model dosyası vb.) diskte KALIR. Normalde bu zararsızdır, ama
  motor entegrasyonu ilerledikçe dosya seti değişecek ve unutulmuş bir eski
  DLL, yeni sürümle çakışıp tuhaf bir hataya yol açabilir. En güvenlisi:
  yeni kurulum başlamadan önce eskisini TAMAMEN kaldırmak.

  ⚠️ AppId SABİT olduğu için Windows bunu zaten "aynı program" sayıp normal
  şartlarda üstüne yazardı; burada ekstra bir SESSİZ kaldırma adımı ekliyoruz
  ki dosya kümesi değiştiğinde artık dosya kalmasın. Kullanıcı hiçbir şey
  görmez (`/VERYSILENT /SUPPRESSMSGBOXES`), kullanıcı verisi (oturum,
  abonelik önbelleği) [UninstallDelete] kapsamı dışında olduğu için silinmez.
}
{
  ⚠️ GUID BURADA YAZILI (AppId satırıyla AYNI olmalı): SetupSetting'AppId'
  ini'deki değeri kaçış karakterleriyle birebir döndürüyor, kayıt yoluna
  gömülürse fazladan bir süslü parantez eklenmiş olurdu. Bu yüzden düz metin.
}
function GetUninstallString(): String;
var
  Value: String;
  UninstallKey: String;
begin
  Result := '';
  UninstallKey := '{28087688-645D-40AA-9214-C350CA80F6AE}_is1';

  {
    UYARI: HKLM64 VE HKLM32, DUZ HKLM DEGIL. [Code] icinde duz HKLM 32-bit
    goruntuye dusuyor; kurulum 64-bit kipte calistigi icin kaydini 64-bit
    goruntuye yaziyor. Duz HKLM ile arandiginda eski surum HIC BULUNAMIYOR ve
    kaldirma adimi sessizce atlaniyordu (kurulum logu ile dogrulandi).
  }
  if RegQueryStringValue(HKLM64, 'SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\' + UninstallKey, 'UninstallString', Value) then
    Result := Value
  else if RegQueryStringValue(HKLM32, 'SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\' + UninstallKey, 'UninstallString', Value) then
    Result := Value;
end;

procedure UninstallPreviousVersion();
var
  UninstallString: String;
  ResultCode: Integer;
  Waited: Integer;
begin
  UninstallString := GetUninstallString();

  { İlk kurulumda kayıt yok — sessizce atlanır, hata verilmez. }
  if UninstallString = '' then
    Exit;

  UninstallString := RemoveQuotes(UninstallString);

  if not FileExists(UninstallString) then
    Exit;

  if Exec(UninstallString, '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
  begin
    {
      ⚠️ BEKLEMEK YETMİYOR, BİTİŞİ ÖLÇMEK GEREKİYOR. `unins000.exe` kendini
      %TEMP% altına kopyalayıp asıl kaldırmayı O kopyaya devrediyor ve hemen
      dönüyor; `ewWaitUntilTerminated` bu yüzden yalnız kopyalama adımını
      bekler. Yeni dosyalar, eski kaldırıcı hâlâ silerken kopyalanırsa
      kaldırıcı YENİ dosyaları da siler. Gerçek bitişin ölçütü kaldırıcının
      kendini diskten silmesidir; en fazla 60 sn beklenir, sonra kurulum yine
      de sürer (kilitlenmeyi tercih etmeyiz).
    }
    Waited := 0;

    while FileExists(UninstallString) and (Waited < 120) do
    begin
      Sleep(500);
      Waited := Waited + 1;
    end;
  end;
end;

function InitializeSetup(): Boolean;
begin
  Result := True;

  if not WebView2Installed() then
    MsgBox(CustomMessage('WebView2Missing'), mbInformation, MB_OK);
end;

{
  ⚠️ KALDIRMA `InitializeSetup`'ta DEĞİL, `ssInstall`'da: ilkinde sihirbaz
  daha ekrana gelmeden çalışırdı ve kullanıcı "İptal"e bassa bile eski sürüm
  çoktan silinmiş olurdu — çalışan bir kurulumu, hiçbir şey kurmadan yok
  etmek demekti. Burada kullanıcı "Kur"a bastıktan SONRA, dosyalar
  kopyalanmadan hemen önce çalışır.
}
procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssInstall then
    UninstallPreviousVersion();
end;
