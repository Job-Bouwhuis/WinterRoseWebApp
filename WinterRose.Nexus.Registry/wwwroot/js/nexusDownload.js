// Detects the visitor's OS and emphasizes the matching download card.
// Never hides or disables the other options - only highlights the relevant one
// and, on macOS, surfaces a note that Nexus isn't supported there.

export function detectOS() {
    const ua = navigator.userAgent || '';
    const platform = navigator.platform || '';

    let os = 'unknown';

    if (/Mac|iPhone|iPad|iPod/.test(platform) || /Macintosh|iPhone|iPad/.test(ua)) {
        os = 'mac';
    } else if (/Win/.test(platform) || /Windows/.test(ua)) {
        os = 'windows';
    } else if (/Linux/.test(platform) || /Linux/.test(ua)) {
        os = 'linux';
    }

    applyOSEmphasis(os);
}

function applyOSEmphasis(os) {
    const banner = document.getElementById('osBanner');
    const bannerText = document.getElementById('osBannerText');
    const macNote = document.getElementById('macNote');
    const cardWindows = document.getElementById('cardWindows');
    const cardLinux = document.getElementById('cardLinux');

    if (!banner || !bannerText) return;

    const labels = {
        windows: 'You\u2019re on Windows \u2014 the build below is yours.',
        linux: 'You\u2019re on Linux \u2014 the build below is yours.',
        mac: 'You\u2019re on macOS.',
        unknown: 'Pick the build that matches your system.'
    };

    bannerText.textContent = labels[os] ?? labels.unknown;
    banner.dataset.os = os;

    if (os === 'windows' && cardWindows) {
        cardWindows.classList.add('is-current');
    } else if (os === 'linux' && cardLinux) {
        cardLinux.classList.add('is-current');
    }

    if (os === 'mac' && macNote) {
        macNote.hidden = false;
    }
}
