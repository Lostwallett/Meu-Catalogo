using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using MyCatalog.Data;

namespace MyCatalog.Tests;

public class CatalogSeedStartupTests
{
    [Theory]
    [InlineData("Development", null)]
    [InlineData("Development", "false")]
    [InlineData("Production", "true")]
    [InlineData("Staging", "true")]
    [InlineData("Production", null)]
    public async Task DisabledStartupDoesNotEvenResolveDatabaseServices(string environment, string? enabled)
    {
        var configuration = Configuration(enabled);
        // Sem DbContext registrado: qualquer tentativa de seed falharia.
        await using var services = new ServiceCollection().BuildServiceProvider();

        Assert.Null(await CatalogSeedStartup.InitializeAsync(services, Environment(environment), configuration));
    }

    [Fact]
    public async Task ExplicitDevelopmentOptInRunsExistingSeedWithoutDuplicatingExamples()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var registrations = new ServiceCollection();
        registrations.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connection));
        await using var services = registrations.BuildServiceProvider();
        await using (var scope = services.CreateAsyncScope())
            await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.EnsureCreatedAsync();

        var configuration = Configuration("true");
        var environment = Environment("Development");
        Assert.Equal((25, 0), (await CatalogSeedStartup.InitializeAsync(services, environment, configuration))!.Value);
        Assert.Equal((0, 25), (await CatalogSeedStartup.InitializeAsync(services, environment, configuration))!.Value);

        await using var checkScope = services.CreateAsyncScope();
        Assert.Equal(25, await checkScope.ServiceProvider.GetRequiredService<ApplicationDbContext>().CatalogItems.CountAsync());
    }

    [Fact]
    public void EnvironmentVariableDoubleUnderscoreOverridesDisabledDefault()
    {
        var prefix = $"MyCatalogSeedTest_{Guid.NewGuid():N}_";
        var variable = prefix + "CatalogSeed__Enabled";
        System.Environment.SetEnvironmentVariable(variable, "true");
        try
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["CatalogSeed:Enabled"] = "false" })
                .AddEnvironmentVariables(prefix)
                .Build();
            Assert.True(configuration.GetValue<bool>("CatalogSeed:Enabled"));
        }
        finally
        {
            System.Environment.SetEnvironmentVariable(variable, null);
        }
    }

    private static IConfiguration Configuration(string? enabled) => new ConfigurationBuilder()
        .AddInMemoryCollection(enabled is null ? [] : new Dictionary<string, string?> { ["CatalogSeed:Enabled"] = enabled })
        .Build();

    private static IHostEnvironment Environment(string name) => new HostEnvironmentStub { EnvironmentName = name };

    private sealed class HostEnvironmentStub : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "MyCatalog.Tests";
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
