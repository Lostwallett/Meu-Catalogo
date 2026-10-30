using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyCatalog.Data;
using MyCatalog.Models;
using MyCatalog.Services;

namespace MyCatalog.Controllers;

public class CatalogItemsController : Controller
{
    private readonly ApplicationDbContext _context;
    private const int ItensPorPagina = 10;

    public CatalogItemsController(ApplicationDbContext context) => _context = context;

    // GET: /CatalogItems
    // Aplica pesquisa, tipo, favoritos e ordenação ANTES da paginação.
    // Também calcula as estatísticas do catálogo completo.
    public async Task<IActionResult> Index(
        string? pesquisa, string? tipo, string? ordenacao,
        bool somenteFavoritos = false, int pagina = 1)
    {
        pesquisa = pesquisa?.Trim();
        var query = FilteredItems(pesquisa, tipo, ordenacao, somenteFavoritos);

        int totalResultados = await query.CountAsync();
        int totalPaginas = Math.Max(1, (int)Math.Ceiling(totalResultados / (double)ItensPorPagina));
        pagina = Math.Clamp(pagina, 1, totalPaginas);

        // Skip ignora registros das páginas anteriores; Take limita a página atual.
        var itens = await query.Skip((pagina - 1) * ItensPorPagina)
                               .Take(ItensPorPagina).ToListAsync();

        var todos = _context.CatalogItems.AsNoTracking();
        var vm = new CatalogIndexViewModel
        {
            Items = itens,
            Pesquisa = pesquisa,
            Tipo = tipo,
            Ordenacao = ordenacao is "titulo_desc" or "ano_desc" or "ano_asc" ? ordenacao : "titulo_asc",
            SomenteFavoritos = somenteFavoritos,
            PaginaAtual = pagina,
            TotalPaginas = totalPaginas,
            TotalResultados = totalResultados,
            TotalItens = await todos.CountAsync(),
            TotalFilmes = await todos.CountAsync(x => x.Type == "Filme"),
            TotalSeries = await todos.CountAsync(x => x.Type == "Série"),
            TotalLivros = await todos.CountAsync(x => x.Type == "Livro"),
            TotalJogos = await todos.CountAsync(x => x.Type == "Jogo"),
            TotalOutros = await todos.CountAsync(x => x.Type == "Outro"),
            ItensPorPagina = ItensPorPagina
        };

        return View(vm);
    }

    // A listagem e o CSV compartilham os mesmos filtros e a ordenação.
    private IQueryable<CatalogItem> FilteredItems(
        string? pesquisa, string? tipo, string? ordenacao, bool somenteFavoritos)
    {
        var query = _context.CatalogItems.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(pesquisa))
        {
            pesquisa = pesquisa.Trim();
            query = query.Where(item => item.Title.Contains(pesquisa));
        }

        if (!string.IsNullOrWhiteSpace(tipo))
            query = query.Where(item => item.Type == tipo);

        if (somenteFavoritos)
            query = query.Where(item => item.IsFavorite);

        // A ordenação estável é importante para evitar itens repetidos entre páginas.
        query = ordenacao switch
        {
            "titulo_desc" => query.OrderByDescending(x => x.Title).ThenBy(x => x.Id),
            "ano_desc" => query.OrderBy(x => x.ReleaseYear == null)
                               .ThenByDescending(x => x.ReleaseYear).ThenBy(x => x.Title).ThenBy(x => x.Id),
            "ano_asc" => query.OrderBy(x => x.ReleaseYear == null)
                              .ThenBy(x => x.ReleaseYear).ThenBy(x => x.Title).ThenBy(x => x.Id),
            _ => query.OrderBy(x => x.Title).ThenBy(x => x.Id)
        };

        return query;
    }

    [HttpGet]
    public async Task<IActionResult> ExportCsv(
        string? pesquisa, string? tipo, string? ordenacao, bool somenteFavoritos = false)
    {
        var items = await FilteredItems(pesquisa, tipo, ordenacao, somenteFavoritos)
            .ToListAsync(HttpContext.RequestAborted);
        return File(CatalogCsvExporter.Export(items), "text/csv; charset=utf-8", "MyCatalog.csv");
    }

    // GET: mostra os detalhes de um item pelo ID.
    public async Task<IActionResult> Details(int? id)
    {
        if (id is null) return NotFound();
        var item = await _context.CatalogItems.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        return item is null ? NotFound() : View(item);
    }

    // GET: abre o formulário de cadastro.
    public IActionResult Create() => View(new CatalogItem());

    // POST: valida o formulário e grava um novo registro.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Title,Type,AuthorOrDirector,ReleaseYear,Genre,Description,IsFavorite")] CatalogItem item)
    {
        if (!ModelState.IsValid) return View(item);
        _context.CatalogItems.Add(item);
        await _context.SaveChangesAsync();
        TempData["Mensagem"] = "Item cadastrado com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    // GET: carrega o item existente para edição.
    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null) return NotFound();
        var item = await _context.CatalogItems.FindAsync(id);
        return item is null ? NotFound() : View(item);
    }

    // POST: altera somente os campos permitidos no item encontrado.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Type,AuthorOrDirector,ReleaseYear,Genre,Description,IsFavorite")] CatalogItem form)
    {
        if (id != form.Id) return NotFound();
        if (!ModelState.IsValid) return View(form);
        var item = await _context.CatalogItems.FindAsync(id);
        if (item is null) return NotFound();
        item.Title = form.Title;
        item.Type = form.Type;
        item.AuthorOrDirector = form.AuthorOrDirector;
        item.ReleaseYear = form.ReleaseYear;
        item.Genre = form.Genre;
        item.Description = form.Description;
        item.IsFavorite = form.IsFavorite;
        await _context.SaveChangesAsync();
        TempData["Mensagem"] = "Item atualizado com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    // GET: pede confirmação antes de excluir.
    public async Task<IActionResult> Delete(int? id)
    {
        if (id is null) return NotFound();
        var item = await _context.CatalogItems.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        return item is null ? NotFound() : View(item);
    }

    // POST: exclui o item após a confirmação do usuário.
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var item = await _context.CatalogItems.FindAsync(id);
        if (item is null) return NotFound();
        _context.CatalogItems.Remove(item);
        await _context.SaveChangesAsync();
        TempData["Mensagem"] = "Item excluído com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    // POST: inverte o estado do favorito e grava no banco.
    // Os parâmetros opcionais permitem retornar à mesma pesquisa/página.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleFavorite(
        int id, string? pesquisa, string? tipo, string? ordenacao,
        bool somenteFavoritos = false, int pagina = 1)
    {
        var item = await _context.CatalogItems.FindAsync(id);
        if (item is null) return NotFound();
        item.IsFavorite = !item.IsFavorite;
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index), new { pesquisa, tipo, ordenacao, somenteFavoritos, pagina });
    }
}
