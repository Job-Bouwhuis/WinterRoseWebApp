// Drives the three visual states of the right-edge nav rail:
//   default   - icons only, flush to the edge
//   nx-near   - cursor is approaching the right edge, rail nudges out slightly
//   nx-active - cursor is directly over the rail, fully expanded
//
// Only runs on pointer-capable / hover-capable layouts; mobile uses the
// hamburger toggle exclusively and never reads this.

const NEAR_THRESHOLD_PX = 350;

export function initNavRail() {
    const rail = document.getElementById('nxRail');
    if (!rail) return;
    if (!window.matchMedia('(hover: hover) and (pointer: fine)').matches) {
        return; // touch device - skip proximity behavior entirely
    }

    let isNear = false;

    const handleMouseMove = (e) => {
        const distanceFromLeftEdge = e.clientX;

        if (!isNear && distanceFromLeftEdge <= NEAR_THRESHOLD_PX) {
            isNear = true;
            rail.classList.add('nx-near');
        }

        if (isNear && distanceFromLeftEdge > NEAR_THRESHOLD_PX + 80) {
            isNear = false;
            rail.classList.remove('nx-near');
        }
    };

    rail.addEventListener('mouseenter', () => rail.classList.add('nx-active'));
    rail.addEventListener('mouseleave', () => rail.classList.remove('nx-active'));

    document.addEventListener('mousemove', handleMouseMove);
}
