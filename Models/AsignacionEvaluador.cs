namespace BancaInti.Models
{
    // Un evaluado tiene un único evaluador por periodo operativo.
    public class AsignacionEvaluador
    {
        public int Id { get; set; }

        public int PeriodoOperativoId { get; set; }
        public PeriodoOperativo? PeriodoOperativo { get; set; }

        public int EvaluadorId { get; set; }
        public Trabajador? Evaluador { get; set; }

        public int EvaluadoId { get; set; }
        public Trabajador? Evaluado { get; set; }

        public EstadoEvaluacion Estado { get; set; }
        public decimal? PuntajeFinal { get; set; }
        public string? Calificacion { get; set; }
        public string? ComentarioFinal { get; set; }
        public DateTime? FechaCierre { get; set; }

        public List<Objetivo> Objetivos { get; set; } = [];
        public List<Comentario> Comentarios { get; set; } = [];
    }
}
