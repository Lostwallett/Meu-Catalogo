using System.Data;
using Microsoft.EntityFrameworkCore;
using MyCatalog.Models;

namespace MyCatalog.Data;

// Dados demonstrativos: chamada somente na inicialização em Development.
public static class CatalogSeedData
{
    public static async Task<(int Added, int Existing)> InitializeAsync(ApplicationDbContext context)
    {
        // Mantém a consulta e a inserção na mesma transação para proteger
        // a verificação dos exemplos durante inicializações simultâneas.
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        // Só lê os registros existentes; nunca os anexa para atualização.
        var existingItems = await context.CatalogItems.AsNoTracking()
            .Select(item => new { item.Title, item.Type }).ToListAsync();
        var keys = existingItems.Select(item => Key(item.Title, item.Type)).ToHashSet();

        CatalogItem[] examples =
        [
            Item("A Viagem de Chihiro", "Filme", "Hayao Miyazaki", 2001, "Animação / Fantasia", "Uma garota entra em um mundo de espíritos e procura salvar seus pais.", true),
            Item("Interestelar", "Filme", "Christopher Nolan", 2014, "Ficção científica", "Exploradores viajam pelo espaço em busca de um futuro para a humanidade."),
            Item("O Senhor dos Anéis: A Sociedade do Anel", "Filme", "Peter Jackson", 2001, "Fantasia / Aventura", "Um hobbit inicia uma jornada para destruir um anel poderoso."),
            Item("Cidade de Deus", "Filme", "Fernando Meirelles e Kátia Lund", 2002, "Drama", "Um jovem encontra na fotografia uma forma de contar a vida em sua comunidade."),
            Item("De Volta para o Futuro", "Filme", "Robert Zemeckis", 1985, "Ficção científica / Aventura", "Um adolescente viaja ao passado e precisa garantir o encontro de seus pais."),
            Item("O Show de Truman", "Filme", "Peter Weir", 1998, "Drama / Comédia", "Um homem começa a questionar a realidade cuidadosamente construída ao seu redor."),
            Item("WALL-E", "Filme", "Andrew Stanton", 2008, "Animação / Ficção científica", "Um robô responsável pela limpeza da Terra embarca em uma aventura espacial."),
            Item("Dom Casmurro", "Livro", "Machado de Assis", 1899, "Romance", "Bento Santiago revisita suas memórias e seu relacionamento com Capitu."),
            Item("O Hobbit", "Livro", "J. R. R. Tolkien", 1937, "Fantasia", "Bilbo Bolseiro acompanha um grupo de anões em uma jornada até uma montanha distante.", true),
            Item("1984", "Livro", "George Orwell", 1949, "Distopia", "Um homem tenta preservar sua individualidade em uma sociedade de vigilância."),
            Item("Orgulho e Preconceito", "Livro", "Jane Austen", 1813, "Romance", "Elizabeth Bennet enfrenta expectativas sociais e primeiras impressões enganosas."),
            Item("O Pequeno Príncipe", "Livro", "Antoine de Saint-Exupéry", 1943, "Fábula", "Um viajante de outro planeta compartilha reflexões sobre amizade e afeto."),
            Item("Capitães da Areia", "Livro", "Jorge Amado", 1937, "Romance", "Um grupo de meninos vive nas ruas de Salvador e enfrenta a exclusão social."),
            Item("Frankenstein", "Livro", "Mary Shelley", 1818, "Ficção científica / Terror", "Um cientista dá vida a uma criatura e enfrenta as consequências de sua criação."),
            Item("Breaking Bad", "Série", "Vince Gilligan", 2008, "Drama / Crime", "Um professor de química passa a produzir drogas após receber um diagnóstico grave."),
            Item("Stranger Things", "Série", "Matt Duffer e Ross Duffer", 2016, "Ficção científica / Suspense", "Amigos investigam desaparecimentos e fenômenos sobrenaturais em uma pequena cidade."),
            Item("Dark", "Série", "Baran bo Odar e Jantje Friese", 2017, "Ficção científica / Mistério", "Desaparecimentos revelam conexões entre famílias e diferentes épocas."),
            Item("Avatar: A Lenda de Aang", "Série", "Michael Dante DiMartino e Bryan Konietzko", 2005, "Animação / Fantasia", "Um jovem precisa dominar quatro elementos para restaurar o equilíbrio do mundo.", true),
            Item("The Good Place", "Série", "Michael Schur", 2016, "Comédia / Fantasia", "Uma mulher tenta se tornar uma pessoa melhor após chegar à vida após a morte."),
            Item("Sherlock", "Série", "Mark Gatiss e Steven Moffat", 2010, "Mistério / Crime", "Sherlock Holmes e John Watson investigam crimes na Londres contemporânea."),
            Item("Anne with an E", "Série", "Moira Walley-Beckett", 2017, "Drama", "Uma órfã criativa transforma a vida de dois irmãos que a acolhem em sua fazenda."),
            Item("Stardew Valley", "Jogo", "Eric Barone", 2016, "Simulação / RPG", "O jogador recupera uma fazenda e constrói relações com os moradores de uma vila."),
            Item("Minecraft", "Jogo", "Mojang", 2011, "Construção / Sobrevivência", "Um mundo de blocos permite explorar, coletar recursos e construir livremente."),
            Item("Portal 2", "Jogo", "Valve", 2011, "Quebra-cabeça", "Desafios de lógica usam portais para atravessar um complexo de testes."),
            Item("O Auto da Compadecida", "Outro", "Ariano Suassuna", 1955, "Teatro / Comédia", "Peça teatral sobre as aventuras de João Grilo e Chicó no sertão nordestino.")
        ];

        var added = 0;
        foreach (var example in examples)
        {
            if (!keys.Add(Key(example.Title, example.Type)))
                continue;

            context.CatalogItems.Add(example);
            added++;
        }

        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        return (added, examples.Length - added);
    }

    // Normaliza apenas para comparação, sem mudar o texto salvo pelo usuário.
    private static (string Title, string Type) Key(string title, string type) =>
        (Normalize(title), Normalize(type));

    private static string Normalize(string value) =>
        string.Join(" ", value.Normalize().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            .ToUpperInvariant();

    private static CatalogItem Item(string title, string type, string authorOrDirector,
        int releaseYear, string genre, string description, bool isFavorite = false) => new()
    {
        Title = title,
        Type = type,
        AuthorOrDirector = authorOrDirector,
        ReleaseYear = releaseYear,
        Genre = genre,
        Description = description,
        IsFavorite = isFavorite
    };
}
