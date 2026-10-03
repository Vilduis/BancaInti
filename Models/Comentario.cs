namespace BancaInti.Models
{
    // Conversación entre evaluador y evaluado; opcionalmente sobre un objetivo.
    public class Comentario
    {
        public int Id { get; set; }

        public int AsignacionId { get; set; }
        public AsignacionEvaluador? Asignacion { get; set; }

        public int? ObjetivoId { get; set; }
        public Objetivo? Objetivo { get; set; }

        public int AutorId { get; set; }
        public Trabajador? Autor { get; set; }

        public string Texto { get; set; } = "";
        public DateTime Fecha { get; set; }
    }
}
