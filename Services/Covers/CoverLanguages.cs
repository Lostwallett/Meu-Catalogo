namespace MyCatalog.Services.Covers;

public static class CoverLanguages
{
    private static readonly Dictionary<string, string> Names = new(StringComparer.OrdinalIgnoreCase)
    {
        ["por"] = "Português", ["pt"] = "Português", ["pt-br"] = "Português", ["pt-pt"] = "Português",
        ["eng"] = "Inglês", ["en"] = "Inglês",
        ["spa"] = "Espanhol", ["es"] = "Espanhol",
        ["fre"] = "Francês", ["fra"] = "Francês", ["fr"] = "Francês",
        ["ger"] = "Alemão", ["deu"] = "Alemão", ["de"] = "Alemão",
        ["ita"] = "Italiano", ["it"] = "Italiano",
        ["jpn"] = "Japonês", ["ja"] = "Japonês",
        ["chi"] = "Chinês", ["zho"] = "Chinês", ["zh"] = "Chinês",
        ["rus"] = "Russo", ["ru"] = "Russo",
        ["ara"] = "Árabe", ["ar"] = "Árabe",
        ["dut"] = "Neerlandês", ["nld"] = "Neerlandês", ["nl"] = "Neerlandês",
        ["kor"] = "Coreano", ["ko"] = "Coreano",
        ["lat"] = "Latim", ["la"] = "Latim"
    };

    private static IEnumerable<string> Codes(string? value) => (value ?? "")
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(code => code.Replace("/languages/", "", StringComparison.OrdinalIgnoreCase).ToLowerInvariant());

    public static bool IsPortuguese(string? value) => Codes(value).Any(code =>
        Names.TryGetValue(code, out var name) && name == "Português");

    public static string Display(string? value)
    {
        var names = Codes(value).Where(code => code != "não informado")
            .Select(code => Names.TryGetValue(code, out var name) ? name : $"Outro idioma ({code})")
            .Distinct().OrderByDescending(name => name == "Português").ToArray();
        return names.Length == 0 ? "Não informado" : string.Join(", ", names);
    }
}
