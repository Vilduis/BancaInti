using BancaInti.Models;
using BancaInti.Models.ViewModels;

namespace BancaInti.Services
{
    // Reglas de evaluación: docs/negocio.md#reglas-de-evaluación-por-periodo-operativo
    public class ReglaService
    {
        public ReglaEvaluacion ReglaPorDefecto() => new()
        {
            MinObjetivos = 3,
            MaxObjetivos = 6,
            PesoMinimo = 10,
            PesoMaximo = 40,
            DiasMinimos = 90,
            Rangos =
            [
                new() { Nombre = "No cumple", PuntajeMinimo = 0, PuntajeMaximo = 74 },
                new() { Nombre = "Bueno", PuntajeMinimo = 75, PuntajeMaximo = 79 },
                new() { Nombre = "Muy bueno", PuntajeMinimo = 80, PuntajeMaximo = 89 },
                new() { Nombre = "Sobresaliente", PuntajeMinimo = 90, PuntajeMaximo = 100 },
            ],
        };

        public List<string> Validar(ReglaForm f)
        {
            var errores = new List<string>();
            if (f.MinObjetivos > f.MaxObjetivos)
                errores.Add("El mínimo de objetivos no puede ser mayor que el máximo.");
            if (f.PesoMinimo > f.PesoMaximo)
                errores.Add("El peso mínimo no puede ser mayor que el peso máximo.");
            if (f.PesoMaximo * f.MaxObjetivos < 100)
                errores.Add("Con el máximo de objetivos y el peso máximo no se puede llegar a 100 %.");
            if (f.PesoMinimo * f.MinObjetivos > 100)
                errores.Add("Con el mínimo de objetivos y el peso mínimo se supera el 100 %.");

            // Los rangos deben cubrir 0–100 sin huecos ni cruces.
            var rangos = f.Rangos.OrderBy(r => r.PuntajeMinimo).ToList();
            if (rangos.Count == 0)
            {
                errores.Add("Debe registrar al menos un rango de calificación.");
                return errores;
            }
            if (rangos.Any(r => r.PuntajeMinimo > r.PuntajeMaximo))
                errores.Add("En cada rango, 'Desde' no puede ser mayor que 'Hasta'.");
            if (rangos[0].PuntajeMinimo != 0 || rangos[^1].PuntajeMaximo != 100)
                errores.Add("Los rangos deben empezar en 0 y terminar en 100.");
            for (int i = 1; i < rangos.Count; i++)
            {
                if (rangos[i].PuntajeMinimo != rangos[i - 1].PuntajeMaximo + 1)
                    errores.Add($"Hay un hueco o cruce entre '{rangos[i - 1].Nombre}' y '{rangos[i].Nombre}'.");
            }
            return errores;
        }

        public static ReglaForm ToForm(ReglaEvaluacion r, string periodoNombre) => new()
        {
            PeriodoOperativoId = r.PeriodoOperativoId,
            PeriodoNombre = periodoNombre,
            MinObjetivos = r.MinObjetivos,
            MaxObjetivos = r.MaxObjetivos,
            PesoMinimo = r.PesoMinimo,
            PesoMaximo = r.PesoMaximo,
            DiasMinimos = r.DiasMinimos,
            Rangos = r.Rangos.OrderBy(x => x.PuntajeMinimo)
                .Select(x => new RangoForm { Nombre = x.Nombre, PuntajeMinimo = x.PuntajeMinimo, PuntajeMaximo = x.PuntajeMaximo })
                .ToList(),
        };

        public static void Aplicar(ReglaEvaluacion r, ReglaForm f)
        {
            r.MinObjetivos = f.MinObjetivos;
            r.MaxObjetivos = f.MaxObjetivos;
            r.PesoMinimo = f.PesoMinimo;
            r.PesoMaximo = f.PesoMaximo;
            r.DiasMinimos = f.DiasMinimos;
            r.Rangos.Clear();
            r.Rangos.AddRange(f.Rangos.Select(x => new RangoCalificacion
            {
                Nombre = x.Nombre.Trim(),
                PuntajeMinimo = x.PuntajeMinimo,
                PuntajeMaximo = x.PuntajeMaximo,
            }));
        }
    }
}
