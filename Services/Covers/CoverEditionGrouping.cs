namespace MyCatalog.Services.Covers;

public record CoverEditionGroup(long CoverId, IReadOnlyList<(CoverCandidate Cover, string Token)> Editions)
{
    public (CoverCandidate Cover, string Token) Primary => Editions[0];
    public int ImageCount => Editions.Select(x => x.Cover.CoverId).Distinct().Count();
    public int EditionCount => Editions.Select(x => x.Cover.EditionId).Distinct().Count();
}

public static class CoverEditionGrouping
{
    public static IReadOnlyList<CoverEditionGroup> Group(
        IEnumerable<(CoverCandidate Cover, string Token)> editions, string title)
    {
        // Remove apenas repetições completas de metadados, mantendo variantes de imagem.
        var candidates = editions.DistinctBy(x => x.Cover).ToList();
        var components = candidates.Select(candidate => new List<(CoverCandidate Cover, string Token)> { candidate }).ToList();
        // Primeiro reúne evidências diretas (mesmo ID de imagem ou de edição).
        Merge((left, right) => left.WorkId == right.WorkId &&
            ((left.CoverId > 0 && left.CoverId == right.CoverId) ||
             (OpenLibraryReferences.IsEdition(left.EditionId) && left.EditionId == right.EditionId)), false);
        // Depois, ISBN válido + editora, sem contradições conhecidas no grupo inteiro.
        // Isso impede que um registro incompleto una duas edições conflitantes por transitividade.
        Merge(EditionMetadataIdentity.CanShareEdition, true);

        return components.Select(component =>
        {
            var ordered = component.OrderByDescending(x => CoverLanguages.IsPortuguese(x.Cover.Languages))
                .ThenByDescending(x => BookCoverRelevance.Similarity(title, x.Cover.Title)).ToList();
            return new CoverEditionGroup(ordered[0].Cover.CoverId, ordered);
        }).OrderByDescending(group => CoverLanguages.IsPortuguese(group.Primary.Cover.Languages))
            .ThenByDescending(group => BookCoverRelevance.Similarity(title, group.Primary.Cover.Title)).ToList();

        void Merge(Func<CoverCandidate, CoverCandidate, bool> related, bool rejectConflicts)
        {
            for (var i = 0; i < components.Count; i++)
                for (var j = i + 1; j < components.Count; j++)
                {
                    if (!components[i].Any(left => components[j].Any(right => related(left.Cover, right.Cover)))) continue;
                    if (rejectConflicts && components[i].Any(left => components[j].Any(right => EditionMetadataIdentity.Conflicts(left.Cover, right.Cover)))) continue;
                    components[i].AddRange(components[j]);
                    components.RemoveAt(j);
                    // A união pode conectar componentes já examinados; reinicia a passagem.
                    i = -1;
                    break;
                }
        }
    }
}
