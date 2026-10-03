using BancaInti.Constants;
using BancaInti.Data;
using BancaInti.Models;
using BancaInti.Models.ViewModels;
using BancaInti.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BancaInti.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = Roles.Admin)]
    public class EtapasController(ApplicationDbContext db, PeriodoService periodoService) : Controller
    {
        public async Task<IActionResult> Index(int? periodoId)
        {
            var query = db.Etapas.Include(e => e.PeriodoOperativo).AsQueryable();
            if (periodoId is not null)
                query = query.Where(e => e.PeriodoOperativoId == periodoId);

            ViewBag.PeriodoId = periodoId;
            ViewBag.PeriodoNombre = periodoId is null ? null : (await db.PeriodosOperativos.FindAsync(periodoId)) is { } p ? $"Periodo {p.Anio}" : null;
            return View(await query.OrderByDescending(e => e.PeriodoOperativo!.Anio).ThenBy(e => e.Tipo).ToListAsync());
        }

        public async Task<IActionResult> Create(int? periodoId)
        {
            await CargarPeriodosAsync();
            return View("Form", new EtapaForm { PeriodoOperativoId = periodoId });
        }

        public async Task<IActionResult> Edit(int id)
        {
            var e = await db.Etapas.FindAsync(id);
            if (e is null) return NotFound();

            await CargarPeriodosAsync();
            return View("Form", new EtapaForm { Id = e.Id, PeriodoOperativoId = e.PeriodoOperativoId, Tipo = e.Tipo, FechaInicio = e.FechaInicio, FechaFin = e.FechaFin });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(EtapaForm form)
        {
            var etapa = form.Id == 0 ? null : await db.Etapas.FindAsync(form.Id);
            if (form.Id != 0)
            {
                if (etapa is null) return NotFound();
                // Al editar, el periodo y el tipo no cambian (solo las fechas).
                form.PeriodoOperativoId = etapa.PeriodoOperativoId;
                form.Tipo = etapa.Tipo;
                ModelState.Remove(nameof(form.PeriodoOperativoId));
                ModelState.Remove(nameof(form.Tipo));
            }

            if (ModelState.IsValid)
                ModelState.AgregarErrores(await periodoService.ValidarEtapaAsync(form));
            if (!ModelState.IsValid)
            {
                await CargarPeriodosAsync();
                return View("Form", form);
            }

            etapa ??= db.Etapas.Add(new Etapa { PeriodoOperativoId = form.PeriodoOperativoId!.Value, Tipo = form.Tipo!.Value }).Entity;
            etapa.FechaInicio = form.FechaInicio!.Value;
            etapa.FechaFin = form.FechaFin!.Value;
            await db.SaveChangesAsync();

            TempData["Ok"] = "Etapa guardada.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var etapa = await db.Etapas.Include(e => e.PeriodoOperativo).FirstOrDefaultAsync(e => e.Id == id);
            if (etapa is null) return NotFound();

            if (etapa.PeriodoOperativo!.Activo)
                TempData["Error"] = "No se puede eliminar una etapa del periodo activo; ajuste sus fechas.";
            else
            {
                db.Etapas.Remove(etapa);
                await db.SaveChangesAsync();
                TempData["Ok"] = "Etapa eliminada.";
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task CargarPeriodosAsync()
        {
            var periodos = await db.PeriodosOperativos.OrderByDescending(p => p.Anio).ToListAsync();
            ViewBag.Periodos = new SelectList(
                periodos.Select(p => new { p.Id, Nombre = $"Periodo {p.Anio}{(p.Activo ? " (activo)" : "")} · {p.FechaInicio:dd/MM/yyyy} – {p.FechaFin:dd/MM/yyyy}" }),
                "Id", "Nombre");
        }
    }
}
