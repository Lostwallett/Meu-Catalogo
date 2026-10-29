namespace MyCatalog.Models;

// Reúne os dados necessários para a página de listagem.
// Evita espalhar informações por vários campos de ViewData.
public class CatalogIndexViewModel
{
    public List<CatalogItem> Items { get; set; } = new();
    public string? Pesquisa { get; set; }
    public string? Tipo { get; set; }
    public string Ordenacao { get; set; } = "titulo_asc";
    public bool SomenteFavoritos { get; set; }
    public int PaginaAtual { get; set; } = 1;
    public int TotalPaginas { get; set; } = 1;
    public int TotalResultados { get; set; }
    public int TotalItens { get; set; }
    public int TotalFilmes { get; set; }
    public int TotalSeries { get; set; }
    public int TotalLivros { get; set; }
    public int TotalJogos { get; set; }
    public int TotalOutros { get; set; }
    public int ItensPorPagina { get; set; } = 10;
}
