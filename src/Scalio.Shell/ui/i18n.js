/*
 * Arayüz metinleri — İngilizce ve Türkçe.
 *
 * ⚠️ VARSAYILAN İNGİLİZCE. Ürün euro fiyatla ve İngilizce ağırlıklı bir
 * siteyle satılıyor; Türkçe varsayılan, tanımadığımız bir kullanıcıya
 * anlamadığı bir dille açılmak demek olurdu.
 *
 * ⚠️ TON: kısa, sakin, somut. Teknik terim yalnız gerektiğinde ve hemen
 * ardından ne anlama geldiğiyle birlikte.
 */

const TRANSLATIONS = {
    // ── Kabuk ──────────────────────────────────────────────────────────────
    'app.nav_main':         { en: 'Upscale', tr: 'Büyüt' },
    'app.activity_upscaling': { en: 'Upscaling…', tr: 'Büyütülüyor…' },
    'app.theme_toggle':     { en: 'Switch theme', tr: 'Temayı değiştir' },
    'app.theme_light':      { en: 'Light', tr: 'Açık' },
    'app.theme_dark':       { en: 'Dark', tr: 'Koyu' },
    'app.theme_system':     { en: 'System', tr: 'Sistem' },
    'app.back':             { en: 'Back', tr: 'Geri' },
    'app.language':         { en: 'Language', tr: 'Dil' },
    'app.dismiss':          { en: 'Dismiss', tr: 'Kapat' },
    'app.retry':            { en: 'Try again', tr: 'Yeniden dene' },

    // ── Hata ───────────────────────────────────────────────────────────────
    'error.title':          { en: 'Something went wrong', tr: 'Bir sorun çıktı' },

    // ── Giriş ──────────────────────────────────────────────────────────────

    // ── Ana ekran ──────────────────────────────────────────────────────────
    'main.title':           { en: 'Make your images bigger, without the mush', tr: 'Görsellerinizi bulanıklaşmadan büyütün' },
    'main.intro':           { en: 'Drop photos or a folder below, or pick them by hand.', tr: 'Aşağıya fotoğraf ya da klasör bırakın, ya da elle seçin.' },
    'main.drop_title':      { en: 'Drop images or a folder here', tr: 'Görselleri ya da bir klasörü buraya bırakın' },
    'main.drop_hint':       { en: 'JPEG, PNG, WebP, TIFF, BMP', tr: 'JPEG, PNG, WebP, TIFF, BMP' },
    'main.pick_files':      { en: 'Choose images…', tr: 'Görsel seç…' },
    'main.pick_folder':     { en: 'Choose a folder…', tr: 'Klasör seç…' },
    'main.pick_failed':     { en: 'The file picker could not be opened.', tr: 'Dosya seçici açılamadı.' },
    'main.drop_failed':     { en: 'This file could not be added. Use "Choose images…" instead.', tr: 'Bu dosya eklenemedi. Bunun yerine "Görsel seç…" düğmesini kullanın.' },
    'main.enqueue_failed':  { en: 'The images could not be added to the queue.', tr: 'Görseller kuyruğa eklenemedi.' },
    'main.files_added':     { en: '{0} image(s) ready to configure.', tr: 'Ayarlanmayı bekleyen {0} görsel var.' },
    'main.remove_pending':  { en: 'Remove', tr: 'Kaldır' },

    // ── Kuyruk ─────────────────────────────────────────────────────────────
    'queue.title':          { en: 'Queue', tr: 'Kuyruk' },
    'queue.empty':          { en: 'No images added yet.', tr: 'Henüz görsel eklenmedi.' },
    'queue.status_queued':  { en: 'Queued', tr: 'Sırada' },
    'queue.status_processing': { en: 'Processing…', tr: 'İşleniyor…' },
    'queue.status_completed':  { en: 'Done', tr: 'Tamamlandı' },
    'queue.status_failed':     { en: 'Failed', tr: 'Hata' },
    'queue.status_cancelled':  { en: 'Cancelled', tr: 'İptal edildi' },
    'queue.cancel':         { en: 'Cancel', tr: 'İptal' },
    'queue.show_folder':    { en: 'Show in folder', tr: 'Klasörde göster' },
    'queue.compare':        { en: 'Before / after', tr: 'Önce / sonra' },

    // ── Ayarlar paneli ─────────────────────────────────────────────────────
    'settings.panel_title': { en: 'Upscale settings', tr: 'Büyütme ayarları' },
    'settings.size_label':  { en: 'Size', tr: 'Boyut' },
    'settings.mode_scale':  { en: 'Scale', tr: 'Ölçek' },
    'settings.mode_width':  { en: 'Target width', tr: 'Hedef genişlik' },
    'settings.model_label': { en: 'Model', tr: 'Model' },
    'settings.model_photo': { en: 'Photo', tr: 'Fotoğraf' },
    'settings.model_drawing': { en: 'Drawing / logo', tr: 'Çizim / logo' },
    'settings.format_label': { en: 'Output format', tr: 'Çıktı biçimi' },
    'settings.quality_label': { en: 'Quality', tr: 'Kalite' },
    'settings.crop_label':  { en: 'Crop preset', tr: 'Kırpma ön ayarı' },
    'settings.crop_none':   { en: 'None', tr: 'Yok' },
    'settings.crop_hero':   { en: 'Hero (2560×1440)', tr: 'Hero (2560×1440)' },
    'settings.crop_card':   { en: 'Card (1200×800)', tr: 'Card (1200×800)' },
    'settings.crop_instagram': { en: 'Instagram (1080×1350)', tr: 'Instagram (1080×1350)' },
    'settings.crop_opengraph': { en: 'Open Graph (1200×630)', tr: 'Open Graph (1200×630)' },
    'settings.enqueue':     { en: 'Add to queue', tr: 'Kuyruğa ekle' },

    // ── Ayarlar / Yardım ekranı ────────────────────────────────────────────
    'help.title':           { en: 'Settings & help', tr: 'Ayarlar ve yardım' },
    'help.version_title':   { en: 'Version', tr: 'Sürüm' },
    'help.version_line':    { en: 'Version {0}', tr: 'Sürüm {0}' },
    'settings.support':     { en: 'Help & diagnostics', tr: 'Yardım ve tanı' },
    'support.body':         { en: 'Create a diagnostic file to send us if something goes wrong.', tr: 'Bir şeyler ters giderse bize gönderebileceğiniz bir tanı dosyası oluşturun.' },
    'support.create':       { en: 'Create diagnostic file', tr: 'Tanı dosyası oluştur' },
    'support.open_folder':  { en: 'Open log folder', tr: 'Günlük klasörünü aç' },
    'support.written':      { en: 'Saved to {0}', tr: 'Kaydedildi: {0}' },
    'support.failed':       { en: 'The diagnostic file could not be created.', tr: 'Tanı dosyası oluşturulamadı.' },
    'support.website':      { en: 'Scalio website', tr: 'Scalio web sitesi' },
    'help.how_title':       { en: 'How it works', tr: 'Nasıl çalışır' },
    'help.step_1':          { en: 'Drag and drop images or a folder, or pick them by hand.', tr: 'Görselleri ya da bir klasörü sürükleyip bırakın, ya da elle seçin.' },
    'help.step_2':          { en: 'Set scale or target width, model (photo/drawing) and output format.', tr: 'Ölçek ya da hedef genişliği, modeli (fotoğraf/çizim) ve çıktı biçimini ayarlayın.' },
    'help.step_3':          { en: 'Add to the queue — the engine upscales on this computer’s GPU.', tr: 'Kuyruğa ekleyin — motor bu bilgisayarın ekran kartında büyütür.' },
    'help.step_4':          { en: 'Compare before/after and find the result in its folder.', tr: 'Önce/sonra karşılaştırın, sonucu bulunduğu klasörde bulun.' },
    'help.step_5':          {
        en: 'Scalio is free: x2, x4, batches and ready-made sizes — no account, no trial, no subscription.',
        tr: 'Scalio ücretsiz: x2, x4, toplu işlem ve hazır boyutlar — hesap, deneme ya da abonelik yok.',
    },
    'settings.appearance':  { en: 'Appearance', tr: 'Görünüm' },
    'settings.theme':       { en: 'Theme', tr: 'Tema' },
    'settings.about':       { en: 'About', tr: 'Hakkında' },
    'settings.about_1':     { en: 'Photos are upscaled with Real-ESRGAN, on this computer.', tr: 'Fotoğraflar bu bilgisayarda, Real-ESRGAN ile büyütülür.' },
    'settings.about_2':     { en: 'Nothing you upscale is ever uploaded.', tr: 'Büyüttüğünüz hiçbir şey yüklenmez.' },
    'settings.about_3':     { en: 'Every feature is free, with no account needed.', tr: 'Tüm özellikler ücretsiz, hesap gerekmez.' },

    // ── Güncelleme ─────────────────────────────────────────────────────────
    'update.check':         { en: 'Check for updates', tr: 'Güncelleme denetle' },
    'update.get':           { en: 'Download', tr: 'İndir' },
    'update.available':     { en: 'Scalio {0} is available — you have {1}.', tr: 'Scalio {0} çıktı — sizde {1} var.' },
    'update.current':       { en: 'Version {0} — up to date.', tr: 'Sürüm {0} — güncel.' },
    'update.manual_note':   { en: 'The download opens in your browser.', tr: 'İndirme tarayıcınızda açılır.' },
    'update.later':         { en: 'Later', tr: 'Sonra' },
    'update.download_browser': { en: 'Download', tr: 'İndir' },
    'update.modal_title':   { en: 'Scalio {0} is ready', tr: 'Scalio {0} hazır' },
    'update.modal_body':    {
        en: 'You’re using {0} right now. The download opens in your browser.',
        tr: 'Şu an {0} kullanıyorsunuz. İndirme tarayıcınızda açılır.',
    },
    'update.required_body': {
        en: 'This version is no longer supported; update to continue.',
        tr: 'Bu sürüm artık desteklenmiyor; devam etmek için güncelleyin.',
    },

    // ── Karşılaştırma ──────────────────────────────────────────────────────
    'compare.close':        { en: 'Close', tr: 'Kapat' },
    'compare.before':       { en: 'Before', tr: 'Önce' },
    'compare.after':        { en: 'After', tr: 'Sonra' },

    // ── Önizleme (uygulama dışında açıldığında) ────────────────────────────
    'preview.note': {
        en: 'Preview mode — this page is open outside Scalio. Nothing shown here touches a real file.',
        tr: 'Önizleme modu — bu sayfa Scalio dışında açıldı. Burada gösterilenler gerçek bir dosyaya dokunmaz.',
    },
};

const LANGUAGES = ['en', 'tr'];
const LANGUAGE_NAMES = { en: 'English', tr: 'Türkçe' };

let currentLanguage = 'en';

/**
 * ⚠️ Eksik çeviride ANAHTAR GÖSTERİLMEZ, İngilizceye düşülür: kullanıcıya
 * "queue.cancel" yazmak hiçbir şey anlatmaz.
 */
function t(key, ...params) {
    const entry = TRANSLATIONS[key];

    if (!entry) {
        return key;
    }

    const text = entry[currentLanguage] ?? entry.en ?? key;

    return params.length === 0
        ? text
        : text.replace(/\{(\d+)\}/g, (_, index) => params[Number(index)] ?? '');
}

/** Sayfadaki `data-i18n` işaretli her düğümü günceller. */
function applyTranslations() {
    document.documentElement.lang = currentLanguage;

    for (const el of document.querySelectorAll('[data-i18n]')) {
        const icon = el.querySelector(':scope > [data-icon]');

        el.textContent = t(el.dataset.i18n);

        if (icon) {
            el.prepend(icon);
        }
    }

    for (const el of document.querySelectorAll('[data-i18n-placeholder]')) {
        el.placeholder = t(el.dataset.i18nPlaceholder);
    }

    for (const el of document.querySelectorAll('[data-i18n-title]')) {
        el.title = t(el.dataset.i18nTitle);
    }

    for (const el of document.querySelectorAll('[data-i18n-label]')) {
        el.setAttribute('aria-label', t(el.dataset.i18nLabel));
    }

    applyIcons();
}

function setLanguage(language) {
    currentLanguage = LANGUAGES.includes(language) ? language : 'en';

    try {
        localStorage.setItem('scalio.language', currentLanguage);
    } catch {
        // Depolama kapalıysa dil yine çalışır, yalnızca hatırlanmaz.
    }

    applyTranslations();
}

function currentLocale() {
    return currentLanguage;
}

/**
 * Açılış dili: kaydedilmiş tercih → işletim sistemi dili → İngilizce.
 */
function initialLanguage() {
    try {
        const stored = localStorage.getItem('scalio.language');

        if (stored && LANGUAGES.includes(stored)) {
            return stored;
        }
    } catch { /* yoksay */ }

    const system = (navigator.language ?? 'en').slice(0, 2).toLowerCase();

    return LANGUAGES.includes(system) ? system : 'en';
}
