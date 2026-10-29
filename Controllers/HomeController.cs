using Microsoft.AspNetCore.Mvc;

namespace MyCatalog.Controllers;

public class HomeController : Controller
{
    // Exibe a página inicial.
    public IActionResult Index() => View();

    // Página de erro genérica para ambientes de produção.
    public IActionResult Error() => View();
}
