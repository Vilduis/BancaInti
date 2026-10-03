using BancaInti.Constants;
using System.ComponentModel.DataAnnotations;

namespace BancaInti.Models.ViewModels
{
    public class PeriodoEstrategicoForm
    {
        public int Id { get; set; }

        [Required(ErrorMessage = Mensajes.Requerido), StringLength(80)]
        public string Nombre { get; set; } = "";

        [Display(Name = "Fecha inicio"), Required(ErrorMessage = Mensajes.Requerido)]
        public DateOnly? FechaInicio { get; set; }

        [Display(Name = "Fecha fin"), Required(ErrorMessage = Mensajes.Requerido)]
        public DateOnly? FechaFin { get; set; }
    }
}
