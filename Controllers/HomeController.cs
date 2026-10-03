using BancaInti.Constants;
using BancaInti.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace BancaInti.Controllers
{
    public class HomeController : Controller
    {
        // Envía a cada usuario al área de su rol de mayor nivel.
        public IActionResult Index()
        {
            if (User.Identity?.IsAuthenticated != true)
                return Redirect("/Identity/Account/Login");

            var area = Roles.Todos.FirstOrDefault(User.IsInRole);
            if (area is null)
                return View();

            return RedirectToAction("Index", "Home", new { area });
        }

        // Páginas de estado HTTP (UseStatusCodePagesWithReExecute).
        public IActionResult Estado(int codigo) =>
            codigo == 404 ? View("NoEncontrado") : Error();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
