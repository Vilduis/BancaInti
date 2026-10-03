using BancaInti.Constants;
using BancaInti.Data;
using BancaInti.Models.ViewModels;
using BancaInti.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BancaInti.Areas.Evaluador.Controllers
{
    [Area("Evaluador")]
    [Authorize(Roles = Roles.Evaluador)]
    public class HomeController(ApplicationDbContext db, EvaluacionService evaluacionService,
        ParticipacionService participacionService, ReportePdfService reportePdfService, UserManager<IdentityUser> userManager) : Controller
    {
        // Dashboard "Mi equipo" del periodo activo.
        public async Task<IActionResult> Index()
        {
            var vm = new MiEquipoViewModel { Periodo = await evaluacionService.PeriodoActivoAsync() };
            if (vm.Periodo is null)
                return View(vm);

            var userId = userManager.GetUserId(User)!;
            vm.Etapa = evaluacionService.EtapaVigente(vm.Periodo);
            vm.Asignaciones = await db.Asignaciones
                .Include(a => a.Evaluado)
                .Include(a => a.Objetivos).ThenInclude(o => o.Seguimientos)
                .Where(a => a.PeriodoOperativoId == vm.Periodo.Id && a.Evaluador!.UserId == userId)
                .OrderBy(a => a.Evaluado!.Apellidos)
                .AsSplitQuery()
                .ToListAsync();
            vm.Participacion = await participacionService.CalcularAsync(vm.Periodo, vm.Asignaciones.Select(a => a.Evaluado!));
            return View(vm);
        }

        // Reporte del equipo: resumen + ficha de cada evaluado en un solo PDF.
        public async Task<IActionResult> PdfEquipo()
        {
            var userId = userManager.GetUserId(User)!;
            var asignaciones = await evaluacionService.AsignacionesDelEvaluadorAsync(userId);
            if (asignaciones.Count == 0)
            {
                TempData["Error"] = "No tiene evaluados asignados en el periodo activo.";
                return RedirectToAction(nameof(Index));
            }

            var periodo = asignaciones[0].PeriodoOperativo!;
            var evaluador = asignaciones[0].Evaluador!;
            var participacion = await participacionService.CalcularAsync(periodo, asignaciones.Select(a => a.Evaluado!));
            var pdf = reportePdfService.Grupal(periodo, evaluador, asignaciones, participacion);
            return File(pdf, "application/pdf", ReportePdfService.NombreArchivo($"Evaluacion {periodo.Anio} Equipo {evaluador.NombreCompleto}"));
        }
    }
}
