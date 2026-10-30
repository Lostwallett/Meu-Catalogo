// Recupera a visualização preferida deste navegador (tabela ou cartões).
// O localStorage não é o banco SQL Server: guarda apenas uma preferência local.
(function () {
    const tabela = document.getElementById('visualizacaoTabela');
    const cartoes = document.getElementById('visualizacaoCartoes');
    const btnTabela = document.getElementById('btnTabela');
    const btnCartoes = document.getElementById('btnCartoes');
    if (!tabela || !cartoes || !btnTabela || !btnCartoes) return;

    function exibir(modo) {
        const mostrarCartoes = modo === 'cartoes';
        tabela.style.display = mostrarCartoes ? 'none' : 'block';
        cartoes.style.display = mostrarCartoes ? 'block' : 'none';
        btnTabela.className = mostrarCartoes ? 'btn btn-outline-primary' : 'btn btn-primary';
        btnCartoes.className = mostrarCartoes ? 'btn btn-primary' : 'btn btn-outline-primary';
        btnTabela.setAttribute('aria-pressed', String(!mostrarCartoes));
        btnCartoes.setAttribute('aria-pressed', String(mostrarCartoes));
        localStorage.setItem('visualizacaoCatalogo', mostrarCartoes ? 'cartoes' : 'tabela');
    }

    btnTabela.addEventListener('click', () => exibir('tabela'));
    btnCartoes.addEventListener('click', () => exibir('cartoes'));
    exibir(localStorage.getItem('visualizacaoCatalogo') || 'tabela');
})();
