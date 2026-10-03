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
    // Justificaciones registradas por trabajador.
    [Area("Admin")]
    [Authorize(Roles = Roles.Admin)]
    public class JustificacionesController(ApplicationDbContext db, JustificacionService justificacionService) : Controller
    {
        public async Task<IActionResult> Index(int? trabajadorId)
        {
            var query = db.JustificacionesTrabajador
                .Include(j => j.Trabajador)
                .Include(j => j.TipoJustificacion)
                .AsQueryable();
            if (trabajadorId is not null)
                query = query.Where(j => j.TrabajadorId == trabajadorId);

            ViewBag.TrabajadorId = trabajadorId;
            ViewBag.TrabajadorNombre = trabajadorId is null ? null : (await db.Trabajadores.FindAsync(trabajadorId))?.NombreCompleto;
            return View(await query.OrderByDescending(j => j.FechaInicio).ToListAsync());
        }

        public async Task<IActionResult> Create(int? trabajadorId)
        {
            await CargarListasAsync();
            return View("Form", new JustificacionForm { TrabajadorId = trabajadorId });
        }

        public async Task<IActionResult> Edit(int id)
        {
            var j = await db.JustificacionesTrabajador.FindAsync(id);
            if (j is null) return NotFound();

            await CargarListasAsync(incluirTipoId: j.TipoJustificacionId);
            return View("Form", new JustificacionForm
            {
                Id = j.Id,
                TrabajadorId = j.TrabajadorId,
                TipoJustificacionId = j.TipoJustificacionId,
                FechaInicio = j.FechaInicio,
                FechaFin = j.FechaFin,
                Observacion = j.Observacion,
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(JustificacionForm form)
        {
            if (ModelState.IsValid)
                ModelState.AgregarErrores(await justificacionService.ValidarAsync(form));
            if (!ModelState.IsValid)
            {
                await CargarListasAsync(incluirTipoId: form.TipoJustificacionId);
                return View("Form", form);
            }

            var j = form.Id == 0 ? db.JustificacionesTrabajador.Add(new JustificacionTrabajador()).Entity
                                 : await db.JustificacionesTrabajador.FindAsync(form.Id);
            if (j is null) return NotFound();

            j.TrabajadorId = form.TrabajadorId!.Value;
            j.TipoJustificacionId = form.TipoJustificacionId!.Value;
            j.FechaInicio = form.FechaInicio!.Value;
            j.FechaFin = form.FechaFin!.Value;
            j.Observacion = string.IsNullOrWhiteSpace(form.Observacion) ? null : form.Observacion.Trim();
            await db.SaveChangesAsync();

            TempData["Ok"] = "Justificación guardada.";
            return RedirectToAction(nameof(Index), new { trabajadorId = j.TrabajadorId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var j = await db.JustificacionesTrabajador.FindAsync(id);
            if (j is null) return NotFound();

            db.JustificacionesTrabajador.Remove(j);
            await db.SaveChangesAsync();
            TempData["Ok"] = "Justificación eliminada.";
            return RedirectToAction(nameof(Index), new { trabajadorId = j.TrabajadorId });
        }

        // En edición se incluye el tipo actual aunque esté inactivo.
        private async Task CargarListasAsync(bool soloTiposActivos = true, int? incluirTipoId = null)
        {
            var trabajadores = await db.Trabajadores.OrderBy(t => t.Apellidos).ToListAsync();
            ViewBag.Trabajadores = new SelectList(trabajadores.Select(t => new { t.Id, Nombre = $"{t.Apellidos}, {t.Nombres} ({t.Codigo})" }), "Id", "Nombre");

            var tipos = await db.TiposJustificacion
                .Where(t => !soloTiposActivos || t.Activo || t.Id == incluirTipoId)
                .OrderBy(t => t.Nombre)
                .ToListAsync();
            ViewBag.Tipos = new SelectList(tipos, "Id", "Nombre");
        }
    }
}
