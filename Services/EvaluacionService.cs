using BancaInti.Data;
using BancaInti.Models;
using BancaInti.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace BancaInti.Services
{
    // Flujo de objetivos, seguimiento y evaluación: docs/negocio.md#flujo-de-evaluación
    public class EvaluacionService(ApplicationDbContext db, TimeProvider reloj)
    {
        public DateOnly Hoy => DateOnly.FromDateTime(reloj.GetLocalNow().DateTime);

        public Task<PeriodoOperativo?> PeriodoActivoAsync() =>
            db.PeriodosOperativos
                .Include(p => p.Etapas)
                .Include(p => p.Regla).ThenInclude(r => r!.Rangos)
                .FirstOrDefaultAsync(p => p.Activo);

        public TipoEtapa? EtapaVigente(PeriodoOperativo periodo) =>
            periodo.Etapas.FirstOrDefault(e => e.FechaInicio <= Hoy && Hoy <= e.FechaFin)?.Tipo;

        public Task<Trabajador?> TrabajadorDelUsuarioAsync(string userId) =>
            db.Trabajadores.FirstOrDefaultAsync(t => t.UserId == userId);

        // Carga la asignación solo si pertenece al evaluador del usuario y al periodo activo.
        public Task<AsignacionEvaluador?> AsignacionDelEvaluadorAsync(int asignacionId, string userId) =>
            AsignacionesConDetalle()
                .FirstOrDefaultAsync(a => a.Id == asignacionId && a.Evaluador!.UserId == userId && a.PeriodoOperativo!.Activo);

        // Todo el equipo del evaluador en el periodo activo, con el mismo detalle (reporte del equipo).
        public Task<List<AsignacionEvaluador>> AsignacionesDelEvaluadorAsync(string userId) =>
            AsignacionesConDetalle()
                .Where(a => a.Evaluador!.UserId == userId && a.PeriodoOperativo!.Activo)
                .OrderBy(a => a.Evaluado!.Apellidos)
                .ToListAsync();

        // Asignación del usuario como evaluado en el periodo activo.
        public Task<AsignacionEvaluador?> AsignacionDelEvaluadoAsync(string userId) =>
            AsignacionesConDetalle()
                .FirstOrDefaultAsync(a => a.Evaluado!.UserId == userId && a.PeriodoOperativo!.Activo);

        private IQueryable<AsignacionEvaluador> AsignacionesConDetalle() =>
            db.Asignaciones
                .Include(a => a.PeriodoOperativo).ThenInclude(p => p!.Etapas)
                .Include(a => a.PeriodoOperativo).ThenInclude(p => p!.Regla).ThenInclude(r => r!.Rangos)
                .Include(a => a.Evaluador)
                .Include(a => a.Evaluado)
                .Include(a => a.Objetivos.OrderBy(o => o.Id)).ThenInclude(o => o.Seguimientos)
                .Include(a => a.Comentarios.OrderBy(c => c.Fecha)).ThenInclude(c => c.Autor)
                .AsSplitQuery();

        public List<string> ValidarObjetivo(AsignacionEvaluador a, ObjetivoForm f)
        {
            var errores = ValidarEdicionObjetivos(a);
            if (errores.Count > 0)
                return errores;

            var regla = a.PeriodoOperativo!.Regla!;
            if (f.Peso < regla.PesoMinimo || f.Peso > regla.PesoMaximo)
                errores.Add($"El peso debe estar entre {regla.PesoMinimo} % y {regla.PesoMaximo} %.");

            var otros = a.Objetivos.Where(o => o.Id != f.Id).ToList();
            if (f.Id == 0 && otros.Count >= regla.MaxObjetivos)
                errores.Add($"No puede registrar más de {regla.MaxObjetivos} objetivos.");
            var suma = otros.Sum(o => o.Peso) + (f.Peso ?? 0);
            if (suma > 100)
                errores.Add($"La suma de pesos sería {suma} %; no puede superar 100 %.");
            return errores;
        }

        // Objetivos editables: etapa Registro y estado Borrador.
        public List<string> ValidarEdicionObjetivos(AsignacionEvaluador a)
        {
            if (EtapaVigente(a.PeriodoOperativo!) != TipoEtapa.Registro)
                return ["Los objetivos solo se registran durante la etapa de Registro."];
            if (a.Estado != EstadoEvaluacion.Borrador)
                return ["Los objetivos ya fueron confirmados. Reábralos para modificarlos."];
            return [];
        }

        public List<string> ValidarConfirmacion(AsignacionEvaluador a)
        {
            var errores = ValidarEdicionObjetivos(a);
            if (errores.Count > 0)
                return errores;

            var regla = a.PeriodoOperativo!.Regla!;
            if (a.Objetivos.Count < regla.MinObjetivos)
                errores.Add($"Debe registrar al menos {regla.MinObjetivos} objetivos.");
            var suma = a.Objetivos.Sum(o => o.Peso);
            if (suma != 100)
                errores.Add($"La suma de pesos es {suma} %; debe ser 100 %.");
            return errores;
        }

        public List<string> ValidarSeguimiento(AsignacionEvaluador a)
        {
            if (EtapaVigente(a.PeriodoOperativo!) != TipoEtapa.Seguimiento)
                return ["El avance solo se registra durante la etapa de Seguimiento."];
            if (a.Estado != EstadoEvaluacion.Confirmado)
                return ["Los objetivos deben estar confirmados para registrar avance."];
            return [];
        }

        public List<string> ValidarEvaluacion(AsignacionEvaluador a)
        {
            if (EtapaVigente(a.PeriodoOperativo!) != TipoEtapa.Evaluacion)
                return ["La evaluación solo se registra durante la etapa de Evaluación."];
            if (a.Estado != EstadoEvaluacion.Confirmado)
                return [a.Estado == EstadoEvaluacion.Evaluado ? "La evaluación ya fue cerrada." : "Los objetivos no fueron confirmados."];
            return [];
        }

        public void CerrarEvaluacion(AsignacionEvaluador a, EvaluacionForm f)
        {
            foreach (var o in a.Objetivos)
            {
                var ev = f.Objetivos.First(x => x.ObjetivoId == o.Id);
                o.ResultadoFinal = ev.ResultadoFinal;
                o.Cumplimiento = ev.Cumplimiento;
            }
            a.PuntajeFinal = Puntaje(a.Objetivos);
            a.Calificacion = Calificar(a.PeriodoOperativo!.Regla!.Rangos, a.PuntajeFinal.Value);
            a.ComentarioFinal = f.ComentarioFinal.Trim();
            a.FechaCierre = DateTime.UtcNow;
            a.Estado = EstadoEvaluacion.Evaluado;
        }

        public static string EstadoTexto(EstadoEvaluacion e) => e switch
        {
            EstadoEvaluacion.Confirmado => "Objetivos confirmados",
            EstadoEvaluacion.Evaluado => "Evaluado",
            _ => "Borrador",
        };

        // Clase CSS de la insignia de calificación (site.css). Rangos con otro nombre salen en gris.
        public static string ClaseCalificacion(string? calificacion) => calificacion switch
        {
            "No cumple" => "calif-no-cumple",
            "Bueno" => "calif-bueno",
            "Muy bueno" => "calif-muy-bueno",
            "Sobresaliente" => "calif-sobresaliente",
            _ => "calif-otra",
        };

        // Último avance registrado como % de la meta.
        public static decimal? AvancePorcentaje(Objetivo o)
        {
            var ultimo = o.Seguimientos.OrderBy(s => s.Fecha).ThenBy(s => s.Id).LastOrDefault();
            return ultimo is null ? null : Math.Round(ultimo.Avance / o.Meta * 100, 1);
        }

        // Avance del evaluado ponderado por peso (cada objetivo topa en 100 %).
        public static decimal AvancePonderado(IEnumerable<Objetivo> objetivos) =>
            Math.Round(objetivos.Sum(o => Math.Min(AvancePorcentaje(o) ?? 0, 100) * o.Peso / 100), 1);

        public static decimal Puntaje(IEnumerable<Objetivo> objetivos) =>
            Math.Round(objetivos.Sum(o => (decimal)(o.Cumplimiento ?? 0) * o.Peso / 100), 2);

        // El puntaje se redondea al entero para ubicarlo en los rangos (74.5 → 75).
        public static string Calificar(IEnumerable<RangoCalificacion> rangos, decimal puntaje)
        {
            var entero = (int)Math.Round(puntaje, MidpointRounding.AwayFromZero);
            return rangos.FirstOrDefault(r => r.PuntajeMinimo <= entero && entero <= r.PuntajeMaximo)?.Nombre ?? "Sin rango";
        }
    }
}
