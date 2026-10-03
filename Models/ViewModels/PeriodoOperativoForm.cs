using BancaInti.Constants;
using System.ComponentModel.DataAnnotations;

namespace BancaInti.Models.ViewModels
{
    public class PeriodoOperativoForm
    {
        public int Id { get; set; }

        [Display(Name = "Periodo estratégico"), Required(ErrorMessage = Mensajes.Requerido)]
        public int? PeriodoEstrategicoId { get; set; }

        [Display(Name = "Año"), Required(ErrorMessage = Mensajes.Requerido), Range(2000, 2100, ErrorMessage = Mensajes.Rango)]
        public int? Anio { get; set; }

        [Display(Name = "Fecha inicio"), Required(ErrorMessage = Mensajes.Requerido)]
        public DateOnly? FechaInicio { get; set; }

        [Display(Name = "Fecha fin"), Required(ErrorMessage = Mensajes.Requerido)]
        public DateOnly? FechaFin { get; set; }

        [Display(Name = "Mensaje para el colaborador"), StringLength(1000)]
        public string? MensajeColaborador { get; set; }
    }
}
