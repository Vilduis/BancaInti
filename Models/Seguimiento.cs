namespace BancaInti.Models
{
    // Avance acumulado del objetivo a una fecha.
    public class Seguimiento
    {
        public int Id { get; set; }

        public int ObjetivoId { get; set; }
        public Objetivo? Objetivo { get; set; }

        public DateOnly Fecha { get; set; }
        public decimal Avance { get; set; }
        public string? Comentario { get; set; }
    }
}
