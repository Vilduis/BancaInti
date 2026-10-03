using BancaInti.Data;
using BancaInti.Models;
using BancaInti.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace BancaInti.Services
{
    // Reglas de periodos y etapas: docs/negocio.md#jerarquía-de-periodos
    public class PeriodoService(ApplicationDbContext db, ReglaService reglaService)
    {
        public static string NombreEtapa(TipoEtapa tipo) => tipo == TipoEtapa.Evaluacion ? "Evaluación" : tipo.ToString();

        public async Task<List<string>> ValidarEstrategicoAsync(PeriodoEstrategicoForm f)
        {
            var errores = new List<string>();
            if (f.FechaFin <= f.FechaInicio)
                errores.Add("La fecha fin debe ser posterior a la fecha inicio.");

            var cruce = await db.PeriodosEstrategicos.AnyAsync(p =>
                p.Id != f.Id && p.FechaInicio <= f.FechaFin && f.FechaInicio <= p.FechaFin);
            if (cruce)
                errores.Add("Las fechas se cruzan con otro periodo estratégico.");

            if (f.Id != 0)
            {
                var fuera = await db.PeriodosOperativos.AnyAsync(p =>
                    p.PeriodoEstrategicoId == f.Id && (p.FechaInicio < f.FechaInicio || p.FechaFin > f.FechaFin));
                if (fuera)
                    errores.Add("Hay periodos operativos que quedarían fuera de las nuevas fechas.");
            }
            return errores;
        }

        public async Task<List<string>> ValidarOperativoAsync(PeriodoOperativoForm f)
        {
            var errores = new List<string>();
            var inicio = f.FechaInicio!.Value;
            var fin = f.FechaFin!.Value;

            if (fin <= inicio)
                errores.Add("La fecha fin debe ser posterior a la fecha inicio.");

            var estrategico = await db.PeriodosEstrategicos.FindAsync(f.PeriodoEstrategicoId);
            if (estrategico is null)
                errores.Add("El periodo estratégico no existe.");
            else if (inicio < estrategico.FechaInicio || fin > estrategico.FechaFin)
                errores.Add($"Las fechas deben estar dentro del periodo estratégico ({estrategico.FechaInicio:dd/MM/yyyy} – {estrategico.FechaFin:dd/MM/yyyy}).");

            if (await db.PeriodosOperativos.AnyAsync(p => p.Id != f.Id && p.Anio == f.Anio))
                errores.Add($"Ya existe un periodo operativo para el año {f.Anio}.");

            if (await db.PeriodosOperativos.AnyAsync(p => p.Id != f.Id && p.FechaInicio <= fin && inicio <= p.FechaFin))
                errores.Add("Las fechas se cruzan con otro periodo operativo.");

            if (f.Id != 0 && await db.Etapas.AnyAsync(e => e.PeriodoOperativoId == f.Id && (e.FechaInicio < inicio || e.FechaFin > fin)))
                errores.Add("Hay etapas que quedarían fuera de las nuevas fechas. Ajústelas primero en Etapas.");
            return errores;
        }

        // Una etapa por tipo, dentro del operativo y en orden Registro → Seguimiento → Evaluación sin cruzarse.
        public async Task<List<string>> ValidarEtapaAsync(EtapaForm f)
        {
            var errores = new List<string>();
            var inicio = f.FechaInicio!.Value;
            var fin = f.FechaFin!.Value;
            var tipo = f.Tipo!.Value;

            if (fin < inicio)
                errores.Add("La fecha fin no puede ser anterior a la fecha inicio.");

            var periodo = await db.PeriodosOperativos.Include(p => p.Etapas).FirstOrDefaultAsync(p => p.Id == f.PeriodoOperativoId);
            if (periodo is null)
            {
                errores.Add("El periodo operativo no existe.");
                return errores;
            }
            if (inicio < periodo.FechaInicio || fin > periodo.FechaFin)
                errores.Add($"La etapa debe estar dentro del periodo {periodo.Anio} ({periodo.FechaInicio:dd/MM/yyyy} – {periodo.FechaFin:dd/MM/yyyy}).");

            var otras = periodo.Etapas.Where(e => e.Id != f.Id).ToList();
            if (otras.Any(e => e.Tipo == tipo))
                errores.Add($"El periodo {periodo.Anio} ya tiene la etapa {NombreEtapa(tipo)}.");
            foreach (var e in otras.Where(e => e.Tipo < tipo && e.FechaFin >= inicio))
                errores.Add($"Debe iniciar después de que termine {NombreEtapa(e.Tipo)} ({e.FechaFin:dd/MM/yyyy}).");
            foreach (var e in otras.Where(e => e.Tipo > tipo && e.FechaInicio <= fin))
                errores.Add($"Debe terminar antes de que inicie {NombreEtapa(e.Tipo)} ({e.FechaInicio:dd/MM/yyyy}).");
            return errores;
        }

        public async Task<PeriodoOperativo> CrearOperativoAsync(PeriodoOperativoForm f)
        {
            var periodo = new PeriodoOperativo();
            Aplicar(periodo, f);

            // Por defecto aplican todos los tipos de justificación activos; RRHH puede ajustarlo.
            var tipos = await db.TiposJustificacion.Where(t => t.Activo).Select(t => t.Id).ToListAsync();
            periodo.Justificaciones = tipos.Select(id => new PeriodoJustificacion { TipoJustificacionId = id }).ToList();
            periodo.Regla = reglaService.ReglaPorDefecto();

            db.PeriodosOperativos.Add(periodo);
            await db.SaveChangesAsync();
            return periodo;
        }

        public async Task ActualizarOperativoAsync(PeriodoOperativo periodo, PeriodoOperativoForm f)
        {
            Aplicar(periodo, f);
            await db.SaveChangesAsync();
        }

        public async Task ActivarAsync(int id)
        {
            // Con entidades rastreadas (no ExecuteUpdate) para que el cambio quede en la auditoría.
            var periodos = await db.PeriodosOperativos.Where(p => p.Activo || p.Id == id).ToListAsync();
            foreach (var p in periodos)
                p.Activo = p.Id == id;
            await db.SaveChangesAsync();
        }

        public static PeriodoOperativoForm ToForm(PeriodoOperativo p) =>
            new()
            {
                Id = p.Id,
                PeriodoEstrategicoId = p.PeriodoEstrategicoId,
                Anio = p.Anio,
                FechaInicio = p.FechaInicio,
                FechaFin = p.FechaFin,
                MensajeColaborador = p.MensajeColaborador,
            };

        private static void Aplicar(PeriodoOperativo p, PeriodoOperativoForm f)
        {
            p.PeriodoEstrategicoId = f.PeriodoEstrategicoId!.Value;
            p.Anio = f.Anio!.Value;
            p.FechaInicio = f.FechaInicio!.Value;
            p.FechaFin = f.FechaFin!.Value;
            p.MensajeColaborador = string.IsNullOrWhiteSpace(f.MensajeColaborador) ? null : f.MensajeColaborador.Trim();
        }
    }
}
