using BancaInti.Data;
using BancaInti.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace BancaInti.Services
{
    public class JustificacionService(ApplicationDbContext db)
    {
        public async Task<List<string>> ValidarAsync(JustificacionForm f)
        {
            var errores = new List<string>();
            var inicio = f.FechaInicio!.Value;
            var fin = f.FechaFin!.Value;

            if (fin < inicio)
                errores.Add("La fecha fin no puede ser anterior a la fecha inicio.");

            var trabajador = await db.Trabajadores.FindAsync(f.TrabajadorId);
            if (trabajador is null)
                return ["El trabajador no existe."];
            if (inicio < trabajador.FechaIngreso)
                errores.Add($"La justificación no puede iniciar antes del ingreso del trabajador ({trabajador.FechaIngreso:dd/MM/yyyy}).");
            if (trabajador.FechaCese is { } cese && fin > cese)
                errores.Add($"La justificación no puede terminar después del cese del trabajador ({cese:dd/MM/yyyy}).");

            var tipo = await db.TiposJustificacion.FindAsync(f.TipoJustificacionId);
            if (tipo is null || (!tipo.Activo && f.Id == 0))
                errores.Add("Seleccione un tipo de justificación activo.");

            var cruce = await db.JustificacionesTrabajador.AnyAsync(j =>
                j.TrabajadorId == f.TrabajadorId && j.Id != f.Id && j.FechaInicio <= fin && inicio <= j.FechaFin);
            if (cruce)
                errores.Add("Las fechas se cruzan con otra justificación del mismo trabajador.");

            return errores;
        }
    }
}
