// Experiência visual isolada: não altera itens nem a preferência tabela/cartões.
(function () {
    document.querySelectorAll('.cover-image').forEach(image => {
        function updateFallback() {
            const loaded = image.complete && image.naturalWidth > 0;
            image.hidden = !loaded;
            image.parentElement.querySelector('.cover-fallback').hidden = loaded;
            image.closest('[data-cover-candidate]')?.querySelectorAll('[data-cover-confirm]')
                .forEach(confirmation => {
                    if (!image.dataset.coverId || confirmation.dataset.coverId === image.dataset.coverId)
                        confirmation.disabled = !loaded;
                });
        }
        image.addEventListener('load', updateFallback);
        image.addEventListener('error', updateFallback);
        if (image.complete) updateFallback();
    });
    const grid = document.getElementById('gradeCartoes');
    const button = document.getElementById('btnCapas');
    if (!grid || !button) return;

    function showCovers(show) {
        grid.classList.toggle('covers-enabled', show);
        button.setAttribute('aria-pressed', String(show));
        button.textContent = show ? 'Ocultar capas' : 'Mostrar capas';
    }

    let enabled = true;
    try { enabled = localStorage.getItem('capasCatalogoExperimento') !== 'ocultas'; } catch { /* Preferência opcional. */ }
    showCovers(enabled);
    button.hidden = false;
    button.addEventListener('click', () => {
        const show = !grid.classList.contains('covers-enabled');
        showCovers(show);
        try { localStorage.setItem('capasCatalogoExperimento', show ? 'visiveis' : 'ocultas'); } catch { /* Sem persistência, o botão continua funcionando. */ }
    });

})();
