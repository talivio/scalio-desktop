/*
 * Scalio arayüzü — WebView2 köprüsü üzerinden büyütme motoruyla konuşur.
 *
 * ⚠️ BURADA HİÇBİR GÖRSEL İŞLEME YOK. Bu dosya yalnızca ekranı yönetir;
 * büyütme motoru C# tarafında ve ARKA PLAN İŞ PARÇACIĞINDA çalışır.
 *
 * Akış: ana ekran (bırak/seç → ayarlar → kuyruğa ekle → kuyruk listesi,
 * olay tabanlı güncellenir) → önce/sonra karşılaştırma bir modal.
 * Ayarlar/Yardım üst çubuktan.
 *
 * ⚠️ GİRİŞ, DENEME VE ABONELİK YOK (2026-11-04): Scalio tamamen ücretsiz,
 * hesapsız çalışan bir araç. x4, toplu işlem ve hazır boyutlar herkese açık;
 * motor zaten kullanıcının kendi ekran kartında çalışıyor.
 */


/*
 * ⚠️ Köprü yoksa SESSİZCE PATLAMAZ. Bu sayfa normal bir tarayıcıda da
 * açılabiliyor (tasarımı gözden geçirmek için) ama orada
 * `window.chrome.webview` yoktur. Önizlemede ÖRNEK VERİ üreten bir taklit
 * köprü devreye girer; her ekran gezilebilir ve bu açıkça yazılır.
 */
const hasBridge = Boolean(window.chrome?.webview);

/** Kuyruk olayı gibi istek-cevap eşleşmesi olmayan mesajları dinleyenler. */
const eventListeners = new Set();

function onEvent(fn) {
    eventListeners.add(fn);
}

function fireEvent(message) {
    for (const fn of eventListeners) {
        fn(message);
    }
}

const realBridge = () => {
    const pending = new Map();
    let counter = 0;

    window.chrome.webview.addEventListener('message', (event) => {
        const message = event.data;

        // ⚠️ Kimliksiz mesaj bir İSTEĞİN CEVABI DEĞİL, bir OLAY (kuyruk
        // durumu). Bekleyen istek eşlemesinden ayrı ele alınır.
        if (message.kind === 'event') {
            fireEvent(message);
            return;
        }

        const entry = pending.get(message.id);

        if (!entry) {
            return;
        }

        pending.delete(message.id);

        if (message.kind === 'error') {
            entry.reject(message);
        } else {
            entry.resolve(message.data);
        }
    });

    return {
        send(action, payload = {}) {
            const id = `r${++counter}`;

            return new Promise((resolve, reject) => {
                pending.set(id, { resolve, reject });
                window.chrome.webview.postMessage({ id, action, ...payload });
            });
        },
    };
};

/*
 * ÖNİZLEME KÖPRÜSÜ — yalnız sayfa uygulama dışında açıldığında.
 *
 * ⚠️ Buradaki adlar UYDURMA örnek veridir; hiçbir gerçek dosyaya bakılmaz.
 * Amaç, tasarımı tarayıcıda uçtan uca gezebilmek. Köprü varken bu nesne hiç
 * kurulmaz.
 */
const previewBridge = () => {
    const wait = (ms) => new Promise((r) => setTimeout(r, ms));

    // ⚠️ Tarayıcıda senaryo denemek için URL parametreleri: `?update=optional` /
    // `?update=forced` (açılışta güncelleme modalı).
    const params = new URLSearchParams(location.search);
    const updateParam = params.get('update');

    const previewJobs = [];

    const runPreviewJob = async (job) => {
        job.status = 'processing';
        fireEvent({ kind: 'event', event: 'jobUpdated', data: describeJob(job) });

        await wait(1400 + Math.random() * 900);

        if (job.fileName.toLowerCase().includes('fail')) {
            job.status = 'failed';
            job.error = 'Preview: simulated failure (file name contains "fail").';
        } else {
            job.status = 'completed';
            job.resultPath = job.sourcePath;
        }

        fireEvent({ kind: 'event', event: 'jobUpdated', data: describeJob(job) });
    };

    const describeJob = (job) => ({
        id: job.id,
        fileName: job.fileName,
        sourcePath: job.sourcePath,
        status: job.status,
        error: job.error ?? null,
        resultPath: job.resultPath ?? null,
    });

    let chain = Promise.resolve();

    return {
        async send(action, payload = {}) {
            await wait(80);

            switch (action) {
                case 'app_info': return { version: '0.1.0' };
                case 'open_url':
                    notify({ tone: 'info', message: 'Preview: this would open your browser.' });
                    return { opened: false };
                case 'pick_files':
                    return { paths: ['C:\\Users\\me\\Pictures\\holiday-01.jpg', 'C:\\Users\\me\\Pictures\\holiday-02.jpg'] };
                case 'pick_folder':
                    return { paths: ['C:\\Users\\me\\Pictures\\Album\\cover.jpg'] };
                case 'get_thumbnail':
                    // Önizleme modunda gerçek dosya yok; küçük bir örnek görsel döner.
                    return {
                        dataUrl: 'data:image/svg+xml;utf8,' + encodeURIComponent(
                            '<svg xmlns="http://www.w3.org/2000/svg" width="80" height="80">'
                            + '<rect width="80" height="80" fill="#0086E0"/>'
                            + '<circle cx="40" cy="32" r="12" fill="#fff" opacity="0.85"/>'
                            + '<path d="M8 68 L30 44 L48 60 L58 48 L72 68 Z" fill="#fff" opacity="0.85"/>'
                            + '</svg>',
                        ),
                    };
                case 'enqueue_job': {
                    const jobs = (payload.paths ?? []).map((path) => ({
                        id: `preview-${previewJobs.length + Math.random().toString(36).slice(2, 7)}`,
                        fileName: path.split(/[\\/]/).pop(),
                        sourcePath: path,
                        status: 'queued',
                    }));

                    previewJobs.push(...jobs);

                    for (const job of jobs) {
                        chain = chain.then(() => runPreviewJob(job));
                    }

                    return { jobs: jobs.map(describeJob) };
                }
                case 'cancel_job': {
                    const job = previewJobs.find((j) => j.id === payload.jobId);

                    if (job && job.status === 'queued') {
                        job.status = 'cancelled';
                        fireEvent({ kind: 'event', event: 'jobUpdated', data: describeJob(job) });
                    }

                    return { ok: true };
                }
                case 'get_queue_state':
                    return { jobs: previewJobs.map(describeJob) };
                case 'open_in_folder':
                    notify({ tone: 'info', message: 'Preview: this would open the file in Explorer.' });
                    return { opened: false };
                case 'create_support_bundle':
                    return { path: 'C:\\Users\\me\\AppData\\Local\\Scalio\\logs\\scalio-destek-2026-09-17.txt' };
                case 'open_log_folder':
                    return { path: 'C:\\Users\\me\\AppData\\Local\\Scalio\\logs' };
                case 'check_update': {
                    if (updateParam !== 'optional' && updateParam !== 'forced') {
                        return {
                            current: '0.2.0', latest: null, updateAvailable: false, required: false, downloadUrl: null,
                        };
                    }

                    return {
                        current: '0.1.0', latest: '0.2.0', updateAvailable: true,
                        required: updateParam === 'forced',
                        downloadUrl: SITE_URL + 'download',
                        sha256: 'ab'.repeat(32),
                        canInstall: false,
                    };
                }
                default: return {};
            }
        },
    };
};

const bridge = hasBridge ? realBridge() : previewBridge();

const $ = (id) => document.getElementById(id);

const state = {
    jobs: [],
    mode: 'scale',
    scale: 2,
    model: 'photo',
    format: 'png',
    quality: 90,
    cropPreset: '',
    pendingFiles: [], // { path, fileName } henüz kuyruğa eklenmemiş
};

// ── Yardımcılar ─────────────────────────────────────────────────────────────

function el(tag, className, text) {
    const node = document.createElement(tag);

    if (className) {
        node.className = className;
    }

    if (text !== undefined) {
        node.textContent = text;
    }

    return node;
}

function button(label, { icon: iconName, className = 'cut-sm', onClick, title } = {}) {
    const btn = el('button', className);

    if (iconName) {
        btn.append(icon(iconName));
    }

    btn.append(document.createTextNode(label));

    if (title) {
        btn.title = title;
    }

    if (onClick) {
        btn.addEventListener('click', onClick);
    }

    return btn;
}

function fileName(path) {
    return path.split(/[\\/]/).pop();
}

// ── Bildirimler ──────────────────────────────────────────────────────────────

function notify({ tone = 'info', title, message, actions = [] }) {
    const host = $('notice-host');
    const box = el('div', `notice cut notice-${tone === 'error' ? 'bad' : tone === 'ok' ? 'ok' : tone === 'warn' ? 'warn' : 'info'}`);

    box.append(icon(tone === 'error' ? 'alert' : tone === 'ok' ? 'checkCircle' : tone === 'warn' ? 'alert' : 'info', 'notice-icon'));

    const body = el('div', 'notice-body');

    if (title) {
        body.append(el('strong', null, title));
    }

    body.append(document.createTextNode(message ?? ''));

    if (actions.length > 0) {
        const row = el('div', 'notice-actions');

        for (const action of actions) {
            row.append(button(action.label, { className: action.primary ? 'btn-primary cut-sm' : 'cut-sm', onClick: () => { action.onClick(); box.remove(); } }));
        }

        body.append(row);
    }

    box.append(body);
    box.append(button('', { icon: 'close', className: 'notice-close', onClick: () => box.remove(), title: t('app.dismiss') }));

    host.append(box);
    applyIcons(box);
}

function fail(error, fallbackKey) {
    notify({ tone: 'error', message: error?.message || t(fallbackKey) });
}

// ── Ekran gösterimi ──────────────────────────────────────────────────────────

function show(screenId) {
    for (const section of document.querySelectorAll('.screen')) {
        section.classList.toggle('hidden', section.id !== screenId);
    }

    for (const item of document.querySelectorAll('.nav-item')) {
        item.classList.toggle('nav-active', item.dataset.nav === screenId);
    }
}

// ── Modal (güncelleme) ────────────────────────────────────────────────

let modalEscapeHandler = null;

function closeAllModals() {
    $('update-modal').classList.add('hidden');
    modalEscapeHandler = null;
}

/**
 * Aynı anda en fazla BİR modal açık olur — yeni modal açılırken öteki kapanır
 * (güncelleme ve ödeme kilidi aynı anda görünmez, bkz. `boot`).
 */
function openModal(id, onEscape = null) {
    closeAllModals();

    const modal = $(id);
    modal.classList.remove('hidden');
    modalEscapeHandler = onEscape;

    modal.querySelector('.modal button:not(.hidden)')?.focus();
}

function closeModal(id) {
    $(id).classList.add('hidden');
    modalEscapeHandler = null;
}

document.addEventListener('keydown', (event) => {
    if (event.key === 'Escape' && modalEscapeHandler) {
        event.preventDefault();
        modalEscapeHandler();
    }
});

// ── Etkinlik göstergesi ────────────────────────────────────────────────────

function setActivity(textKey) {
    const bar = $('activity');

    if (!textKey) {
        bar.classList.add('hidden');
        return;
    }

    $('activity-text').textContent = t(textKey);
    bar.classList.remove('hidden');
}

// ── Açılış ────────────────────────────────────────────────────────────────

async function boot() {
    applyTranslations();

    // ⚠️ Güncelleme denetimi AÇILIŞI BEKLETMEZ: ekran önce çizilir, modal
    // (varsa) sonuç gelince açılır.
    const updatePromise = checkUpdate();

    await enterApp();

    const updateStatus = await updatePromise;

    if (updateStatus?.updateAvailable) {
        maybeShowUpdateModal(updateStatus);
    }
}

async function enterApp() {
    $('nav').classList.remove('hidden');

    try {
        const info = await bridge.send('app_info');
        $('version-line').textContent = t('help.version_line', info.version || '0.0.0');
    } catch { /* sürüm satırı checkUpdate'ten de dolar */ }

    show('main');
    updateEnqueueEnabled();

    await refreshQueueState();
}

// ── Gezinme ──────────────────────────────────────────────────────────────

for (const item of document.querySelectorAll('.nav-item')) {
    item.addEventListener('click', () => show(item.dataset.nav));
}

document.querySelectorAll('[data-scale]').forEach((btn) => {
    btn.addEventListener('click', () => {
        setScale(Number(btn.dataset.scale));
    });
});

function setScale(value) {
    state.scale = value;

    for (const btn of document.querySelectorAll('[data-scale]')) {
        btn.setAttribute('aria-checked', String(Number(btn.dataset.scale) === value));
        btn.classList.toggle('seg-active', Number(btn.dataset.scale) === value);
    }
}

$('crop-preset').addEventListener('change', () => {
    state.cropPreset = $('crop-preset').value;
});

// ── Ayarlar paneli ────────────────────────────────────────────────────────

$('mode-scale').addEventListener('click', () => setMode('scale'));
$('mode-width').addEventListener('click', () => setMode('width'));

function setMode(mode) {
    state.mode = mode;
    $('mode-scale').setAttribute('aria-checked', String(mode === 'scale'));
    $('mode-scale').classList.toggle('seg-active', mode === 'scale');
    $('mode-width').setAttribute('aria-checked', String(mode === 'width'));
    $('mode-width').classList.toggle('seg-active', mode === 'width');
    $('scale-options').classList.toggle('hidden', mode !== 'scale');
    $('width-input-row').classList.toggle('hidden', mode !== 'width');
}

document.querySelectorAll('[data-model]').forEach((btn) => {
    btn.addEventListener('click', () => {
        state.model = btn.dataset.model;

        for (const b of document.querySelectorAll('[data-model]')) {
            b.setAttribute('aria-checked', String(b === btn));
            b.classList.toggle('seg-active', b === btn);
        }
    });
});

document.querySelectorAll('[data-format]').forEach((btn) => {
    btn.addEventListener('click', () => {
        state.format = btn.dataset.format;

        for (const b of document.querySelectorAll('[data-format]')) {
            b.setAttribute('aria-checked', String(b === btn));
            b.classList.toggle('seg-active', b === btn);
        }

        $('quality-row').classList.toggle('hidden', state.format === 'png');
    });
});

$('quality').addEventListener('input', () => {
    state.quality = Number($('quality').value);
    $('quality-value').textContent = String(state.quality);
});

// ── Dosya ekleme ─────────────────────────────────────────────────────────

const dropzone = $('dropzone');

dropzone.addEventListener('click', () => pickFiles());
dropzone.addEventListener('keydown', (event) => {
    if (event.key === 'Enter' || event.key === ' ') {
        event.preventDefault();
        pickFiles();
    }
});

$('pick-files').addEventListener('click', (event) => { event.stopPropagation(); pickFiles(); });
$('pick-folder').addEventListener('click', (event) => { event.stopPropagation(); pickFolder(); });

async function pickFiles() {
    try {
        const result = await bridge.send('pick_files');
        addPendingFiles(result.paths ?? []);
    } catch (error) {
        fail(error, 'main.pick_failed');
    }
}

async function pickFolder() {
    try {
        const result = await bridge.send('pick_folder');
        addPendingFiles(result.paths ?? []);
    } catch (error) {
        fail(error, 'main.pick_failed');
    }
}

['dragenter', 'dragover'].forEach((eventName) => {
    dropzone.addEventListener(eventName, (event) => {
        event.preventDefault();
        dropzone.classList.add('drop-active');
    });
});

['dragleave', 'drop'].forEach((eventName) => {
    dropzone.addEventListener(eventName, (event) => {
        event.preventDefault();
        dropzone.classList.remove('drop-active');
    });
});

dropzone.addEventListener('drop', (event) => {
    const files = [...(event.dataTransfer?.files ?? [])];

    // ⚠️ `file.path` yalnız WebView2 içinde dolu gelir (yerel sürükle-bırak).
    // Düz bir tarayıcıda (önizleme) bu alan yok; önizlemede dosya adı kaynak
    // yolu yerine geçer, çünkü gerçek bir diske zaten bakılmıyor.
    const paths = files.map((file) => file.path || file.name).filter(Boolean);

    if (paths.length > 0) {
        // Sürüklenen dosyalar gerçek `File` nesneleri: köprüye hiç gitmeden
        // `URL.createObjectURL` ile anında önizleme üretilebilir.
        addPendingFiles(paths, files);
    }
});

function addPendingFiles(paths, droppedFiles) {
    for (let i = 0; i < paths.length; i++) {
        const path = paths[i];
        const entry = { path, fileName: fileName(path), previewUrl: null };

        state.pendingFiles.push(entry);

        const dropped = droppedFiles?.[i];

        if (dropped) {
            // Sürüklenen dosya: bayt zaten tarayıcıda, köprüye gitmeye gerek yok.
            entry.previewUrl = URL.createObjectURL(dropped);
        } else {
            // Seçici ile eklenen dosya: yalnız yol bilgisi var, küçük bir
            // önizleme için köprüye sorulur. Başarısız olursa (bozuk dosya vb.)
            // genel simge kalır — kuyruğa eklemeyi ENGELLEMEZ.
            bridge.send('get_thumbnail', { path })
                .then((result) => {
                    entry.previewUrl = result.dataUrl;
                    renderPendingPreviews();
                })
                .catch(() => {});
        }
    }

    updateEnqueueEnabled();
    renderPendingPreviews();
}

/**
 * Ayarlanmayı bekleyen görselleri yükleme alanının içinde küçük kartlar
 * olarak çizer — "N görsel eklendi" bildiriminin YERİNE geçer.
 */
function renderPendingPreviews() {
    const host = $('pending-previews');
    const hasPending = state.pendingFiles.length > 0;

    host.replaceChildren();
    host.classList.toggle('hidden', !hasPending);
    dropzone.classList.toggle('has-pending', hasPending);

    state.pendingFiles.forEach((entry, index) => {
        const thumb = el('div', 'pending-thumb');
        thumb.title = entry.fileName;

        if (entry.previewUrl) {
            const img = el('img');
            img.src = entry.previewUrl;
            img.alt = entry.fileName;
            thumb.append(img);
        } else {
            const placeholder = el('div', 'pending-thumb-icon');
            placeholder.append(icon('image'));
            thumb.append(placeholder);
        }

        const remove = el('button', 'pending-thumb-remove');
        remove.type = 'button';
        remove.title = t('main.remove_pending');
        remove.setAttribute('aria-label', t('main.remove_pending'));
        remove.append(icon('close'));
        remove.addEventListener('click', (event) => {
            event.stopPropagation();
            removePendingFile(index);
        });
        thumb.append(remove);

        host.append(thumb);
    });
}

function removePendingFile(index) {
    const [removed] = state.pendingFiles.splice(index, 1);

    if (removed?.previewUrl?.startsWith('blob:')) {
        URL.revokeObjectURL(removed.previewUrl);
    }

    updateEnqueueEnabled();
    renderPendingPreviews();
}

function updateEnqueueEnabled() {
    $('enqueue').disabled = state.pendingFiles.length === 0;
}

$('enqueue').addEventListener('click', async () => {
    if (state.pendingFiles.length === 0) {
        return;
    }

    const payload = {
        paths: state.pendingFiles.map((f) => f.path),
        model: state.model,
        format: state.format,
        quality: state.format === 'png' ? undefined : state.quality,
        cropPreset: state.cropPreset,
    };

    if (state.mode === 'scale') {
        payload.scale = state.scale;
    } else {
        payload.targetWidth = Number($('target-width').value) || 2000;
    }

    try {
        const result = await bridge.send('enqueue_job', payload);

        for (const job of result.jobs ?? []) {
            upsertJob(job);
        }

        for (const entry of state.pendingFiles) {
            if (entry.previewUrl?.startsWith('blob:')) {
                URL.revokeObjectURL(entry.previewUrl);
            }
        }

        state.pendingFiles = [];
        renderPendingPreviews();
        updateEnqueueEnabled();
    } catch (error) {
        fail(error, 'main.enqueue_failed');
    }
});

// ── Kuyruk ───────────────────────────────────────────────────────────────

onEvent((message) => {
    if (message.event === 'jobUpdated' || message.event === 'jobAdded') {
        upsertJob(message.data);
    }
});

async function refreshQueueState() {
    try {
        const result = await bridge.send('get_queue_state');

        for (const job of result.jobs ?? []) {
            upsertJob(job);
        }
    } catch {
        // Kuyruk durumu okunamadı — sessizce geçilir, olaylar zaten akıyor.
    }
}

function upsertJob(job) {
    const index = state.jobs.findIndex((j) => j.id === job.id);

    if (index === -1) {
        state.jobs.unshift(job);
    } else {
        state.jobs[index] = job;
    }

    renderQueue();
}

const statusMeta = {
    queued: { icon: 'clock', tone: '', labelKey: 'queue.status_queued' },
    processing: { icon: null, tone: '', labelKey: 'queue.status_processing' },
    completed: { icon: 'checkCircle', tone: 'tone-ok', labelKey: 'queue.status_completed' },
    failed: { icon: 'alert', tone: 'tone-bad', labelKey: 'queue.status_failed' },
    cancelled: { icon: 'close', tone: '', labelKey: 'queue.status_cancelled' },
};

function renderQueue() {
    const list = $('queue-list');
    const empty = $('queue-empty');

    empty.classList.toggle('hidden', state.jobs.length > 0);
    list.classList.toggle('hidden', state.jobs.length === 0);
    list.textContent = '';

    const anyProcessing = state.jobs.some((j) => j.status === 'processing' || j.status === 'queued');
    setActivity(anyProcessing ? 'app.activity_upscaling' : null);

    for (const job of state.jobs) {
        list.append(renderQueueItem(job));
    }

    applyIcons(list);
}

function renderQueueItem(job) {
    const item = el('li', 'queue-item');

    const thumb = el('div', 'queue-thumb');
    thumb.append(icon('image'));
    item.append(thumb);

    const body = el('div', 'queue-body');
    body.append(el('span', 'queue-name', job.fileName));

    const meta = statusMeta[job.status] ?? statusMeta.queued;
    const status = el('span', `queue-status ${meta.tone}`);

    if (job.status === 'processing') {
        status.append(el('span', 'spinner'));
    } else if (meta.icon) {
        status.append(icon(meta.icon));
    }

    const statusText = el('span', null, job.status === 'failed' && job.error ? job.error : t(meta.labelKey));
    status.append(statusText);
    body.append(status);
    item.append(body);

    const actions = el('div', 'queue-actions');

    if (job.status === 'queued' || job.status === 'processing') {
        actions.append(button('', {
            icon: 'close',
            title: t('queue.cancel'),
            onClick: () => bridge.send('cancel_job', { jobId: job.id }),
        }));
    }

    if (job.status === 'completed') {
        actions.append(button('', {
            icon: 'folderOpen',
            title: t('queue.show_folder'),
            onClick: () => bridge.send('open_in_folder', { jobId: job.id }),
        }));

        actions.append(button('', {
            icon: 'compare',
            title: t('queue.compare'),
            onClick: () => openCompare(job),
        }));
    }

    item.append(actions);

    return item;
}

// ── Önce / sonra karşılaştırma ─────────────────────────────────────────────

function openCompare(job) {
    $('compare-title').textContent = job.fileName;
    $('compare-before').src = '';
    $('compare-after').src = '';
    $('compare-overlay').classList.remove('hidden');
    setCompareSplit(50);
}

$('compare-close').onclick = () => $('compare-overlay').classList.add('hidden');

$('compare-overlay').addEventListener('click', (event) => {
    if (event.target === $('compare-overlay')) {
        $('compare-overlay').classList.add('hidden');
    }
});

function setCompareSplit(percent) {
    const clamped = Math.min(100, Math.max(0, percent));
    const wrap = $('compare-before-wrap');
    wrap.style.width = `${clamped}%`;
    $('compare-before').style.width = `${(100 / clamped) * 100 || 0}%`;
    $('compare-handle').style.left = `${clamped}%`;
}

let dragging = false;

$('compare-handle').addEventListener('mousedown', () => { dragging = true; });
window.addEventListener('mouseup', () => { dragging = false; });
$('compare-stage').addEventListener('mousedown', (event) => {
    dragging = true;
    updateCompareFromEvent(event);
});

window.addEventListener('mousemove', (event) => {
    if (dragging) {
        updateCompareFromEvent(event);
    }
});

function updateCompareFromEvent(event) {
    const rect = $('compare-stage').getBoundingClientRect();
    const percent = ((event.clientX - rect.left) / rect.width) * 100;
    setCompareSplit(percent);
}

// ── Ayarlar / Yardım ───────────────────────────────────────────────────────

async function checkUpdate({ announce = false } = {}) {
    let status;

    try {
        status = await bridge.send('check_update');
    } catch {
        // Denetim başarısızlığı kullanıcının sorunu değil; sessizce geçilir.
        return undefined;
    }

    $('version-line').textContent = t('help.version_line', status.current);

    if (!status.updateAvailable) {
        $('update-banner').classList.add('hidden');
        $('update-open-settings').classList.add('hidden');

        if (announce) {
            notify({ tone: 'ok', message: t('update.current', status.current) });
        }

        return status;
    }

    $('update-text').textContent = t('update.available', status.latest, status.current);
    $('update-banner').classList.remove('hidden');
    $('update-open-settings').classList.remove('hidden');

    const open = () => bridge.send('open_url', { url: status.downloadUrl });
    $('update-open').onclick = open;
    $('update-open-settings').onclick = open;

    return status;
}

$('check-update').onclick = () => checkUpdate({ announce: true });

/**
 * Açılış güncelleme modalı. Zorunlu sürümde "Sonra" yok ve Escape kapatmaz;
 * gönüllü sürümde aynı sürüm için bir kez "Sonra" denince bir daha
 * sorulmaz.
 *
 * ⚠️ Scalio'da uygulama içi kurulum akışı henüz bağlı değil (bkz.
 * `UpdateChecker`/`UpdateInstaller` — köprüde `updateDownload`/
 * `updateInstall` eylemi yok). "Güncelle" bu yüzden mevcut akışla aynı
 * şeyi yapar: indirmeyi tarayıcıda açar.
 */
function maybeShowUpdateModal(status) {
    if (!status.required) {
        let dismissed = null;

        try {
            dismissed = localStorage.getItem('scalio.updateDismissed');
        } catch { /* yoksay */ }

        if (dismissed === status.latest) {
            return;
        }
    }

    $('update-modal-title').textContent = t('update.modal_title', status.latest);
    $('update-modal-body').textContent = status.required
        ? t('update.required_body')
        : t('update.modal_body', status.current);
    $('update-modal-install-label').textContent = t('update.download_browser');

    $('update-modal-install').onclick = () => bridge.send('open_url', { url: status.downloadUrl });

    $('update-modal-later').classList.toggle('hidden', Boolean(status.required));

    const dismiss = () => {
        closeModal('update-modal');

        if (!status.required) {
            try {
                localStorage.setItem('scalio.updateDismissed', status.latest);
            } catch { /* yoksay */ }
        }
    };

    $('update-modal-later').onclick = dismiss;

    openModal('update-modal', status.required ? null : dismiss);
}

$('support-bundle').onclick = async () => {
    try {
        const result = await bridge.send('create_support_bundle');
        $('support-result').textContent = t('support.written', result.path);
        $('support-result').classList.remove('hidden');
    } catch {
        notify({ tone: 'error', message: t('support.failed') });
    }
};

/*
 * ⚠️ Ayarlar artık bir nav sekmesi DEĞİL, sağ üstteki çark ikonu. `show()`
 * nav vurgusunu `.nav-item` üzerinden yaptığı için çark düğmesi vurgulanmaz;
 * ekran geçişi aynı mantıkla çalışır.
 */
$('nav-settings-gear').onclick = () => show('help');

$('open-logs').onclick = () => bridge.send('open_log_folder');
$('open-site').onclick = () => bridge.send('open_url', { url: SITE_URL });

// ── Dil ve tema ────────────────────────────────────────────────────────────

/**
 * Dil seçimi Ayarlar ekranındaki "Görünüm" kartında, tema ile AYNI desende
 * (segmented radiogroup) duruyor — üst çubuktaki eski açılır liste kaldırıldı.
 */
function populateLanguageOptions() {
    const host = $('lang-seg');
    host.textContent = '';

    for (const code of LANGUAGES) {
        const btn = el('button', null, LANGUAGE_NAMES[code] ?? code);
        btn.dataset.lang = code;
        btn.setAttribute('role', 'radio');
        btn.addEventListener('click', () => changeLanguage(code));
        host.append(btn);
    }

    markLanguage();
}

function markLanguage() {
    const active = currentLocale();

    for (const btn of $('lang-seg').querySelectorAll('[data-lang]')) {
        btn.classList.toggle('seg-active', btn.dataset.lang === active);
        btn.setAttribute('aria-checked', String(btn.dataset.lang === active));
    }
}

function changeLanguage(code) {
    setLanguage(code);
    markLanguage();
    renderQueue();
}

const THEME_KEY = 'scalio.theme';

function applyTheme(preference) {
    const root = document.documentElement;
    const system = window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
    const effective = preference === 'system' ? system : preference;

    root.classList.toggle('dark', effective === 'dark');

    for (const btn of document.querySelectorAll('[data-theme]')) {
        btn.classList.toggle('seg-active', btn.dataset.theme === preference);
    }

    try {
        localStorage.setItem(THEME_KEY, preference);
    } catch {
        // Depolama kapalıysa tema yine uygulanır, yalnızca hatırlanmaz.
    }
}

function storedTheme() {
    try {
        return localStorage.getItem(THEME_KEY) ?? 'system';
    } catch {
        return 'system';
    }
}

document.querySelectorAll('[data-theme]').forEach((btn) => {
    btn.addEventListener('click', () => applyTheme(btn.dataset.theme));
});

// ── Önizleme uyarısı ───────────────────────────────────────────────────────

if (!hasBridge) {
    notify({ tone: 'info', message: t('preview.note') });
}

// ── Başlangıç ────────────────────────────────────────────────────────────

setLanguage(initialLanguage());
populateLanguageOptions();
applyTheme(storedTheme());
boot();
