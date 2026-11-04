using Microsoft.EntityFrameworkCore;
using MyCatalog.Data;
using MyCatalog.Services.Covers;

var builder = WebApplication.CreateBuilder(args);

var expectedTestDatabase = builder.Configuration["ManualTest:ExpectedDatabase"];
if (!string.IsNullOrWhiteSpace(expectedTestDatabase))
    ManualTestDatabaseGuard.Validate(builder.Configuration.GetConnectionString("DefaultConnection"), expectedTestDatabase);

// Habilita os controllers e as páginas Razor (MVC).
builder.Services.AddControllersWithViews();
builder.Services.Configure<BookCoverOptions>(builder.Configuration.GetSection("BookCovers"));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<OpenLibraryRequestGate>();
builder.Services.AddSingleton<OpenLibraryCache>();
builder.Services.AddDataProtection();
builder.Services.AddScoped<CoverSelectionTokens>();
builder.Services.AddScoped<CatalogCoverService>();
builder.Services.AddHttpClient<ICoverProvider, OpenLibraryCoverProvider>()
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });

// Registra o contexto do Entity Framework para usar SQL Server LocalDB.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

if (!string.IsNullOrWhiteSpace(expectedTestDatabase))
{
    var target = ManualTestDatabaseGuard.Validate(builder.Configuration.GetConnectionString("DefaultConnection"), expectedTestDatabase);
    app.Logger.LogInformation("TESTE ISOLADO | Servidor: {Server} | Banco: {Database} | Capas habilitadas: {Enabled}",
        target.Server, target.Database, builder.Configuration.GetValue<bool>("BookCovers:Enabled"));
}

if (app.Environment.IsDevelopment())
{
    await using var scope = app.Services.CreateAsyncScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var result = await CatalogSeedData.InitializeAsync(context);
    app.Logger.LogInformation(
        "Dados demonstrativos: {Added} adicionados e {Existing} já existentes. Registros existentes preservados.",
        result.Added, result.Existing);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

// Rota padrão: a página inicial do site.
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.Run();
