using BancaInti.Data;
using BancaInti.Models;
using BancaInti.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace BancaInti.Services
{
    // Consultas de RRHH: docs/negocio.md#consultas-admin
    public class ConsultaService(ApplicationDbContext db, AsignacionService asignacionService,
        ParticipacionService participacionService, EvaluacionService evaluacionService)
    {
        public async Task<TablaConsulta> ParticipantesAsync(PeriodoOperativo periodo, bool incluirNoParticipan)
        {
            var minimo = await DiasMinimosAsync(periodo.Id);
            var trabajadores = await asignacionService.Elegibles(periodo).OrderBy(t => t.Apellidos).ToListAsync();
            var dias = await participacionService.CalcularAsync(periodo, trabajadores);

            var filas = trabajadores
                .Select(t => (t, p: dias[t.Id]))
                .Where(x => incluirNoParticipan || x.p.DiasEfectivos > minimo)
                .Select(x => new object?[]
                {
                    x.t.Codigo, x.t.NombreCompleto, x.t.Area, x.t.Cargo, x.t.FechaIngreso, x.t.FechaCese,
                    x.p.DiasPeriodo, x.p.DiasJustificados, x.p.DiasEfectivos, x.p.DiasEfectivos > minimo,
                })
                .ToList();

            return new TablaConsulta
            {
                Titulo = "Lista de participantes",
                Subtitulo = $"Periodo {periodo.Anio}. Días efectivos = días en el periodo menos justificaciones; participan quienes superan {minimo} días.",
                Archivo = $"Participantes_{periodo.Anio}",
                Filtros = "_FiltroParticipantes",
                Columnas = ["Código", "Trabajador", "Área", "Cargo", "Ingreso", "Cese", "Días en periodo", "Días justificados", "Días efectivos", "Participa"],
                ColumnasFiltro = ["Área", "Cargo", "Participa"],
                Filas = filas,
            };
        }

        public async Task<TablaConsulta> JustificacionesAsync(PeriodoOperativo periodo)
        {
            var aplican = await db.PeriodosJustificacion.Where(pj => pj.PeriodoOperativoId == periodo.Id)
                .Select(pj => pj.TipoJustificacionId).ToListAsync();
            var lista = await db.JustificacionesTrabajador
                .Include(j => j.Trabajador)
                .Include(j => j.TipoJustificacion)
                .Where(j => j.FechaInicio <= periodo.FechaFin && j.FechaFin >= periodo.FechaInicio)
                .OrderBy(j => j.Trabajador!.Apellidos).ThenBy(j => j.FechaInicio)
                .ToListAsync();

            return new TablaConsulta
            {
                Titulo = "Justificaciones por trabajador",
                Subtitulo = $"Periodo {periodo.Anio}. Ausencias de cada trabajador que se cruzan con las fechas del periodo.",
                Archivo = $"Justificaciones_{periodo.Anio}",
                Filtros = "_FiltroPeriodo",
                Columnas = ["Código", "Trabajador", "Área", "Tipo", "Inicio", "Fin", "Días", "Aplica en el periodo", "Observación"],
                ColumnasFiltro = ["Trabajador", "Área", "Tipo", "Aplica en el periodo"],
                Filas = lista.Select(j => new object?[]
                {
                    j.Trabajador!.Codigo, j.Trabajador.NombreCompleto, j.Trabajador.Area, j.TipoJustificacion!.Nombre,
                    j.FechaInicio, j.FechaFin, j.Dias, aplican.Contains(j.TipoJustificacionId), j.Observacion,
                }).ToList(),
            };
        }

        // Trabajadores vigentes (sin cese a la fecha) con edad mayor a la indicada.
        public async Task<TablaConsulta> MayoresDeAsync(int edad)
        {
            var hoy = evaluacionService.Hoy;
            var trabajadores = await db.Trabajadores
                .Where(t => t.FechaCese == null || t.FechaCese >= hoy)
                .OrderBy(t => t.FechaNacimiento)
                .ToListAsync();

            return new TablaConsulta
            {
                Titulo = $"Trabajadores mayores de {edad} años",
                Subtitulo = $"Trabajadores vigentes (sin cese) con su edad calculada al {hoy:dd/MM/yyyy}.",
                Archivo = $"Mayores_de_{edad}",
                Filtros = "_FiltroMayores",
                Columnas = ["Código", "Trabajador", "Área", "Cargo", "Nacimiento", "Edad", "Ingreso", "Años de servicio"],
                ColumnasFiltro = ["Área", "Cargo"],
                Filas = trabajadores
                    .Select(t => (t, edad: Anios(t.FechaNacimiento, hoy)))
                    .Where(x => x.edad > edad)
                    .Select(x => new object?[]
                    {
                        x.t.Codigo, x.t.NombreCompleto, x.t.Area, x.t.Cargo, x.t.FechaNacimiento, x.edad,
                        x.t.FechaIngreso, Anios(x.t.FechaIngreso, hoy),
                    }).ToList(),
            };
        }

        public async Task<TablaConsulta> EvaluadoresAsync(PeriodoOperativo periodo)
        {
            var asignaciones = await db.Asignaciones
                .Include(a => a.Evaluador)
                .Include(a => a.Evaluado)
                .Include(a => a.Objetivos).ThenInclude(o => o.Seguimientos)
                .Where(a => a.PeriodoOperativoId == periodo.Id)
                .OrderBy(a => a.Evaluador!.Apellidos).ThenBy(a => a.Evaluado!.Apellidos)
                .AsSplitQuery()
                .ToListAsync();

            return new TablaConsulta
            {
                Titulo = "Evaluadores y sus evaluados",
                Subtitulo = $"Periodo {periodo.Anio}. Equipo de cada evaluador con el estado, avance y resultado de cada evaluado.",
                Archivo = $"Evaluadores_{periodo.Anio}",
                Filtros = "_FiltroPeriodo",
                Columnas = ["Evaluador", "Área evaluador", "Evaluado", "Cargo evaluado", "Estado", "Objetivos", "Peso total", "Avance ponderado (%)", "Puntaje", "Calificación"],
                ColumnasFiltro = ["Evaluador", "Área evaluador", "Estado", "Calificación"],
                Filas = asignaciones.Select(a => new object?[]
                {
                    a.Evaluador!.NombreCompleto, a.Evaluador.Area, a.Evaluado!.NombreCompleto, a.Evaluado.Cargo,
                    EvaluacionService.EstadoTexto(a.Estado), a.Objetivos.Count, a.Objetivos.Sum(o => o.Peso),
                    EvaluacionService.AvancePonderado(a.Objetivos), a.PuntajeFinal, a.Calificacion,
                }).ToList(),
            };
        }

        public async Task<TablaConsulta> AuditoriaAsync(DateOnly desde, DateOnly hasta, string? usuario, string? entidad)
        {
            // Rango en hora local (Perú) convertido a UTC.
            var ini = desde.ToDateTime(TimeOnly.MinValue, DateTimeKind.Local).ToUniversalTime();
            var fin = hasta.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Local).ToUniversalTime();
            var query = db.LogsAuditoria.Where(l => l.Fecha >= ini && l.Fecha < fin);
            if (!string.IsNullOrWhiteSpace(usuario))
                query = query.Where(l => l.Usuario.Contains(usuario.Trim()));
            if (!string.IsNullOrWhiteSpace(entidad))
                query = query.Where(l => l.Entidad == entidad);

            const int maximo = 2000;
            var logs = await query.OrderByDescending(l => l.Id).Take(maximo).ToListAsync();

            return new TablaConsulta
            {
                Titulo = "Auditoría",
                Subtitulo = $"Quién creó, modificó o eliminó cada dato, del {desde:dd/MM/yyyy} al {hasta:dd/MM/yyyy}." + (logs.Count == maximo ? $" Se muestran los {maximo} más recientes." : ""),
                Archivo = $"Auditoria_{desde:yyyyMMdd}_{hasta:yyyyMMdd}",
                Filtros = "_FiltroAuditoria",
                Columnas = ["Fecha", "Usuario", "Acción", "Entidad", "Id", "Valores anteriores", "Valores nuevos", "IP"],
                ColumnasFiltro = ["Acción"],
                Filas = logs.Select(l => new object?[]
                {
                    l.Fecha.ToLocalTime(), l.Usuario, l.Accion, l.Entidad, l.EntidadId, l.ValoresAnteriores, l.ValoresNuevos, l.Ip,
                }).ToList(),
            };
        }

        public Task<List<string>> EntidadesAuditadasAsync() =>
            db.LogsAuditoria.Select(l => l.Entidad).Distinct().OrderBy(e => e).ToListAsync();

        private async Task<int> DiasMinimosAsync(int periodoId) =>
            await db.ReglasEvaluacion.Where(r => r.PeriodoOperativoId == periodoId)
                .Select(r => (int?)r.DiasMinimos).FirstOrDefaultAsync() ?? 90;

        private static int Anios(DateOnly desde, DateOnly hasta)
        {
            var anios = hasta.Year - desde.Year;
            return desde.AddYears(anios) > hasta ? anios - 1 : anios;
        }
    }
}
