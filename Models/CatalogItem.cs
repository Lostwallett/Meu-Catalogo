using System.ComponentModel.DataAnnotations;

namespace MyCatalog.Models;

// Cada objeto desta classe representa um item do catálogo.
public class CatalogItem
{
    public int Id { get; set; }

    public CatalogItemCover? Cover { get; set; }

    [Required(ErrorMessage = "Informe o título.")]
    [StringLength(150)]
    [Display(Name = "Título")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Selecione um tipo.")]
    [Display(Name = "Tipo")]
    public string Type { get; set; } = string.Empty;

    [StringLength(150)]
    [Display(Name = "Autor ou diretor")]
    public string? AuthorOrDirector { get; set; }

    [Range(1800, 2200, ErrorMessage = "Informe um ano válido.")]
    [Display(Name = "Ano de lançamento")]
    public int? ReleaseYear { get; set; }

    [StringLength(100)]
    [Display(Name = "Gênero")]
    public string? Genre { get; set; }

    [StringLength(2000)]
    [Display(Name = "Descrição")]
    public string? Description { get; set; }

    // false = item comum; true = item favorito.
    [Display(Name = "Favorito")]
    public bool IsFavorite { get; set; } = false;
}
