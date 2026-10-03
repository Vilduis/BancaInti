using BancaInti.Constants;
using System.ComponentModel.DataAnnotations;

namespace BancaInti.Models
{
    public class PeriodoEstrategico
    {
        public int Id { get; set; }

        [Required(ErrorMessage = Mensajes.Requerido), StringLength(80)]
        public string Nombre { get; set; } = "";

        [Display(Name = "Fecha inicio")]
        public DateOnly FechaInicio { get; set; }

        [Display(Name = "Fecha fin")]
        public DateOnly FechaFin { get; set; }

        public List<PeriodoOperativo> PeriodosOperativos { get; set; } = [];
    }
}
