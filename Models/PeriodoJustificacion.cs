namespace BancaInti.Models
{
    // Tipos de justificación que aplican en un periodo operativo.
    public class PeriodoJustificacion
    {
        public int PeriodoOperativoId { get; set; }
        public PeriodoOperativo? PeriodoOperativo { get; set; }

        public int TipoJustificacionId { get; set; }
        public TipoJustificacion? TipoJustificacion { get; set; }
    }
}
