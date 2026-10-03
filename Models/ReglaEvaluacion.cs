namespace BancaInti.Models
{
    public class ReglaEvaluacion
    {
        public int Id { get; set; }

        public int PeriodoOperativoId { get; set; }
        public PeriodoOperativo? PeriodoOperativo { get; set; }

        public int MinObjetivos { get; set; }
        public int MaxObjetivos { get; set; }
        public int PesoMinimo { get; set; }
        public int PesoMaximo { get; set; }
        public int DiasMinimos { get; set; }

        public List<RangoCalificacion> Rangos { get; set; } = [];
    }
}
