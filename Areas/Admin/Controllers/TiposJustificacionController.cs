using BancaInti.Constants;
using BancaInti.Data;
using BancaInti.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BancaInti.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = Roles.Admin)]
    public class TiposJustificacionController(ApplicationDbContext db) : Controller
    {
        public async Task<IActionResult> Index()
        {
            return View(await db.TiposJustificacion.OrderBy(t => t.Nombre).ToListAsync());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(int id, string? nombre)
        {
            nombre = nombre?.Trim() ?? "";
            if (nombre.Length == 0 || nombre.Length > 80)
                TempData["Error"] = "El nombre es obligatorio (máximo 80 caracteres).";
            else if (await db.TiposJustificacion.AnyAsync(t => t.Id != id && t.Nombre.ToLower() == nombre.ToLower()))
                TempData["Error"] = $"Ya existe el tipo \"{nombre}\".";
            else
            {
                var tipo = id == 0 ? db.TiposJustificacion.Add(new TipoJustificacion()).Entity
                                   : await db.TiposJustificacion.FindAsync(id);
                if (tipo is null) return NotFound();
                tipo.Nombre = nombre;
                await db.SaveChangesAsync();
                TempData["Ok"] = "Tipo de justificación guardado.";
            }
            return RedirectToAction(nameof(Index));
        }

        // Se desactiva en lugar de eliminar: puede estar usado en justificaciones y periodos.
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarEstado(int id)
        {
            var tipo = await db.TiposJustificacion.FindAsync(id);
            if (tipo is null) return NotFound();
            tipo.Activo = !tipo.Activo;
            await db.SaveChangesAsync();
            TempData["Ok"] = tipo.Activo ? "Tipo activado." : "Tipo desactivado.";
            return RedirectToAction(nameof(Index));
        }
    }
}
