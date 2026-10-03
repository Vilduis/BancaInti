using BancaInti.Constants;
using BancaInti.Data;
using BancaInti.Models;
using BancaInti.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BancaInti.Areas.Colaborador.Controllers
{
    [Area("Colaborador")]
    [Authorize(Roles = Roles.Colaborador)]
    public class HomeController(ApplicationDbContext db, EvaluacionService evaluacionService, UserManager<IdentityUser> userManager) : Controller
    {
        private string UserId => userManager.GetUserId(User)!;

        // Pantalla de ingreso: mensaje en texto plano con botón OK que lleva a sus objetivos.
        public async Task<IActionResult> Index()
        {
            var periodo = await evaluacionService.PeriodoActivoAsync();
            ViewBag.Mensaje = periodo?.MensajeColaborador ?? PeriodoOperativo.MensajePorDefecto;
            return View();
        }

        public async Task<IActionResult> Objetivos()
        {
            ViewBag.Periodo = await evaluacionService.PeriodoActivoAsync();
            return View(await evaluacionService.AsignacionDelEvaluadoAsync(UserId));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Comentar(int asignacionId, int? objetivoId, string? texto)
        {
            var a = await evaluacionService.AsignacionDelEvaluadoAsync(UserId);
            if (a is null || a.Id != asignacionId) return NotFound();
            if (a.Estado == EstadoEvaluacion.Borrador) return NotFound();
            if (objetivoId is not null && a.Objetivos.All(o => o.Id != objetivoId)) return NotFound();

            texto = texto?.Trim();
            if (string.IsNullOrEmpty(texto) || texto.Length > 1000)
                TempData["Error"] = "El comentario es obligatorio (máximo 1000 caracteres).";
            else
            {
                a.Comentarios.Add(new Comentario { ObjetivoId = objetivoId, AutorId = a.EvaluadoId, Texto = texto, Fecha = DateTime.UtcNow });
                await db.SaveChangesAsync();
                TempData["Ok"] = "Comentario enviado.";
            }
            return Redirect(Url.Action(nameof(Objetivos)) + "#comentarios");
        }
    }
}
