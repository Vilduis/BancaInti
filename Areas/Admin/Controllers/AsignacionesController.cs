using BancaInti.Constants;
using BancaInti.Data;
using BancaInti.Models.ViewModels;
using BancaInti.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BancaInti.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = Roles.Admin)]
    public class AsignacionesController(ApplicationDbContext db, AsignacionService asignacionService, ParticipacionService participacionService) : Controller
    {
        // Sin periodoId se muestra el periodo activo.
        public async Task<IActionResult> Index(int? periodoId)
        {
            var vm = new AsignacionesViewModel
            {
                Periodos = await db.PeriodosOperativos.OrderByDescending(p => p.Anio).ToListAsync(),
            };
            vm.Periodo = periodoId is null ? vm.Periodos.FirstOrDefault(p => p.Activo) ?? vm.Periodos.FirstOrDefault()
                                           : vm.Periodos.FirstOrDefault(p => p.Id == periodoId);
            if (vm.Periodo is null)
                return View(vm);

            var asignaciones = await db.Asignaciones
                .Include(a => a.Evaluador)
                .Include(a => a.Evaluado)
                .Where(a => a.PeriodoOperativoId == vm.Periodo.Id)
                .ToListAsync();
            var asignados = asignaciones.Select(a => a.EvaluadoId).ToHashSet();

            vm.Equipos = asignaciones
                .OrderBy(a => a.Evaluado!.Apellidos)
                .GroupBy(a => a.Evaluador!)
                .OrderBy(g => g.Key.Apellidos)
                .ToList();
            vm.SinAsignar = (await asignacionService.Elegibles(vm.Periodo).OrderBy(t => t.Area).ThenBy(t => t.Apellidos).ToListAsync())
                .Where(t => !asignados.Contains(t.Id))
                .ToList();
            vm.Evaluadores = await asignacionService.Evaluadores().OrderBy(t => t.Apellidos).ToListAsync();
            vm.Participacion = await participacionService.CalcularAsync(vm.Periodo,
                vm.SinAsignar.Concat(asignaciones.Select(a => a.Evaluado!)));

            ViewBag.DiasMinimos = await db.ReglasEvaluacion
                .Where(r => r.PeriodoOperativoId == vm.Periodo.Id)
                .Select(r => (int?)r.DiasMinimos)
                .FirstOrDefaultAsync() ?? 90;
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Asignar(int periodoId, int evaluadorId, List<int> evaluadoIds)
        {
            var errores = await asignacionService.AsignarAsync(periodoId, evaluadorId, evaluadoIds);
            if (errores.Count > 0)
                TempData["Error"] = string.Join(" ", errores);
            else
                TempData["Ok"] = $"{evaluadoIds.Count} trabajador(es) asignado(s).";
            return RedirectToAction(nameof(Index), new { periodoId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Quitar(int id)
        {
            var asignacion = await db.Asignaciones.FindAsync(id);
            if (asignacion is null) return NotFound();

            if (await db.Objetivos.AnyAsync(o => o.AsignacionId == id))
                TempData["Error"] = "No se puede quitar: el evaluador ya registró objetivos para este trabajador.";
            else
            {
                db.Asignaciones.Remove(asignacion);
                await db.SaveChangesAsync();
                TempData["Ok"] = "Asignación eliminada.";
            }
            return RedirectToAction(nameof(Index), new { periodoId = asignacion.PeriodoOperativoId });
        }
    }
}
