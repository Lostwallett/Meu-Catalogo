using Microsoft.EntityFrameworkCore;
using MyCatalog.Data;

var builder = WebApplication.CreateBuilder(args);

// Habilita os controllers e as páginas Razor (MVC).
builder.Services.AddControllersWithViews();

// Registra o contexto do Entity Framework para usar SQL Server LocalDB.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

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
