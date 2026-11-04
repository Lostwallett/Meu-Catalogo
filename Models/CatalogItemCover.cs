using System.ComponentModel.DataAnnotations;

namespace MyCatalog.Models;

public class CatalogItemCover
{
    public int CatalogItemId { get; set; }
    [MaxLength(40)] public string Provider { get; set; } = string.Empty;
    [MaxLength(80)] public string ExternalItemId { get; set; } = string.Empty;
    [MaxLength(80)] public string ExternalImageId { get; set; } = string.Empty;
    [MaxLength(80)] public string? ExternalWorkId { get; set; }
    // Concorrência otimista sem depender de um recurso exclusivo do SQL Server.
    public Guid Version { get; set; } = Guid.NewGuid();
}
