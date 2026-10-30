using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MyCatalog.Data;
using MyCatalog.Models;

namespace MyCatalog.Tests;

public class CatalogSeedDataTests : IAsyncLifetime
{
    // A conexão aberta mantém este banco privado vivo apenas durante um teste.
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private DbContextOptions<ApplicationDbContext> _options = null!;

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();
        _options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(_connection).Options;
        await using var context = new ApplicationDbContext(_options);
        await context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync() => await _connection.DisposeAsync();

    private async Task<(int Added, int Existing)> SeedAsync()
    {
        await using var context = new ApplicationDbContext(_options);
        return await CatalogSeedData.InitializeAsync(context);
    }

    private async Task<List<CatalogItem>> ReadAsync()
    {
        await using var context = new ApplicationDbContext(_options);
        return await context.CatalogItems.AsNoTracking().OrderBy(item => item.Id).ToListAsync();
    }

    private async Task InsertAsync(params CatalogItem[] items)
    {
        await using var context = new ApplicationDbContext(_options);
        context.AddRange(items);
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task EmptyDatabaseReceives25ValidExamplesAcrossAllSupportedTypes()
    {
        Assert.Equal((25, 0), await SeedAsync());
        var items = await ReadAsync();
        Assert.Equal(25, items.Count);
        Assert.Equal(25, items.Select(item => (item.Title, item.Type)).Distinct().Count());
        Assert.Equal(new[] { "Filme", "Jogo", "Livro", "Outro", "Série" },
            items.Select(item => item.Type).Distinct().OrderBy(type => type, StringComparer.Ordinal).ToArray());
        Assert.All(items, item => Assert.True(Validator.TryValidateObject(item,
            new ValidationContext(item), new List<ValidationResult>(), validateAllProperties: true)));
    }

    [Fact]
    public async Task RepeatedStartsDoNotDuplicateOrChangePersistedExamples()
    {
        await SeedAsync();
        var before = JsonSerializer.Serialize(await ReadAsync());
        Assert.Equal((0, 25), await SeedAsync());
        Assert.Equal((0, 25), await SeedAsync());
        Assert.Equal(before, JsonSerializer.Serialize(await ReadAsync()));
    }

    [Theory]
    [InlineData("A Viagem de Chihiro", "Filme")]
    [InlineData("  a  viagem DE chihiro  ", " filme ")]
    public async Task ExistingExampleIsPreservedIncludingEmptyFieldsAndFavorite(string title, string type)
    {
        var original = new CatalogItem
        {
            Title = title, Type = type, AuthorOrDirector = "Minha anotação", ReleaseYear = null,
            Genre = "", Description = "Descrição pessoal", IsFavorite = false
        };
        await InsertAsync(original);
        var before = JsonSerializer.Serialize(original);
        Assert.Equal((24, 1), await SeedAsync());
        var items = await ReadAsync();
        Assert.Equal(25, items.Count);
        Assert.Equal(before, JsonSerializer.Serialize(items.Single(item => item.Id == original.Id)));
    }

    [Fact]
    public async Task UnrelatedRecordAndSameTitleWithDifferentTypeArePreserved()
    {
        await InsertAsync(new CatalogItem { Title = "Meu item", Type = "Outro", IsFavorite = true },
            new CatalogItem { Title = "O Hobbit", Type = "Filme", Description = "Minha adaptação" });
        var before = await ReadAsync();
        Assert.Equal((25, 0), await SeedAsync());
        var after = await ReadAsync();
        Assert.Equal(27, after.Count);
        Assert.All(before, item => Assert.Equal(JsonSerializer.Serialize(item),
            JsonSerializer.Serialize(after.Single(saved => saved.Id == item.Id))));
        Assert.Equal(2, after.Count(item => item.Title == "O Hobbit"));
    }

    [Fact]
    public async Task LaterUserEditsAreNotOverwrittenOnNextStart()
    {
        await SeedAsync();
        await using (var context = new ApplicationDbContext(_options))
        {
            var item = await context.CatalogItems.SingleAsync(item => item.Title == "O Hobbit");
            item.AuthorOrDirector = "Anotação pessoal";
            item.ReleaseYear = 2000;
            item.Genre = "Meu gênero";
            item.Description = "Minha descrição";
            item.IsFavorite = false;
            await context.SaveChangesAsync();
        }
        var before = JsonSerializer.Serialize(await ReadAsync());
        Assert.Equal((0, 25), await SeedAsync());
        Assert.Equal(before, JsonSerializer.Serialize(await ReadAsync()));
    }
}
