using BancaInti.Constants;
using BancaInti.Data;
using BancaInti.Models;
using BancaInti.Models.ViewModels;
using BancaInti.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BancaInti.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = Roles.Admin)]
    public class PeriodosEstrategicosController(ApplicationDbContext db, PeriodoService periodoService) : Controller
    {
        public async Task<IActionResult> Index()
        {
            var periodos = await db.PeriodosEstrategicos
                .Include(p => p.PeriodosOperativos)
                .OrderByDescending(p => p.FechaInicio)
                .ToListAsync();
            return View(periodos);
        }

        public IActionResult Create() => View("Form", new PeriodoEstrategicoForm());

        public async Task<IActionResult> Edit(int id)
        {
            var p = await db.PeriodosEstrategicos.FindAsync(id);
            if (p is null) return NotFound();
            return View("Form", new PeriodoEstrategicoForm { Id = p.Id, Nombre = p.Nombre, FechaInicio = p.FechaInicio, FechaFin = p.FechaFin });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(PeriodoEstrategicoForm form)
        {
            if (ModelState.IsValid)
                ModelState.AgregarErrores(await periodoService.ValidarEstrategicoAsync(form));
            if (!ModelState.IsValid)
                return View("Form", form);

            var periodo = form.Id == 0 ? db.PeriodosEstrategicos.Add(new PeriodoEstrategico()).Entity
                                       : await db.PeriodosEstrategicos.FindAsync(form.Id);
            if (periodo is null) return NotFound();

            periodo.Nombre = form.Nombre.Trim();
            periodo.FechaInicio = form.FechaInicio!.Value;
            periodo.FechaFin = form.FechaFin!.Value;
            await db.SaveChangesAsync();

            TempData["Ok"] = "Periodo estratégico guardado.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var periodo = await db.PeriodosEstrategicos.Include(p => p.PeriodosOperativos).FirstOrDefaultAsync(p => p.Id == id);
            if (periodo is null) return NotFound();

            if (periodo.PeriodosOperativos.Count > 0)
                TempData["Error"] = "No se puede eliminar: tiene periodos operativos.";
            else
            {
                db.PeriodosEstrategicos.Remove(periodo);
                await db.SaveChangesAsync();
                TempData["Ok"] = "Periodo estratégico eliminado.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
