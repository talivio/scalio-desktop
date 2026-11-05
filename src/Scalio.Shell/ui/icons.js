/*
 * İkon seti — gömülü satır içi SVG.
 *
 * ⚠️ İKON YAZI TİPİ YA DA DIŞ KÜTÜPHANE YOK VE OLMAYACAK. Sayfanın içerik
 * güvenlik politikası dışarıya HİÇBİR isteğe izin vermiyor (`default-src
 * 'none'`) — bir kurtarma programının arayüzü internete bağlanmamalı,
 * kullanıcının dosya adlarının sızması için en kısa yol o olurdu. Font
 * Awesome benzeri bir paket ya ağdan gelirdi ya da pakete birkaç yüz KB
 * eklerdi; ihtiyacımız olan kırk kadar simge için ikisi de fazla.
 *
 * ⚠️ HEPSİ `currentColor` KULLANIR. Sabit renk verilseydi koyu temada
 * görünmez olurdu; ikon, üstünde durduğu düğmenin rengini alır.
 *
 * ⚠️ `stroke-linecap="square"` — tasarım dili köşeli (`cut` sınıfları). Yuvarlak
 * uçlu ikonlar arayüzün geri kalanıyla çelişiyordu.
 */

const ICONS = {
    // ── Durum ──────────────────────────────────────────────────────────────
    shield: '<path d="M12 3 4 6.5v5.5c0 4.5 3.4 7.2 8 8.5 4.6-1.3 8-4 8-8.5V6.5L12 3Z"/>',
    shieldCheck: '<path d="M12 3 4 6.5v5.5c0 4.5 3.4 7.2 8 8.5 4.6-1.3 8-4 8-8.5V6.5L12 3Z"/><path d="m8.5 12 2.5 2.5 4.5-4.5"/>',
    check: '<path d="m5 12.5 4.5 4.5L19 7"/>',
    checkCircle: '<circle cx="12" cy="12" r="8.5"/><path d="m8 12.5 2.8 2.8L16.5 9"/>',
    alert: '<path d="M12 4 2.5 20h19L12 4Z"/><path d="M12 10v4"/><path d="M12 17.2v.1"/>',
    info: '<circle cx="12" cy="12" r="8.5"/><path d="M12 11v5"/><path d="M12 8v.1"/>',
    close: '<path d="m6 6 12 12"/><path d="m18 6-12 12"/>',
    clock: '<circle cx="12" cy="12" r="8.5"/><path d="M12 7.5V12l3 2"/>',
    lock: '<path d="M6 10.5h12v10H6z"/><path d="M8.5 10.5V7.5a3.5 3.5 0 0 1 7 0v3"/>',
    sparkle: '<path d="M12 3v4"/><path d="M12 17v4"/><path d="M3 12h4"/><path d="M17 12h4"/><path d="m6 6 2.5 2.5"/><path d="m15.5 15.5 2.5 2.5"/><path d="m18 6-2.5 2.5"/><path d="m8.5 15.5-2.5 2.5"/>',

    // ── Eylem ──────────────────────────────────────────────────────────────
    search: '<circle cx="11" cy="11" r="6.5"/><path d="m16 16 4.5 4.5"/>',
    scanDeep: '<path d="M3 7V4h3"/><path d="M21 7V4h-3"/><path d="M3 17v3h3"/><path d="M21 17v3h-3"/><circle cx="12" cy="12" r="3.5"/>',
    restore: '<path d="M4 12a8 8 0 1 0 2.5-5.8"/><path d="M4 4v4h4"/>',
    download: '<path d="M12 4v10"/><path d="m7.5 10 4.5 4.5 4.5-4.5"/><path d="M4.5 19.5h15"/>',
    upload: '<path d="M12 20V9.5"/><path d="m7.5 13.5 4.5-4.5 4.5 4.5"/><path d="M4.5 4.5h15"/>',
    folder: '<path d="M3 6.5h6l2 2.5h10v11H3v-13.5Z"/>',
    folderOpen: '<path d="M3 6.5h6l2 2.5h9v3"/><path d="M3 20V6.5"/><path d="M3 20h15.5l3-8H6.5l-3.5 8Z"/>',
    trash: '<path d="M4.5 6.5h15"/><path d="M9 6.5V4h6v2.5"/><path d="M6.5 6.5 7.5 20h9l1-13.5"/>',
    recycle: '<path d="M4.5 6.5h15"/><path d="M9 6.5V4h6v2.5"/><path d="M6.5 6.5 7.5 20h9l1-13.5"/><path d="M10 10.5v6"/><path d="M14 10.5v6"/>',
    chat: '<path d="M4 5h16v11H9l-5 4V5Z"/>',
    send: '<path d="M4 12 20 4l-4.5 16-4-6.5L4 12Z"/>',
    logout: '<path d="M14 4H5v16h9"/><path d="M18 8.5 21.5 12 18 15.5"/><path d="M10.5 12h11"/>',
    login: '<path d="M10 4h9v16h-9"/><path d="M6 8.5 2.5 12 6 15.5"/><path d="M2.5 12h11"/>',
    back: '<path d="M20 12H4"/><path d="m9.5 6.5-5.5 5.5 5.5 5.5"/>',
    forward: '<path d="M4 12h16"/><path d="m14.5 6.5 5.5 5.5-5.5 5.5"/>',
    external: '<path d="M14 4h6v6"/><path d="M20 4 11 13"/><path d="M18 13.5V20H4V6h6.5"/>',
    monitor: '<rect x="3" y="4" width="18" height="12" rx="1.5"/><path d="M9 20h6"/><path d="M12 16v4"/>',
    settings: '<circle cx="12" cy="12" r="3.2"/><path d="M19.4 15a1.6 1.6 0 0 0 .3 1.8l.1.1a2 2 0 1 1-2.8 2.8l-.1-.1a1.6 1.6 0 0 0-1.8-.3 1.6 1.6 0 0 0-1 1.5v.2a2 2 0 1 1-4 0v-.1a1.6 1.6 0 0 0-1-1.5 1.6 1.6 0 0 0-1.8.3l-.1.1a2 2 0 1 1-2.8-2.8l.1-.1a1.6 1.6 0 0 0 .3-1.8 1.6 1.6 0 0 0-1.5-1H3a2 2 0 1 1 0-4h.1a1.6 1.6 0 0 0 1.5-1 1.6 1.6 0 0 0-.3-1.8l-.1-.1a2 2 0 1 1 2.8-2.8l.1.1a1.6 1.6 0 0 0 1.8.3H9a1.6 1.6 0 0 0 1-1.5V3a2 2 0 1 1 4 0v.1a1.6 1.6 0 0 0 1 1.5 1.6 1.6 0 0 0 1.8-.3l.1-.1a2 2 0 1 1 2.8 2.8l-.1.1a1.6 1.6 0 0 0-.3 1.8V9a1.6 1.6 0 0 0 1.5 1h.2a2 2 0 1 1 0 4h-.1a1.6 1.6 0 0 0-1.5 1Z"/>',
    home: '<path d="M4 11 12 4l8 7"/><path d="M6 10v10h12V10"/><path d="M10 20v-6h4v6"/>',
    sort: '<path d="M4 7h16"/><path d="M7 12h10"/><path d="M10 17h4"/>',
    copy: '<path d="M9 9h11v11H9z"/><path d="M4 15V4h11"/>',
    phone: '<path d="M7 3h10v18H7z"/><path d="M11 18h2"/>',
    user: '<circle cx="12" cy="8.5" r="3.5"/><path d="M4.5 20.5a7.5 7.5 0 0 1 15 0"/>',
    plus: '<path d="M12 5v14"/><path d="M5 12h14"/>',
    minus: '<path d="M5 12h14"/>',
    eye: '<path d="M2.5 12S6 5.5 12 5.5 21.5 12 21.5 12 18 18.5 12 18.5 2.5 12 2.5 12Z"/><circle cx="12" cy="12" r="3"/>',

    // ── Nesne ──────────────────────────────────────────────────────────────
    drive: '<path d="M3 5h18v9H3z"/><path d="M3 14h18v5H3z"/><path d="M6.5 16.5h.1"/>',
    usb: '<path d="M9 3h6v5H9z"/><path d="M7 8h10v13H7z"/><path d="M10.5 12h.1"/><path d="M13.5 12h.1"/>',
    cloud: '<path d="M7 18.5A4 4 0 0 1 7.4 10.6a5.2 5.2 0 0 1 10 1.1 3.6 3.6 0 0 1-.4 6.8H7Z"/>',
    cloudOff: '<path d="M7 18.5A4 4 0 0 1 7.4 10.6a5.2 5.2 0 0 1 10 1.1 3.6 3.6 0 0 1-.4 6.8H7Z"/><path d="m4 4 16 16"/>',
    file: '<path d="M6 3h8l4 4v14H6V3Z"/><path d="M14 3v4h4"/>',
    fileDoc: '<path d="M6 3h8l4 4v14H6V3Z"/><path d="M14 3v4h4"/><path d="M9 12h6"/><path d="M9 15.5h6"/>',
    fileSheet: '<path d="M6 3h8l4 4v14H6V3Z"/><path d="M14 3v4h4"/><path d="M9 11h6v7H9z"/><path d="M12 11v7"/><path d="M9 14.5h6"/>',
    fileSlides: '<path d="M6 3h8l4 4v14H6V3Z"/><path d="M14 3v4h4"/><path d="M9 11h6v5H9z"/>',
    filePdf: '<path d="M6 3h8l4 4v14H6V3Z"/><path d="M14 3v4h4"/><path d="M9 17v-6h2a1.5 1.5 0 0 1 0 3H9"/><path d="M14 11v6"/>',
    fileImage: '<path d="M6 3h8l4 4v14H6V3Z"/><path d="M14 3v4h4"/><path d="m8 18 3-4 2.5 2.5L15 14l3 4"/><path d="M9.5 11h.1"/>',
    fileVideo: '<path d="M6 3h8l4 4v14H6V3Z"/><path d="M14 3v4h4"/><path d="m10 11 5 3-5 3v-6Z"/>',
    fileAudio: '<path d="M6 3h8l4 4v14H6V3Z"/><path d="M14 3v4h4"/><path d="M14 10v6"/><path d="M14 10l-4 1v6"/><circle cx="12.3" cy="17" r="1.6"/><circle cx="8.3" cy="17.5" r="1.6"/>',
    fileArchive: '<path d="M6 3h8l4 4v14H6V3Z"/><path d="M14 3v4h4"/><path d="M10 3v2"/><path d="M10 7v2"/><path d="M10 11v2"/><path d="M8.5 15h3v3h-3z"/>',
    fileCode: '<path d="M6 3h8l4 4v14H6V3Z"/><path d="M14 3v4h4"/><path d="m10 12-2 2.5 2 2.5"/><path d="m14 12 2 2.5-2 2.5"/>',
    filter: '<path d="M3.5 5h17l-6.5 7.5V20l-4-2.5v-5L3.5 5Z"/>',

    // ── Destek / bakım ─────────────────────────────────────────────────────
    log: '<path d="M6 3h8l4 4v14H6V3Z"/><path d="M9.5 12h5"/><path d="M9.5 15.5h5"/><path d="M9.5 8.5h2"/>',
    lifebuoy: '<circle cx="12" cy="12" r="8.5"/><circle cx="12" cy="12" r="3.5"/><path d="m6 6 3.5 3.5"/><path d="m18 6-3.5 3.5"/><path d="m6 18 3.5-3.5"/><path d="m18 18-3.5-3.5"/>',
    refresh: '<path d="M20 12a8 8 0 1 1-2.5-5.8"/><path d="M20 4v4h-4"/>',
    sun: '<circle cx="12" cy="12" r="4"/><path d="M12 2.5v2"/><path d="M12 19.5v2"/><path d="M2.5 12h2"/><path d="M19.5 12h2"/><path d="m5.5 5.5 1.5 1.5"/><path d="m17 17 1.5 1.5"/><path d="m18.5 5.5-1.5 1.5"/><path d="m7 17-1.5 1.5"/>',
    moon: '<path d="M20 14.5A8.5 8.5 0 0 1 9.5 4a8.5 8.5 0 1 0 10.5 10.5Z"/>',
    globe: '<circle cx="12" cy="12" r="8.5"/><path d="M3.5 12h17"/><path d="M12 3.5c2.5 2.5 3.5 5.5 3.5 8.5s-1 6-3.5 8.5c-2.5-2.5-3.5-5.5-3.5-8.5s1-6 3.5-8.5Z"/>',

    // ── Scalio — büyütme ───────────────────────────────────────────────────
    image: '<path d="M4 5h16v14H4z"/><circle cx="9" cy="10" r="1.6"/><path d="m6 17 4.5-5 3 3.2L16 12l4 5"/>',
    zoomPlus: '<circle cx="11" cy="11" r="6.5"/><path d="m16 16 4.5 4.5"/><path d="M11 8.5v5"/><path d="M8.5 11h5"/>',
    crop: '<path d="M6 2v14h14"/><path d="M18 22V8H4"/>',
    compare: '<path d="M12 3v18"/><path d="M4 8H2"/><path d="M4 16H2"/><path d="M22 8h-2"/><path d="M22 16h-2"/><path d="M4 5h5v14H4z"/><path d="M15 5h5v14h-5z"/>',
    grid: '<path d="M4 4h7v7H4z"/><path d="M13 4h7v7h-7z"/><path d="M4 13h7v7H4z"/><path d="M13 13h7v7h-7z"/>',
    stack: '<path d="M4 6.5h16v11H4z"/><path d="M4 6.5 7 3.5h10l3 3"/>',
};

/**
 * `data-icon` işaretli düğümlere simgeyi basar.
 *
 * ⚠️ ZATEN BASILMIŞSA TEKRAR BASILMAZ (`data-icon-done`). Dil değiştiğinde
 * `applyTranslations` yeniden çalışıyor ve bu işlev de onunla birlikte
 * çağrılıyor; kontrol olmadan her dil değişiminde ikonlar üst üste eklenirdi.
 * İkon adı değiştiyse (dosya türü satırları yeniden çizilirken) yeniden
 * basılır — karşılaştırma ad üzerinden.
 */
function applyIcons(root = document) {
    for (const el of root.querySelectorAll('[data-icon]')) {
        if (el.dataset.iconDone === el.dataset.icon) {
            continue;
        }

        const paths = ICONS[el.dataset.icon];

        if (!paths) {
            continue;
        }

        /*
         * ⚠️ `innerHTML` BURADA GÜVENLİ ve bilinçli: basılan şey yalnızca
         * yukarıdaki sabit tablodan geliyor, hiçbir kullanıcı verisi ya da
         * ağdan gelen metin buraya ulaşmıyor. Dosya adları hiçbir zaman ikon
         * olarak basılmaz.
         */
        el.innerHTML =
            `<svg class="icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" `
            + `stroke-width="1.7" stroke-linecap="square" stroke-linejoin="miter" `
            + `aria-hidden="true" focusable="false">${paths}</svg>`;

        el.dataset.iconDone = el.dataset.icon;
    }
}

/** Tek bir ikon düğümü üretir (JS ile çizilen satırlar için). */
function icon(name, extraClass = '') {
    const span = document.createElement('span');
    span.dataset.icon = name;

    if (extraClass) {
        span.className = extraClass;
    }

    applyIcons({ querySelectorAll: () => [span] });

    return span;
}
