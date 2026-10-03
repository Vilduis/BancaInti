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
    public class PeriodosOperativosController(ApplicationDbContext db, PeriodoService periodoService, ReglaService reglaService) : Controller
    {
        public async Task<IActionResult> Index()
        {
            var periodos = await db.PeriodosOperativos
                .Include(p => p.PeriodoEstrategico)
                .OrderByDescending(p => p.Anio)
                .ToListAsync();
            return View(periodos);
        }

        public async Task<IActionResult> Create(int? estrategicoId)
        {
            await CargarEstrategicosAsync();
            return View("Form", new PeriodoOperativoForm { PeriodoEstrategicoId = estrategicoId });
        }

        public async Task<IActionResult> Edit(int id)
        {
            var periodo = await db.PeriodosOperativos.FindAsync(id);
            if (periodo is null) return NotFound();

            await CargarEstrategicosAsync();
            return View("Form", PeriodoService.ToForm(periodo));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(PeriodoOperativoForm form)
        {
            if (ModelState.IsValid)
                ModelState.AgregarErrores(await periodoService.ValidarOperativoAsync(form));
            if (!ModelState.IsValid)
            {
                await CargarEstrategicosAsync();
                return View("Form", form);
            }

            if (form.Id == 0)
                await periodoService.CrearOperativoAsync(form);
            else
            {
                var periodo = await db.PeriodosOperativos.FindAsync(form.Id);
                if (periodo is null) return NotFound();
                await periodoService.ActualizarOperativoAsync(periodo, form);
            }

            TempData["Ok"] = "Periodo operativo guardado.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Activar(int id)
        {
            await periodoService.ActivarAsync(id);
            TempData["Ok"] = "Periodo operativo activado.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var periodo = await db.PeriodosOperativos.FindAsync(id);
            if (periodo is null) return NotFound();

            if (periodo.Activo)
                TempData["Error"] = "No se puede eliminar el periodo activo.";
            else
            {
                db.PeriodosOperativos.Remove(periodo);
                await db.SaveChangesAsync();
                TempData["Ok"] = "Periodo operativo eliminado.";
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Justificaciones(int id)
        {
            var periodo = await db.PeriodosOperativos.Include(p => p.Justificaciones).FirstOrDefaultAsync(p => p.Id == id);
            if (periodo is null) return NotFound();

            return View(new JustificacionesPeriodoForm
            {
                PeriodoOperativoId = periodo.Id,
                PeriodoNombre = $"Periodo {periodo.Anio}",
                Seleccionados = periodo.Justificaciones.Select(j => j.TipoJustificacionId).ToList(),
                Tipos = await db.TiposJustificacion.Where(t => t.Activo).OrderBy(t => t.Nombre).ToListAsync(),
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Justificaciones(JustificacionesPeriodoForm form)
        {
            var periodo = await db.PeriodosOperativos.Include(p => p.Justificaciones)
                .FirstOrDefaultAsync(p => p.Id == form.PeriodoOperativoId);
            if (periodo is null) return NotFound();

            periodo.Justificaciones.RemoveAll(j => !form.Seleccionados.Contains(j.TipoJustificacionId));
            foreach (var tipoId in form.Seleccionados.Except(periodo.Justificaciones.Select(j => j.TipoJustificacionId)).ToList())
                periodo.Justificaciones.Add(new PeriodoJustificacion { TipoJustificacionId = tipoId });
            await db.SaveChangesAsync();

            TempData["Ok"] = "Justificaciones del periodo guardadas.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Reglas(int id)
        {
            var periodo = await db.PeriodosOperativos
                .Include(p => p.Regla).ThenInclude(r => r!.Rangos)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (periodo is null) return NotFound();

            var regla = periodo.Regla ?? reglaService.ReglaPorDefecto();
            regla.PeriodoOperativoId = periodo.Id;
            return View(ReglaService.ToForm(regla, $"Periodo {periodo.Anio}"));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Reglas(ReglaForm form)
        {
            if (ModelState.IsValid)
                ModelState.AgregarErrores(reglaService.Validar(form));
            if (!ModelState.IsValid)
                return View(form);

            var periodo = await db.PeriodosOperativos
                .Include(p => p.Regla).ThenInclude(r => r!.Rangos)
                .FirstOrDefaultAsync(p => p.Id == form.PeriodoOperativoId);
            if (periodo is null) return NotFound();

            periodo.Regla ??= new ReglaEvaluacion();
            ReglaService.Aplicar(periodo.Regla, form);
            await db.SaveChangesAsync();

            TempData["Ok"] = "Reglas de evaluación guardadas.";
            return RedirectToAction(nameof(Index));
        }

        private async Task CargarEstrategicosAsync()
        {
            ViewBag.Estrategicos = new SelectList(
                await db.PeriodosEstrategicos.OrderByDescending(p => p.FechaInicio).ToListAsync(), "Id", "Nombre");
        }
    }
}
