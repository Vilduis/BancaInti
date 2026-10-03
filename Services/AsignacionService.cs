using BancaInti.Constants;
using BancaInti.Data;
using BancaInti.Models;
using Microsoft.EntityFrameworkCore;

namespace BancaInti.Services
{
    public class AsignacionService(ApplicationDbContext db)
    {
        // Trabajadores cuyo usuario tiene el rol Evaluador.
        public IQueryable<Trabajador> Evaluadores() =>
            from t in db.Trabajadores
            join ur in db.UserRoles on t.UserId equals ur.UserId
            join r in db.Roles on ur.RoleId equals r.Id
            where r.Name == Roles.Evaluador
            select t;

        // Trabajadores que pueden ser evaluados en el periodo: vigentes durante el periodo.
        public IQueryable<Trabajador> Elegibles(PeriodoOperativo periodo) =>
            db.Trabajadores.Where(t => t.FechaIngreso <= periodo.FechaFin
                                    && (t.FechaCese == null || t.FechaCese >= periodo.FechaInicio));

        public async Task<List<string>> AsignarAsync(int periodoId, int evaluadorId, IReadOnlyCollection<int> evaluadoIds)
        {
            var errores = new List<string>();
            var periodo = await db.PeriodosOperativos.FindAsync(periodoId);
            if (periodo is null)
                return ["El periodo operativo no existe."];
            if (evaluadoIds.Count == 0)
                return ["Seleccione al menos un trabajador a evaluar."];
            if (!await Evaluadores().AnyAsync(t => t.Id == evaluadorId))
                return ["El trabajador seleccionado no tiene el rol Evaluador."];
            if (evaluadoIds.Contains(evaluadorId))
                errores.Add("Un evaluador no puede evaluarse a sí mismo.");

            var yaAsignados = await db.Asignaciones
                .Where(a => a.PeriodoOperativoId == periodoId && evaluadoIds.Contains(a.EvaluadoId))
                .Select(a => a.Evaluado!.Nombres + " " + a.Evaluado.Apellidos)
                .ToListAsync();
            if (yaAsignados.Count > 0)
                errores.Add("Ya tienen evaluador en este periodo: " + string.Join(", ", yaAsignados) + ".");

            var elegibles = await Elegibles(periodo).CountAsync(t => evaluadoIds.Contains(t.Id));
            if (elegibles != evaluadoIds.Count)
                errores.Add("Algunos trabajadores no están vigentes durante el periodo.");

            if (errores.Count > 0)
                return errores;

            db.Asignaciones.AddRange(evaluadoIds.Select(id => new AsignacionEvaluador
            {
                PeriodoOperativoId = periodoId,
                EvaluadorId = evaluadorId,
                EvaluadoId = id,
            }));
            await db.SaveChangesAsync();
            return errores;
        }
    }
}
