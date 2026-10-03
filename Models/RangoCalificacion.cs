namespace BancaInti.Models
{
    public class RangoCalificacion
    {
        public int Id { get; set; }

        public int ReglaEvaluacionId { get; set; }
        public ReglaEvaluacion? ReglaEvaluacion { get; set; }

        public string Nombre { get; set; } = "";
        public int PuntajeMinimo { get; set; }
        public int PuntajeMaximo { get; set; }
    }
}
