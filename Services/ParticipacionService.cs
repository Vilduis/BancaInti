using BancaInti.Data;
using BancaInti.Models;
using BancaInti.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace BancaInti.Services
{
    // Días efectivos: docs/negocio.md#participación--90-días
    public class ParticipacionService(ApplicationDbContext db)
    {
        public async Task<Dictionary<int, Participacion>> CalcularAsync(PeriodoOperativo periodo, IEnumerable<Trabajador> trabajadores)
        {
            var ids = trabajadores.Select(t => t.Id).ToList();
            var tiposQueAplican = await db.PeriodosJustificacion
                .Where(pj => pj.PeriodoOperativoId == periodo.Id)
                .Select(pj => pj.TipoJustificacionId)
                .ToListAsync();
            var justificaciones = await db.JustificacionesTrabajador
                .Where(j => ids.Contains(j.TrabajadorId) && tiposQueAplican.Contains(j.TipoJustificacionId)
                            && j.FechaInicio <= periodo.FechaFin && j.FechaFin >= periodo.FechaInicio)
                .ToListAsync();
            var porTrabajador = justificaciones.ToLookup(j => j.TrabajadorId);

            return trabajadores.ToDictionary(t => t.Id, t => Calcular(periodo, t, porTrabajador[t.Id]));
        }

        public static Participacion Calcular(PeriodoOperativo periodo, Trabajador t, IEnumerable<JustificacionTrabajador> justificaciones)
        {
            // Tramo del trabajador dentro del periodo (días calendario, extremos incluidos).
            var desde = Max(t.FechaIngreso, periodo.FechaInicio);
            var hasta = Min(t.FechaCese ?? periodo.FechaFin, periodo.FechaFin);
            if (hasta < desde)
                return new Participacion(0, 0);

            // Unión de días justificados dentro del tramo (las justificaciones pueden solaparse).
            var dias = new HashSet<int>();
            foreach (var j in justificaciones)
            {
                var ini = Max(j.FechaInicio, desde).DayNumber;
                var fin = Min(j.FechaFin, hasta).DayNumber;
                for (var d = ini; d <= fin; d++)
                    dias.Add(d);
            }
            return new Participacion(hasta.DayNumber - desde.DayNumber + 1, dias.Count);
        }

        private static DateOnly Max(DateOnly a, DateOnly b) => a > b ? a : b;
        private static DateOnly Min(DateOnly a, DateOnly b) => a < b ? a : b;
    }
}
