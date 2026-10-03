namespace BancaInti.Models.ViewModels
{
    public class JustificacionesPeriodoForm
    {
        public int PeriodoOperativoId { get; set; }
        public string PeriodoNombre { get; set; } = "";
        public List<int> Seleccionados { get; set; } = [];
        public List<TipoJustificacion> Tipos { get; set; } = [];
    }
}
