namespace BancaInti.Models
{
    public class Objetivo
    {
        public int Id { get; set; }

        public int AsignacionId { get; set; }
        public AsignacionEvaluador? Asignacion { get; set; }

        public string Descripcion { get; set; } = "";
        public string Indicador { get; set; } = "";
        public string UnidadMedida { get; set; } = "";
        public int Peso { get; set; }
        public decimal Meta { get; set; }

        // Etapa Evaluación.
        public decimal? ResultadoFinal { get; set; }
        public int? Cumplimiento { get; set; }

        public List<Seguimiento> Seguimientos { get; set; } = [];
    }
}
