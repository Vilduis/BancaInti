namespace BancaInti.Models
{
    public class Etapa
    {
        public int Id { get; set; }

        public int PeriodoOperativoId { get; set; }
        public PeriodoOperativo? PeriodoOperativo { get; set; }

        public TipoEtapa Tipo { get; set; }
        public DateOnly FechaInicio { get; set; }
        public DateOnly FechaFin { get; set; }
    }
}
