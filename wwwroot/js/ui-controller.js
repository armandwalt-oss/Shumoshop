(function () {
    'use strict';

    let activeUI = null;
    const overlay = document.getElementById('globalOverlay');

    if (!overlay) {
        console.error('❌ globalOverlay not found. Add <div id="globalOverlay" class="global-overlay"></div> near </body>.');
        return;
    }

    function openUI(name) {
        // Close whatever was open before
        if (activeUI && activeUI !== name) {
            closeUI();
        }

        activeUI = name;
        overlay.classList.add('show');
        document.body.style.overflow = 'hidden';

        document.dispatchEvent(new CustomEvent('ui:open', { detail: name }));
    }

    function closeUI() {
        if (!activeUI) return;

        const closing = activeUI;
        activeUI = null;

        overlay.classList.remove('show');
        document.body.style.overflow = '';

        document.dispatchEvent(new CustomEvent('ui:close', { detail: closing }));
    }

    overlay.addEventListener('click', closeUI);

    document.addEventListener('keydown', (e) => {
        if (e.key === 'Escape') closeUI();
    });

    window.UIController = { openUI, closeUI };
})();
