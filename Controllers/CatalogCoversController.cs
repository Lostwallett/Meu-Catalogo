using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MyCatalog.Data;
using MyCatalog.Models;
using MyCatalog.Services.Covers;

namespace MyCatalog.Controllers;

public class CatalogCoversController(ApplicationDbContext context, ICoverProvider provider,
    CoverSelectionTokens tokens, CatalogCoverService covers, IOptions<BookCoverOptions> options) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int id, string? title, string? author, bool search = false,
        string? workToken = null, int page = 1, CancellationToken cancellationToken = default)
    {
        // A flag também protege instalações cujo esquema ainda não recebeu a migration.
        if (!options.Value.Enabled) return StatusCode(503, "Capas de livros desabilitadas. Habilite somente após preparar o banco isolado.");
        var item = await context.CatalogItems.AsNoTracking().Include(x => x.Cover)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null) return NotFound();
        if (item.Type != "Livro") return BadRequest("A pesquisa está disponível somente para livros.");
        var vm = new BookCoverSearchViewModel
        {
            Item = item, Title = search ? title ?? "" : title ?? item.Title,
            Author = search ? author : author ?? item.AuthorOrDirector,
            Configured = provider.IsConfigured, Searched = search || workToken is not null,
            WorkToken = workToken, Page = page, RemoveToken = item.Cover is null ? null : tokens.ForRemoval(item)
        };
        if (!provider.IsConfigured)
        {
            vm.Message = "Configure um contato válido da Open Library para pesquisar. Nenhuma chamada externa foi realizada.";
            return View(vm);
        }
        try
        {
            if (workToken is not null)
            {
                var selection = tokens.Read(workToken, "work");
                if (selection is null || !CoverSelectionTokens.Matches(selection, item))
                {
                    vm.Message = "A pesquisa expirou ou o item foi alterado. Faça uma nova pesquisa.";
                    vm.WorkToken = null;
                    return View(vm);
                }
                if (page is < 1 or > BookCoverSearchViewModel.MaxEditionPages)
                    throw new CoverProviderException("Limite de edições carregadas atingido. Inicie outra pesquisa para refinar a escolha.");
                vm.SelectedWorkTitle = selection.WorkTitle;
                vm.SelectedWorkId = selection.WorkId;
                vm.SelectedWorkAuthors = selection.WorkAuthors;
                // Acumula as páginas já solicitadas. O cliente reutiliza o cache para
                // não repetir chamadas e a grade agrupa capas também entre páginas.
                // Limite explícito evita varrer milhares de edições ou buscar em lote.
                for (var currentPage = 1; currentPage <= page; currentPage++)
                {
                    var editions = await provider.EditionsAsync(selection.WorkId, currentPage, cancellationToken);
                    vm.Editions.AddRange(editions.Items.Select(x => (x, tokens.ForImage(item, x))));
                    vm.HasNext = editions.HasNext;
                    vm.Page = currentPage;
                    if (!editions.HasNext) break;
                }
                vm.EditionGroups = CoverEditionGrouping.Group(vm.Editions, selection.WorkTitle ?? item.Title);
            }
            else if (search)
            {
                if (!TryValidateModel(vm)) return View(vm);
                var works = await provider.SearchAsync(vm.Title, vm.Author, page, cancellationToken);
                vm.Works = works.Items.Select(x => (x, tokens.ForWork(item, x))).ToList();
                vm.HasNext = works.HasNext;
            }
        }
        catch (CoverProviderException error) { vm.Message = error.Message; }
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Select(int id, string token, CancellationToken cancellationToken)
    {
        if (!options.Value.Enabled) return StatusCode(503);
        var selection = tokens.Read(token, "image");
        if (selection is null || selection.ItemId != id) return BadRequest("Seleção inválida ou expirada. Pesquise novamente.");
        if (!await covers.SaveAsync(selection, false, cancellationToken))
            return Conflict("O item ou sua capa mudou. Reabra a pesquisa antes de confirmar.");
        TempData["Mensagem"] = "Capa associada ao livro. As demais informações foram preservadas.";
        return RedirectToAction("Details", "CatalogItems", new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int id, string token, CancellationToken cancellationToken)
    {
        if (!options.Value.Enabled) return StatusCode(503);
        var selection = tokens.Read(token, "remove");
        if (selection is null || selection.ItemId != id) return BadRequest("Solicitação inválida ou expirada.");
        if (!await covers.SaveAsync(selection, true, cancellationToken))
            return Conflict("O item ou sua capa mudou. Reabra a página.");
        TempData["Mensagem"] = "Associação da capa removida. O livro foi preservado.";
        return RedirectToAction("Details", "CatalogItems", new { id });
    }
}
