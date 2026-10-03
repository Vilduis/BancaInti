using BancaInti.Constants;
using BancaInti.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BancaInti.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = Roles.Admin)]
    public class TrabajadoresController(ApplicationDbContext db) : Controller
    {
        public async Task<IActionResult> Index()
        {
            var trabajadores = await db.Trabajadores
                .Include(t => t.User)
                .OrderBy(t => t.Codigo)
                .ToListAsync();
            return View(trabajadores);
        }
    }
}
