using System.ComponentModel.DataAnnotations;
using MyCatalog.Services.Covers;

namespace MyCatalog.Models;

public class BookCoverSearchViewModel
{
    public const int MaxEditionPages = 10;
    [Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidateNever]
    public CatalogItem Item { get; set; } = new();
    [Required, StringLength(150, MinimumLength = 2)] public string Title { get; set; } = string.Empty;
    [StringLength(150)] public string? Author { get; set; }
    public bool Configured { get; set; }
    public bool Searched { get; set; }
    public string? Message { get; set; }
    public string? WorkToken { get; set; }
    public string? SelectedWorkTitle { get; set; }
    public string? SelectedWorkId { get; set; }
    public string? SelectedWorkAuthors { get; set; }
    public string? RemoveToken { get; set; }
    public int Page { get; set; } = 1;
    public bool HasNext { get; set; }
    public List<(BookWork Work, string Token)> Works { get; set; } = [];
    public List<(CoverCandidate Cover, string Token)> Editions { get; set; } = [];
    public IReadOnlyList<CoverEditionGroup> EditionGroups { get; set; } = [];
}
