using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MyCatalog.Data;

// O contexto só é resolvido depois das duas condições de ativação.
public static class CatalogSeedStartup
{
    public static async Task<(int Added, int Existing)?> InitializeAsync(
        IServiceProvider services, IHostEnvironment environment, IConfiguration configuration)
    {
        if (!environment.IsDevelopment() || !configuration.GetValue<bool>("CatalogSeed:Enabled"))
            return null;

        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await CatalogSeedData.InitializeAsync(context);
    }
}
