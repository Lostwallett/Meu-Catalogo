using Microsoft.EntityFrameworkCore;
using MyCatalog.Data;

var builder = WebApplication.CreateBuilder(args);

// Habilita os controllers e as páginas Razor (MVC).
builder.Services.AddControllersWithViews();

// Registra o contexto do Entity Framework para usar SQL Server LocalDB.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

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
