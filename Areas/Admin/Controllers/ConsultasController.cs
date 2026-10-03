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
    // Cada consulta devuelve una TablaConsulta que se muestra en la vista genérica Tabla.
    [Area("Admin")]
    [Authorize(Roles = Roles.Admin)]
    public class ConsultasController(ApplicationDbContext db, ConsultaService consultas, EvaluacionService evaluacionService) : Controller
    {
        public async Task<IActionResult> Participantes(int? periodoId, bool todos = false)
        {
            var periodo = await PeriodoAsync(periodoId);
            if (periodo is null) return SinPeriodo();
            ViewBag.Todos = todos;
            return View("Tabla", await consultas.ParticipantesAsync(periodo, todos));
        }

        public async Task<IActionResult> Justificaciones(int? periodoId)
        {
            var periodo = await PeriodoAsync(periodoId);
            if (periodo is null) return SinPeriodo();
            return View("Tabla", await consultas.JustificacionesAsync(periodo));
        }

        public async Task<IActionResult> Mayores(int edad = 60)
        {
            ViewBag.Edad = edad;
            return View("Tabla", await consultas.MayoresDeAsync(edad));
        }

        public async Task<IActionResult> Evaluadores(int? periodoId)
        {
            var periodo = await PeriodoAsync(periodoId);
            if (periodo is null) return SinPeriodo();
            return View("Tabla", await consultas.EvaluadoresAsync(periodo));
        }

        public async Task<IActionResult> Auditoria(DateOnly? desde, DateOnly? hasta, string? usuario, string? entidad)
        {
            var hoy = evaluacionService.Hoy;
            ViewBag.Desde = desde ??= hoy.AddDays(-7);
            ViewBag.Hasta = hasta ??= hoy;
            ViewBag.Usuario = usuario;
            ViewBag.Entidades = new SelectList(await consultas.EntidadesAuditadasAsync(), entidad);
            return View("Tabla", await consultas.AuditoriaAsync(desde.Value, hasta.Value, usuario, entidad));
        }


        // Sin periodoId se usa el periodo activo (o el más reciente).
        private async Task<PeriodoOperativo?> PeriodoAsync(int? periodoId)
        {
            var periodos = await db.PeriodosOperativos.OrderByDescending(p => p.Anio).ToListAsync();
            var periodo = periodoId is null ? periodos.FirstOrDefault(p => p.Activo) ?? periodos.FirstOrDefault()
                                            : periodos.FirstOrDefault(p => p.Id == periodoId);
            ViewBag.Periodos = new SelectList(periodos.Select(p => new { p.Id, Nombre = $"{p.Anio}{(p.Activo ? " (activo)" : "")}" }), "Id", "Nombre", periodo?.Id);
            return periodo;
        }

        private IActionResult SinPeriodo()
        {
            TempData["Error"] = "No hay periodos operativos registrados.";
            return RedirectToAction("Index", "Home");
        }
    }
}
