namespace Scalio.Core.Localization;

/// <summary>
/// Kullanıcıya görünen çekirdek metinleri.
///
/// ⚠️ NEDEN BURADA: motorun fırlattığı istisna mesajları doğrudan kullanıcıya
/// gösteriliyor ("kaynak diske yazılamaz", "kaynak veri erişilebilir değil").
/// Arayüzün tamamı çevrilip bu cümleler Türkçe kalsaydı, İngilizce arayüzdeki
/// kullanıcı hata anında — yani en kötü anda — anlamadığı bir dille
/// karşılaşırdı. Bu, PHP sürümünde mobil tarafta GERÇEKTEN yaşandı: aynı kural
/// bir yerde Türkçe, diğerinde İngilizce cevap veriyordu.
///
/// ⚠️ `.resx` KULLANILMIYOR: uydu derlemeleri (satellite assemblies) yayın
/// klasörüne ayrı `.dll`'ler koyar ve ürünün "tek dosya" olma kararını bozar.
/// Sözlük doğrudan koda gömülü.
/// </summary>
public static class Strings
{
    /// <summary>Desteklenen diller. Sıra arayüzdeki seçicide de aynı.</summary>
    public static readonly string[] Supported = ["en", "tr", "et"];

    private static string _language = "en";

    /// <summary>
    /// Etkin dil. Arayüz açılışta bunu bildiriyor.
    ///
    /// ⚠️ Varsayılan İNGİLİZCE: ürün euro fiyatla ve İngilizce ağırlıklı bir
    /// siteyle satılıyor. Türkçe varsayılan, tanımadığımız bir kullanıcıya
    /// anlamadığı bir dille açılmak demek olurdu.
    /// </summary>
    public static string Language
    {
        get => _language;
        set => _language = Supported.Contains(value) ? value : "en";
    }

    public static string Get(string key)
    {
        if (Table.TryGetValue(key, out var translations) && translations.TryGetValue(_language, out var text))
        {
            return text;
        }

        // Çeviri eksikse İngilizceye düşülür; anahtarın kendisini göstermek
        // kullanıcıya hiçbir şey anlatmaz.
        return Table.TryGetValue(key, out var fallback) && fallback.TryGetValue("en", out var english)
            ? english
            : key;
    }

    public static string Get(string key, params object[] arguments)
        => string.Format(Get(key), arguments);

    private static readonly Dictionary<string, Dictionary<string, string>> Table = new()
    {
        ["destination.same_volume"] = new()
        {
            ["en"] = "Recovered files cannot be written to the source drive: writing to the same volume "
                     + "risks overwriting files that have not been recovered yet. Please choose another "
                     + "drive or an external disk.",
            ["tr"] = "Kurtarılan dosyalar kaynak diske yazılamaz: aynı birime yazmak, henüz kurtarılmamış "
                     + "dosyaların üzerine yazma riski taşır. Lütfen başka bir disk ya da harici sürücü seçin.",
            ["et"] = "Taastatud faile ei saa kirjutada lähtekettale: samale kettale kirjutamine võib üle "
                     + "kirjutada faile, mida pole veel taastatud. Palun valige teine ketas või väline draiv.",
        },

        ["source.gone"] = new()
        {
            ["en"] = "The source data is no longer accessible: {0}",
            ["tr"] = "Kaynak veri artık erişilebilir değil: {0}",
            ["et"] = "Lähteandmed pole enam kättesaadavad: {0}",
        },

        ["source.unknown_location"] = new()
        {
            ["en"] = "This candidate's location on disk is unknown: {0}",
            ["tr"] = "Bu adayın disk üzerindeki yeri bilinmiyor: {0}",
            ["et"] = "Selle kandidaadi asukoht kettal on teadmata: {0}",
        },

        ["volume.needs_elevation"] = new()
        {
            ["en"] = "The volume could not be opened for raw reading ({0}:). Deep scan requires administrator rights.",
            ["tr"] = "Birim ham olarak açılamadı ({0}:). Derin tarama yönetici yetkisi ister.",
            ["et"] = "Ketast ei õnnestunud toorlugemiseks avada ({0}:). Süvaskaneerimine vajab administraatoriõigusi.",
        },

        ["volume.windows_only"] = new()
        {
            ["en"] = "Raw volume reading is supported on Windows only.",
            ["tr"] = "Ham birim okuma yalnızca Windows üzerinde desteklenir.",
            ["et"] = "Toorketta lugemine on toetatud ainult Windowsis.",
        },

        ["ntfs.not_ntfs"] = new()
        {
            ["en"] = "This drive is not NTFS, so deep scan cannot run on it.",
            ["tr"] = "Bu disk NTFS değil, bu yüzden derin tarama çalıştırılamıyor.",
            ["et"] = "See ketas ei ole NTFS, seega süvaskaneerimist ei saa käivitada.",
        },

        ["ntfs.mft_unreadable"] = new()
        {
            ["en"] = "The $MFT record could not be read — the volume may be damaged or encrypted.",
            ["tr"] = "$MFT kaydı okunamadı — birim bozuk ya da şifreli olabilir.",
            ["et"] = "$MFT kirjet ei õnnestunud lugeda — ketas võib olla kahjustatud või krüpteeritud.",
        },

        ["read.interrupted"] = new()
        {
            ["en"] = "Reading from the source volume was interrupted.",
            ["tr"] = "Kaynak birimden okuma yarıda kesildi.",
            ["et"] = "Lugemine lähtekettalt katkes.",
        },

        ["elevation.declined"] = new()
        {
            ["en"] = "Administrator permission was not granted. Raw disk access is impossible without it.",
            ["tr"] = "Yönetici izni verilmedi. Ham disk okuma bu izin olmadan mümkün değil.",
            ["et"] = "Administraatori luba jäi andmata. Ilma selleta ei ole toorketta lugemine võimalik.",
        },

        ["chat.session_expired"] = new()
        {
            ["en"] = "Your session has expired. Please sign in again.",
            ["tr"] = "Oturumunuz düşmüş. Lütfen yeniden giriş yapın.",
            ["et"] = "Teie seanss on aegunud. Palun logige uuesti sisse.",
        },

        ["chat.unavailable"] = new()
        {
            ["en"] = "The assistant is not responding right now. Scanning and recovery are unaffected.",
            ["tr"] = "Asistan şu an yanıt vermiyor. Tarama ve kurtarma etkilenmedi.",
            ["et"] = "Assistent ei vasta praegu. See ei mõjuta skannimist ega taastamist.",
        },

        ["chat.empty_message"] = new()
        {
            ["en"] = "An empty message cannot be sent.",
            ["tr"] = "Boş mesaj gönderilemez.",
            ["et"] = "Tühja sõnumit ei saa saata.",
        },

        ["chat.no_proposal"] = new()
        {
            ["en"] = "There is no proposal to approve.",
            ["tr"] = "Onaylanacak bir öneri yok.",
            ["et"] = "Kinnitamiseks pole ettepanekut.",
        },

        ["recover.no_destination"] = new()
        {
            ["en"] = "Choose the folder for recovered files first.",
            ["tr"] = "Önce kurtarılanların yazılacağı klasörü seçin.",
            ["et"] = "Valige esmalt taastatud failide kaust.",
        },

        ["recover.not_found"] = new()
        {
            ["en"] = "The selected candidates were not found; run the scan again.",
            ["tr"] = "Seçilen adaylar bulunamadı; taramayı yenileyin.",
            ["et"] = "Valitud kandidaate ei leitud; käivitage skannimine uuesti.",
        },

        ["drive.invalid"] = new()
        {
            ["en"] = "Invalid drive letter.",
            ["tr"] = "Geçersiz disk harfi.",
            ["et"] = "Vigane kettatäht.",
        },

        ["cloud.no_upload_url"] = new()
        {
            ["en"] = "OneDrive did not return an upload address.",
            ["tr"] = "OneDrive bir yükleme adresi döndürmedi.",
            ["et"] = "OneDrive ei tagastanud üleslaadimisaadressi.",
        },

        ["cloud.rejected"] = new()
        {
            ["en"] = "OneDrive refused the request (HTTP {0}). {1}",
            ["tr"] = "OneDrive isteği reddetti (HTTP {0}). {1}",
            ["et"] = "OneDrive keeldus päringust (HTTP {0}). {1}",
        },

        ["cloud.out_of_space"] = new()
        {
            ["en"] = "There is not enough free space in your OneDrive.",
            ["tr"] = "OneDrive'ınızda yeterli boş alan yok.",
            ["et"] = "Teie OneDrive'is pole piisavalt vaba ruumi.",
        },

        /*
         * ⚠️ Bu iki cümle bir BAŞARISIZLIK bildirir, uyarı değil: kullanıcı bu
         * dosyayı tek kopyası sanıp kaynağı silebilir. Eksik ya da bozuk bir
         * yüklemeyi "tamam" diye göstermek, veriyi bizim kaybettirmemiz olurdu.
         */
        ["cloud.size_mismatch"] = new()
        {
            ["en"] = "{0} did not upload completely ({1} bytes expected, {2} arrived).",
            ["tr"] = "{0} eksik yüklendi ({1} bayt bekleniyordu, {2} ulaştı).",
            ["et"] = "{0} ei laadinud täielikult üles (oodati {1} baiti, saabus {2}).",
        },

        ["cloud.hash_mismatch"] = new()
        {
            ["en"] = "The content of {0} did not match after upload; it was not marked as recovered.",
            ["tr"] = "{0} dosyasının içeriği yükleme sonrası eşleşmedi; kurtarıldı sayılmadı.",
            ["et"] = "Faili {0} sisu ei ühtinud pärast üleslaadimist; seda ei märgitud taastatuks.",
        },

        ["cloud.incomplete"] = new()
        {
            ["en"] = "The upload could not be completed.",
            ["tr"] = "Yükleme tamamlanamadı.",
            ["et"] = "Üleslaadimist ei õnnestunud lõpetada.",
        },

        ["cloud.not_connected"] = new()
        {
            ["en"] = "No OneDrive account is connected.",
            ["tr"] = "Bağlı bir OneDrive hesabı yok.",
            ["et"] = "Ühtegi OneDrive'i kontot pole ühendatud.",
        },

        /*
         * ⚠️ Alan YÜKLEMEDEN ÖNCE kontrol edilir. Ücretsiz OneDrive 5 GB ve
         * genelde yarı dolu; kontrol etmeden başlamak kullanıcıyı saatler süren
         * bir yüklemenin ortasında duvara toslatırdı.
         */
        ["cloud.not_enough_space"] = new()
        {
            ["en"] = "The selected files need {0} but only {1} is free in your OneDrive. Select fewer files or free up space.",
            ["tr"] = "Seçilen dosyalar {0} yer istiyor; OneDrive'ınızda yalnızca {1} boş. Daha az dosya seçin ya da yer açın.",
            ["et"] = "Valitud failid vajavad {0}, kuid teie OneDrive'is on vaba ainult {1}. Valige vähem faile või vabastage ruumi.",
        },

        ["cloud.folder"] = new()
        {
            ["en"] = "Scalio Recovery",
            ["tr"] = "Scalio Kurtarma",
            ["et"] = "Scalio taastamine",
        },

        ["restore.only_recycle_bin"] = new()
        {
            ["en"] = "Only Recycle Bin items can be restored in place. Deep scan results need a folder on another drive.",
            ["tr"] = "Yalnızca Geri Dönüşüm Kutusundaki dosyalar yerine geri yüklenebilir. Derin tarama sonuçları başka bir diskte klasör ister.",
            ["et"] = "Kohapeal saab taastada ainult prügikasti faile. Süvaskaneerimise tulemused vajavad kausta teisel kettal.",
        },

        ["restore.unknown_original"] = new()
        {
            ["en"] = "The original location of {0} is unknown, so it cannot be restored in place.",
            ["tr"] = "{0} dosyasının özgün konumu bilinmiyor; yerine geri yüklenemez.",
            ["et"] = "Faili {0} algne asukoht on teadmata, seega ei saa seda kohapeal taastada.",
        },

        ["restore.different_volume"] = new()
        {
            ["en"] = "{0} came from a different drive, so restoring in place would copy it. Choose a folder instead.",
            ["tr"] = "{0} başka bir diskten geliyor; yerine geri yüklemek onu kopyalamak olurdu. Bunun yerine klasör seçin.",
            ["et"] = "{0} pärineb teiselt kettalt, seega kohapeal taastamine kopeeriks selle. Valige selle asemel kaust.",
        },

        ["restore.original_locations"] = new()
        {
            ["en"] = "their original locations",
            ["tr"] = "özgün konumları",
            ["et"] = "nende algsed asukohad",
        },

        ["destination.outside"] = new()
        {
            ["en"] = "The file would be written outside the chosen folder, so it was refused.",
            ["tr"] = "Dosya, seçilen klasörün dışına yazılacaktı; işlem reddedildi.",
            ["et"] = "Fail oleks kirjutatud valitud kaustast välja, seetõttu keelduti.",
        },

        ["destination.choose_title"] = new()
        {
            ["en"] = "Where should recovered files be written?",
            ["tr"] = "Kurtarılan dosyalar nereye yazılsın?",
            ["et"] = "Kuhu taastatud failid kirjutada?",
        },

        /*
         * ⚠️ Bu cümle "kurtarma başarısız" DEMEZ ve dememeli: içerik gerçekten
         * geri getirilemiyor, yeniden denemenin faydası yok. Sebebi söylemek,
         * kullanıcıyı sonuçsuz bir döngüden kurtarır.
         */
        ["recover.unsupported"] = new()
        {
            ["en"] = "{0} is compressed or encrypted by Windows, so its content cannot be rebuilt from raw disk data.",
            ["tr"] = "{0} Windows tarafından sıkıştırılmış ya da şifrelenmiş; içeriği ham disk verisinden geri kurulamıyor.",
            ["et"] = "{0} on Windowsi poolt pakitud või krüpteeritud, seega ei saa selle sisu toorandmetest taastada.",
        },

        ["recover.unnamed"] = new()
        {
            ["en"] = "recovered-file",
            ["tr"] = "kurtarilan-dosya",
            ["et"] = "taastatud-fail",
        },

        ["ntfs.boot_unreadable"] = new()
        {
            ["en"] = "The boot sector could not be read.",
            ["tr"] = "Önyükleme sektörü okunamadı.",
            ["et"] = "Alglaadesektorit ei õnnestunud lugeda.",
        },

        ["ntfs.bad_geometry"] = new()
        {
            ["en"] = "Invalid volume geometry.",
            ["tr"] = "Geçersiz birim geometrisi.",
            ["et"] = "Vigane ketta geomeetria.",
        },

        ["ntfs.bad_record_size"] = new()
        {
            ["en"] = "Invalid MFT record size: {0}",
            ["tr"] = "Geçersiz MFT kayıt boyutu: {0}",
            ["et"] = "Vigane MFT kirje suurus: {0}",
        },

        ["job.rejected"] = new()
        {
            ["en"] = "The job file was refused.",
            ["tr"] = "İş dosyası kabul edilmedi.",
            ["et"] = "Töökirjeldust ei aktsepteeritud.",
        },

        ["job.unreadable"] = new()
        {
            ["en"] = "The job file could not be read: {0}",
            ["tr"] = "İş dosyası okunamadı: {0}",
            ["et"] = "Töökirjeldust ei õnnestunud lugeda: {0}",
        },

        ["job.empty"] = new()
        {
            ["en"] = "The job file is empty.",
            ["tr"] = "İş dosyası boş.",
            ["et"] = "Töökirjeldus on tühi.",
        },

        ["job.unknown_action"] = new()
        {
            ["en"] = "Unknown job: {0}",
            ["tr"] = "Bilinmeyen iş: {0}",
            ["et"] = "Tundmatu töö: {0}",
        },

        ["job.cancelled"] = new()
        {
            ["en"] = "The scan was cancelled.",
            ["tr"] = "Tarama iptal edildi.",
            ["et"] = "Skannimine katkestati.",
        },

        ["chat.thinking"] = new()
        {
            ["en"] = "Thinking…",
            ["tr"] = "Düşünüyor…",
            ["et"] = "Mõtlen…",
        },

        ["chat.searching"] = new()
        {
            ["en"] = "Searching the disk…",
            ["tr"] = "Diskte arıyor…",
            ["et"] = "Otsin kettalt…",
        },

        ["chat.gave_up"] = new()
        {
            ["en"] = "I stopped searching here without reaching a result. Could you describe the file a little more — "
                     + "part of its name, its date, or which folder it was in?",
            ["tr"] = "Aramayı burada durdurdum, sonuca ulaşamadım. Aradığınız dosyayı biraz daha tarif eder misiniz — "
                     + "adının bir kısmı, tarihi ya da hangi klasörde olduğu?",
            ["et"] = "Lõpetasin otsingu siin, tulemuseni ei jõudnud. Kas kirjeldaksite faili veidi täpsemalt — "
                     + "osa nimest, kuupäev või milline kaust?",
        },

        ["chat.tool_volumes"] = new()
        {
            ["en"] = "Listing drives…",
            ["tr"] = "Diskleri listeliyor…",
            ["et"] = "Loetlen kettaid…",
        },

        ["chat.tool_recycle"] = new()
        {
            ["en"] = "Scanning the Recycle Bin…",
            ["tr"] = "Geri Dönüşüm Kutusunu tarıyor…",
            ["et"] = "Skannin prügikasti…",
        },

        ["chat.tool_deep"] = new()
        {
            ["en"] = "Looking at the deep scan results…",
            ["tr"] = "Derin tarama sonuçlarına bakıyor…",
            ["et"] = "Vaatan süvaskaneerimise tulemusi…",
        },

        ["chat.tool_shadow"] = new()
        {
            ["en"] = "Reading the shadow-copy scan…",
            ["tr"] = "Gölge kopya taraması okunuyor…",
            ["et"] = "Loen varikoopiate skanni…",
        },

        ["chat.tool_filehistory"] = new()
        {
            ["en"] = "Reading File History results…",
            ["tr"] = "Dosya Geçmişi sonuçları okunuyor…",
            ["et"] = "Loen failiajaloo tulemusi…",
        },

        ["chat.tool_onedrive"] = new()
        {
            ["en"] = "Reading OneDrive recycle bin results…",
            ["tr"] = "OneDrive geri dönüşüm kutusu sonuçları okunuyor…",
            ["et"] = "Loen OneDrive'i prügikasti tulemusi…",
        },

        ["chat.tool_carve"] = new()
        {
            ["en"] = "Reading the signature scan…",
            ["tr"] = "Kalıntı taraması okunuyor…",
            ["et"] = "Loen signatuuriskanni…",
        },

        ["chat.tool_propose"] = new()
        {
            ["en"] = "Preparing a suggestion…",
            ["tr"] = "Öneri hazırlıyor…",
            ["et"] = "Valmistan ettepanekut…",
        },

        ["chat.tool_working"] = new()
        {
            ["en"] = "Working…",
            ["tr"] = "Çalışıyor…",
            ["et"] = "Töötan…",
        },

        ["path.lost_folder"] = new()
        {
            ["en"] = "[lost folder]",
            ["tr"] = "[kayıp klasör]",
            ["et"] = "[kadunud kaust]",
        },

        ["scan.recycle_running"] = new()
        {
            ["en"] = "Scanning the Recycle Bin…",
            ["tr"] = "Geri Dönüşüm Kutusu taranıyor…",
            ["et"] = "Skannin prügikasti…",
        },

        ["scan.shadow_running"] = new()
        {
            ["en"] = "Comparing shadow copies with the drive…",
            ["tr"] = "Gölge kopyalar diskle karşılaştırılıyor…",
            ["et"] = "Võrdlen varikoopiaid kettaga…",
        },

        /*
         * ⚠️ "Gölge kopya yok" bir ARIZA değil, bir durumdur: Sistem Geri
         * Yükleme kapalıysa Windows hiç kopya almaz. Cümle bunu söylemeli ve
         * kullanıcıya ne yapabileceğini bırakmalı — kırmızı hata kutusu değil.
         */
        ["shadow.none"] = new()
        {
            ["en"] = "Windows has no shadow copies of {0}. They exist only when System Protection or File History is on for that drive.",
            ["tr"] = "Windows'un {0} için gölge kopyası yok. Kopyalar yalnız o disk için Sistem Koruması ya da Dosya Geçmişi açıksa alınır.",
            ["et"] = "Windowsil pole ketta {0} varikoopiaid. Neid tehakse ainult siis, kui selle ketta jaoks on Süsteemikaitse või Failiajalugu sisse lülitatud.",
        },

        ["scan.carve_running"] = new()
        {
            ["en"] = "Reading free space for file signatures…",
            ["tr"] = "Boş alan dosya imzaları için okunuyor…",
            ["et"] = "Loen vaba ruumi failisignatuuride leidmiseks…",
        },

        ["recover.onedrive_use_restore"] = new()
        {
            ["en"] = "\"{0}\" is in your OneDrive recycle bin — it is restored in OneDrive, not copied to a folder.",
            ["tr"] = "\"{0}\" OneDrive geri dönüşüm kutusunda — bir klasöre kopyalanmaz, OneDrive'da yerine geri yüklenir.",
            ["et"] = "\"{0}\" on teie OneDrive'i prügikastis — seda ei kopeerita kausta, vaid taastatakse OneDrive'is.",
        },

        ["scan.onedrive_running"] = new()
        {
            ["en"] = "Reading your OneDrive recycle bin…",
            ["tr"] = "OneDrive geri dönüşüm kutunuz okunuyor…",
            ["et"] = "Loen teie OneDrive'i prügikasti…",
        },

        ["onedrive.not_connected"] = new()
        {
            ["en"] = "Connect your OneDrive in Settings first — the online recycle bin lives in your account.",
            ["tr"] = "Önce Ayarlar'dan OneDrive'ınızı bağlayın — çevrimiçi geri dönüşüm kutusu hesabınızda.",
            ["et"] = "Ühendage kõigepealt Seadetes oma OneDrive — veebiprügikast asub teie kontos.",
        },

        ["update.downloading"] = new()
        {
            ["en"] = "Downloading the update…",
            ["tr"] = "Güncelleme indiriliyor…",
            ["et"] = "Laadin värskendust alla…",
        },

        ["update.no_download"] = new()
        {
            ["en"] = "There is no installer to download or install.",
            ["tr"] = "İndirilecek ya da kurulacak bir kurulum dosyası yok.",
            ["et"] = "Allalaaditavat või paigaldatavat installifaili pole.",
        },

        ["update.no_checksum"] = new()
        {
            ["en"] = "The server did not publish a checksum for this update, so Scalio will not install it automatically. Download it from the website instead.",
            ["tr"] = "Sunucu bu güncelleme için özet yayınlamadı; Scalio doğrulayamadığı bir paketi kendiliğinden kurmaz. Web sitesinden indirin.",
            ["et"] = "Server ei avaldanud selle värskenduse kontrollsummat, seega Scalio ei paigalda seda automaatselt. Laadige see alla veebisaidilt.",
        },

        ["update.download_failed"] = new()
        {
            ["en"] = "The update could not be downloaded (HTTP {0}).",
            ["tr"] = "Güncelleme indirilemedi (HTTP {0}).",
            ["et"] = "Värskendust ei õnnestunud alla laadida (HTTP {0}).",
        },

        ["update.hash_mismatch"] = new()
        {
            ["en"] = "The downloaded installer does not match the published checksum, so it was deleted and will not be installed. Try again or download from the website.",
            ["tr"] = "İndirilen kurulum dosyası yayınlanan özetle tutmuyor; silindi ve kurulmayacak. Yeniden deneyin ya da web sitesinden indirin.",
            ["et"] = "Allalaaditud installifail ei vasta avaldatud kontrollsummale; see kustutati ja seda ei paigaldata. Proovige uuesti või laadige alla veebisaidilt.",
        },

        ["update.install_failed"] = new()
        {
            ["en"] = "The installer could not be started: {0}",
            ["tr"] = "Kurulum programı başlatılamadı: {0}",
            ["et"] = "Installiprogrammi ei õnnestunud käivitada: {0}",
        },

        ["update.untrusted_host"] = new()
        {
            ["en"] = "The update address is not on talivio.com; the download was refused.",
            ["tr"] = "Güncelleme adresi talivio.com üzerinde değil; indirme reddedildi.",
            ["et"] = "Värskenduse aadress ei ole talivio.com-is; allalaadimisest keelduti.",
        },

        ["scan.filehistory_running"] = new()
        {
            ["en"] = "Comparing File History backups with the drive…",
            ["tr"] = "Dosya Geçmişi yedekleri sürücüyle karşılaştırılıyor…",
            ["et"] = "Võrdlen failiajaloo varukoopiaid kettaga…",
        },

        /*
         * ⚠️ "Dosya Geçmişi yedeği yok" bir ARIZA değil, bir durumdur: yedek
         * sürücüsü takılı değilse ya da Dosya Geçmişi hiç açılmamışsa Windows
         * hiçbir konum bulamaz. Cümle bunu söylemeli, kırmızı hata kutusu
         * değil (bkz. shadow.none).
         */
        ["filehistory.none"] = new()
        {
            ["en"] = "No File History backup for {0} is plugged in. Windows File History keeps versioned copies on a separate drive — connect that drive and try again.",
            ["tr"] = "{0} için takılı bir Dosya Geçmişi yedeği yok. Windows Dosya Geçmişi sürümlü kopyaları ayrı bir sürücüde tutar — o sürücüyü bağlayıp yeniden deneyin.",
            ["et"] = "Ketta {0} failiajaloo varukoopiat pole ühendatud. Windowsi failiajalugu hoiab versioonidega koopiaid eraldi kettal — ühendage see ketas ja proovige uuesti.",
        },

        ["elevation.prompt"] = new()
        {
            ["en"] = "Windows is asking for administrator permission — choose “Yes” in the window that opened.",
            ["tr"] = "Windows yönetici izni soruyor — açılan pencerede “Evet” deyin.",
            ["et"] = "Windows küsib administraatori luba — valige avanenud aknas „Jah“.",
        },

        ["elevation.started"] = new()
        {
            ["en"] = "The deep scan has started. You can leave this window open.",
            ["tr"] = "Derin tarama başladı. Bu pencereyi açık bırakabilirsiniz.",
            ["et"] = "Süvaskaneerimine algas. Võite selle akna lahti jätta.",
        },

        ["elevation.progress"] = new()
        {
            ["en"] = "Deep scan: {0}% · {1} candidates so far",
            ["tr"] = "Derin tarama: %{0} · şimdiye kadar {1} aday",
            ["et"] = "Süvaskaneerimine: {0}% · seni {1} kandidaati",
        },

        ["elevation.self_path"] = new()
        {
            ["en"] = "The application's own path could not be determined.",
            ["tr"] = "Uygulamanın kendi yolu bulunamadı.",
            ["et"] = "Rakenduse enda asukohta ei õnnestunud tuvastada.",
        },

        ["elevation.start_failed"] = new()
        {
            ["en"] = "The helper process could not be started.",
            ["tr"] = "Yardımcı süreç başlatılamadı.",
            ["et"] = "Abiprotsessi ei õnnestunud käivitada.",
        },

        ["elevation.unknown_error"] = new()
        {
            ["en"] = "Unknown error.",
            ["tr"] = "Bilinmeyen hata.",
            ["et"] = "Tundmatu viga.",
        },

        /*
         * ⚠️ Bu cümle bir antivirüsün süreci öldürdüğü durumu da kapsıyor ve
         * kullanıcıya "donmuş" gibi görünmemesi için VAR: eskiden arayüz burada
         * sonsuza kadar bekliyordu.
         */
        ["elevation.died"] = new()
        {
            ["en"] = "The helper process closed without writing a result (exit code {0}). "
                     + "Antivirus software may have stopped it.",
            ["tr"] = "Yardımcı süreç sonuç yazmadan kapandı (çıkış kodu {0}). "
                     + "Antivirüs yazılımı onu durdurmuş olabilir.",
            ["et"] = "Abiprotsess sulgus tulemust kirjutamata (väljumiskood {0}). "
                     + "Viirusetõrje võis selle peatada.",
        },

        ["app.webview_missing"] = new()
        {
            ["en"] = "Scalio could not start its interface. The Microsoft Edge WebView2 runtime is most likely "
                     + "missing; installing it from microsoft.com resolves this.\n\nDetail: {0}",
            ["tr"] = "Scalio arayüzünü başlatamadı. Büyük olasılıkla Microsoft Edge WebView2 çalışma zamanı "
                     + "eksik; microsoft.com adresinden kurmak sorunu çözer.\n\nAyrıntı: {0}",
            ["et"] = "Scalio ei suutnud liidest käivitada. Tõenäoliselt puudub Microsoft Edge WebView2 "
                     + "käituskeskkond; selle paigaldamine microsoft.com-ist lahendab probleemi.\n\nDetail: {0}",
        },
    };

}
