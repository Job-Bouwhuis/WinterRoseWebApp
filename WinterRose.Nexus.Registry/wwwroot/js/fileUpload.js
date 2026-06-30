export function triggerClick(elementId) {
    const el = document.getElementById(elementId);
    if (el) el.click();
}

export function getRelativePaths(elementId) {
    const el = document.getElementById(elementId);
    if (!el || !el.files) return [];
    return Array.from(el.files).map(f => f.webkitRelativePath || f.name);
}

export function setupDropZone(dropZoneId, inputId) {
    const dropZone = document.getElementById(dropZoneId);
    const input = document.getElementById(inputId);
    if (!dropZone || !input) return;

    const prevent = e => { e.preventDefault(); e.stopPropagation(); };

    ['dragenter', 'dragover', 'dragleave', 'drop'].forEach(evt =>
        dropZone.addEventListener(evt, prevent));

    dropZone.addEventListener('dragover', () => dropZone.classList.add('dragover'));
    dropZone.addEventListener('dragleave', () => dropZone.classList.remove('dragover'));

    dropZone.addEventListener('drop', e => {
        dropZone.classList.remove('dragover');

        const dt = new DataTransfer();

        // Files dropped directly (works for files; folder drop needs the
        // webkitGetAsEntry traversal below for full relative-path support)
        if (e.dataTransfer.items && e.dataTransfer.items.length > 0 &&
            typeof e.dataTransfer.items[0].webkitGetAsEntry === 'function') {
            collectEntries(e.dataTransfer.items, dt).then(() => {
                input.files = dt.files;
                input.dispatchEvent(new Event('change', { bubbles: true }));
            });
        } else {
            for (const file of e.dataTransfer.files) dt.items.add(file);
            input.files = dt.files;
            input.dispatchEvent(new Event('change', { bubbles: true }));
        }
    });
}

async function collectEntries(items, dt) {
    const entries = Array.from(items).map(i => i.webkitGetAsEntry()).filter(Boolean);
    const files = [];

    async function walk(entry, path) {
        if (entry.isFile) {
            const file = await new Promise((res, rej) => entry.file(res, rej));
            // Patch relative path so getRelativePaths() keeps working
            Object.defineProperty(file, 'webkitRelativePath', {
                value: path + entry.name,
                writable: false
            });
            files.push(file);
        } else if (entry.isDirectory) {
            const reader = entry.createReader();
            const children = await new Promise((res, rej) => reader.readEntries(res, rej));
            for (const child of children) {
                await walk(child, path + entry.name + '/');
            }
        }
    }

    for (const entry of entries) {
        await walk(entry, '');
    }

    for (const f of files) dt.items.add(f);
}