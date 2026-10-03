namespace BancaInti.Models.ViewModels
{
    public class AsignacionesViewModel
    {
        public PeriodoOperativo? Periodo { get; set; }
        public List<PeriodoOperativo> Periodos { get; set; } = [];
        public List<Trabajador> Evaluadores { get; set; } = [];

        // Asignaciones agrupadas por evaluador.
        public List<IGrouping<Trabajador, AsignacionEvaluador>> Equipos { get; set; } = [];

        // Trabajadores aún sin evaluador en el periodo.
        public List<Trabajador> SinAsignar { get; set; } = [];

        public Dictionary<int, Participacion> Participacion { get; set; } = [];
    }

    public record Participacion(int DiasPeriodo, int DiasJustificados)
    {
        public int DiasEfectivos => DiasPeriodo - DiasJustificados;
    }
}
