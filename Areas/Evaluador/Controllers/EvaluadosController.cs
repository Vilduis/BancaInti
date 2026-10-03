using BancaInti.Constants;
using BancaInti.Data;
using BancaInti.Models;
using BancaInti.Models.ViewModels;
using BancaInti.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BancaInti.Areas.Evaluador.Controllers
{
    // Gestión de un evaluado: objetivos, seguimiento, evaluación y comentarios.
    // Toda acción carga la asignación filtrando por el usuario actual (no se puede operar sobre equipos ajenos).
    [Area("Evaluador")]
    [Authorize(Roles = Roles.Evaluador)]
    public class EvaluadosController(ApplicationDbContext db, EvaluacionService evaluacionService,
        ParticipacionService participacionService, ReportePdfService reportePdfService, UserManager<IdentityUser> userManager) : Controller
    {
        private string UserId => userManager.GetUserId(User)!;

        public async Task<IActionResult> Detalle(int id)
        {
            var a = await evaluacionService.AsignacionDelEvaluadorAsync(id, UserId);
            if (a is null) return NotFound();

            var periodo = a.PeriodoOperativo!;
            var participacion = await participacionService.CalcularAsync(periodo, [a.Evaluado!]);
            return View(new EvaluadoDetalleViewModel
            {
                Asignacion = a,
                Etapa = evaluacionService.EtapaVigente(periodo),
                Regla = periodo.Regla!,
                Participacion = participacion[a.EvaluadoId],
            });
        }

        public async Task<IActionResult> Pdf(int id)
        {
            var a = await evaluacionService.AsignacionDelEvaluadorAsync(id, UserId);
            if (a is null) return NotFound();

            var participacion = await participacionService.CalcularAsync(a.PeriodoOperativo!, [a.Evaluado!]);
            var pdf = reportePdfService.Individual(a, participacion[a.EvaluadoId]);
            return File(pdf, "application/pdf", ReportePdfService.NombreArchivo($"Evaluacion {a.PeriodoOperativo!.Anio} {a.Evaluado!.NombreCompleto}"));
        }

        // ---------- Objetivos (etapa Registro) ----------

        public async Task<IActionResult> NuevoObjetivo(int asignacionId)
        {
            var a = await evaluacionService.AsignacionDelEvaluadorAsync(asignacionId, UserId);
            if (a is null) return NotFound();
            if (Bloqueado(evaluacionService.ValidarEdicionObjetivos(a), out var r, a.Id)) return r;

            ViewBag.Asignacion = a;
            return View("Objetivo", new ObjetivoForm { AsignacionId = a.Id });
        }

        public async Task<IActionResult> EditarObjetivo(int asignacionId, int id)
        {
            var a = await evaluacionService.AsignacionDelEvaluadorAsync(asignacionId, UserId);
            var o = a?.Objetivos.FirstOrDefault(x => x.Id == id);
            if (a is null || o is null) return NotFound();
            if (Bloqueado(evaluacionService.ValidarEdicionObjetivos(a), out var r, a.Id)) return r;

            ViewBag.Asignacion = a;
            return View("Objetivo", new ObjetivoForm
            {
                Id = o.Id,
                AsignacionId = a.Id,
                Descripcion = o.Descripcion,
                Indicador = o.Indicador,
                UnidadMedida = o.UnidadMedida,
                Peso = o.Peso,
                Meta = o.Meta,
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarObjetivo(ObjetivoForm form)
        {
            var a = await evaluacionService.AsignacionDelEvaluadorAsync(form.AsignacionId, UserId);
            if (a is null) return NotFound();
            var o = form.Id == 0 ? new Objetivo() : a.Objetivos.FirstOrDefault(x => x.Id == form.Id);
            if (o is null) return NotFound();

            if (ModelState.IsValid)
                ModelState.AgregarErrores(evaluacionService.ValidarObjetivo(a, form));
            if (!ModelState.IsValid)
            {
                ViewBag.Asignacion = a;
                return View("Objetivo", form);
            }

            o.Descripcion = form.Descripcion.Trim();
            o.Indicador = form.Indicador.Trim();
            o.UnidadMedida = form.UnidadMedida.Trim();
            o.Peso = form.Peso!.Value;
            o.Meta = form.Meta!.Value;
            if (form.Id == 0)
                a.Objetivos.Add(o);
            await db.SaveChangesAsync();

            TempData["Ok"] = "Objetivo guardado.";
            return RedirectToAction(nameof(Detalle), new { id = a.Id });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarObjetivo(int asignacionId, int id)
        {
            var a = await evaluacionService.AsignacionDelEvaluadorAsync(asignacionId, UserId);
            var o = a?.Objetivos.FirstOrDefault(x => x.Id == id);
            if (a is null || o is null) return NotFound();
            if (Bloqueado(evaluacionService.ValidarEdicionObjetivos(a), out var r, a.Id)) return r;

            db.Objetivos.Remove(o);
            await db.SaveChangesAsync();
            TempData["Ok"] = "Objetivo eliminado.";
            return RedirectToAction(nameof(Detalle), new { id = a.Id });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirmar(int id)
        {
            var a = await evaluacionService.AsignacionDelEvaluadorAsync(id, UserId);
            if (a is null) return NotFound();
            if (Bloqueado(evaluacionService.ValidarConfirmacion(a), out var r, a.Id)) return r;

            a.Estado = EstadoEvaluacion.Confirmado;
            await db.SaveChangesAsync();
            TempData["Ok"] = "Objetivos confirmados. El colaborador ya puede verlos.";
            return RedirectToAction(nameof(Detalle), new { id });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Reabrir(int id)
        {
            var a = await evaluacionService.AsignacionDelEvaluadorAsync(id, UserId);
            if (a is null) return NotFound();

            if (a.Estado != EstadoEvaluacion.Confirmado || evaluacionService.EtapaVigente(a.PeriodoOperativo!) != TipoEtapa.Registro)
                TempData["Error"] = "Solo se pueden reabrir objetivos confirmados durante la etapa de Registro.";
            else
            {
                a.Estado = EstadoEvaluacion.Borrador;
                await db.SaveChangesAsync();
                TempData["Ok"] = "Objetivos reabiertos para edición.";
            }
            return RedirectToAction(nameof(Detalle), new { id });
        }

        // ---------- Seguimiento ----------

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarAvance(int asignacionId, SeguimientoForm form)
        {
            var a = await evaluacionService.AsignacionDelEvaluadorAsync(asignacionId, UserId);
            var o = a?.Objetivos.FirstOrDefault(x => x.Id == form.ObjetivoId);
            if (a is null || o is null) return NotFound();
            if (Bloqueado(evaluacionService.ValidarSeguimiento(a), out var r, a.Id)) return r;
            if (!ModelState.IsValid)
            {
                TempData["Error"] = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return RedirectToAction(nameof(Detalle), new { id = a.Id });
            }

            o.Seguimientos.Add(new Seguimiento
            {
                Fecha = evaluacionService.Hoy,
                Avance = form.Avance!.Value,
                Comentario = string.IsNullOrWhiteSpace(form.Comentario) ? null : form.Comentario.Trim(),
            });
            await db.SaveChangesAsync();
            TempData["Ok"] = "Avance registrado.";
            return RedirectToAction(nameof(Detalle), new { id = a.Id });
        }

        // ---------- Evaluación ----------

        public async Task<IActionResult> Evaluar(int id)
        {
            var a = await evaluacionService.AsignacionDelEvaluadorAsync(id, UserId);
            if (a is null) return NotFound();
            if (Bloqueado(evaluacionService.ValidarEvaluacion(a), out var r, a.Id)) return r;

            ViewBag.Asignacion = a;
            return View(new EvaluacionForm
            {
                AsignacionId = a.Id,
                // Se sugiere como cumplimiento el último avance (topado en 100).
                Objetivos = a.Objetivos.Select(o => new EvaluacionObjetivoForm
                {
                    ObjetivoId = o.Id,
                    ResultadoFinal = o.Seguimientos.OrderBy(s => s.Fecha).ThenBy(s => s.Id).LastOrDefault()?.Avance,
                    Cumplimiento = (int?)Math.Min(Math.Round(EvaluacionService.AvancePorcentaje(o) ?? 0), 100),
                }).ToList(),
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Evaluar(EvaluacionForm form)
        {
            var a = await evaluacionService.AsignacionDelEvaluadorAsync(form.AsignacionId, UserId);
            if (a is null) return NotFound();
            if (Bloqueado(evaluacionService.ValidarEvaluacion(a), out var r, a.Id)) return r;

            if (a.Objetivos.Any(o => form.Objetivos.All(x => x.ObjetivoId != o.Id)))
                ModelState.AddModelError(string.Empty, "Debe calificar todos los objetivos.");
            if (!ModelState.IsValid)
            {
                ViewBag.Asignacion = a;
                return View(form);
            }

            evaluacionService.CerrarEvaluacion(a, form);
            await db.SaveChangesAsync();
            TempData["Ok"] = $"Evaluación cerrada: {a.PuntajeFinal:0.##} puntos ({a.Calificacion}).";
            return RedirectToAction(nameof(Detalle), new { id = a.Id });
        }

        // ---------- Comentarios ----------

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Comentar(int asignacionId, int? objetivoId, string? texto)
        {
            var a = await evaluacionService.AsignacionDelEvaluadorAsync(asignacionId, UserId);
            if (a is null) return NotFound();
            if (objetivoId is not null && a.Objetivos.All(o => o.Id != objetivoId)) return NotFound();

            texto = texto?.Trim();
            if (string.IsNullOrEmpty(texto) || texto.Length > 1000)
                TempData["Error"] = "El comentario es obligatorio (máximo 1000 caracteres).";
            else
            {
                var autor = await evaluacionService.TrabajadorDelUsuarioAsync(UserId);
                a.Comentarios.Add(new Comentario { ObjetivoId = objetivoId, AutorId = autor!.Id, Texto = texto, Fecha = DateTime.UtcNow });
                await db.SaveChangesAsync();
                TempData["Ok"] = "Comentario enviado.";
            }
            return Redirect(Url.Action(nameof(Detalle), new { id = a.Id }) + "#comentarios");
        }

        private bool Bloqueado(List<string> errores, out IActionResult resultado, int asignacionId)
        {
            resultado = RedirectToAction(nameof(Detalle), new { id = asignacionId });
            if (errores.Count == 0) return false;
            TempData["Error"] = string.Join(" ", errores);
            return true;
        }
    }
}
