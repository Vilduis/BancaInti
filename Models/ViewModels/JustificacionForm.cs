using BancaInti.Constants;
using System.ComponentModel.DataAnnotations;

namespace BancaInti.Models.ViewModels
{
    public class JustificacionForm
    {
        public int Id { get; set; }

        [Display(Name = "Trabajador"), Required(ErrorMessage = Mensajes.Requerido)]
        public int? TrabajadorId { get; set; }

        [Display(Name = "Tipo"), Required(ErrorMessage = Mensajes.Requerido)]
        public int? TipoJustificacionId { get; set; }

        [Display(Name = "Fecha inicio"), Required(ErrorMessage = Mensajes.Requerido)]
        public DateOnly? FechaInicio { get; set; }

        [Display(Name = "Fecha fin"), Required(ErrorMessage = Mensajes.Requerido)]
        public DateOnly? FechaFin { get; set; }

        [Display(Name = "Observación"), StringLength(250)]
        public string? Observacion { get; set; }
    }
}
