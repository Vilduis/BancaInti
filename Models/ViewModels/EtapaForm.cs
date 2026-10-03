using BancaInti.Constants;
using System.ComponentModel.DataAnnotations;

namespace BancaInti.Models.ViewModels
{
    public class EtapaForm
    {
        public int Id { get; set; }

        [Display(Name = "Periodo operativo"), Required(ErrorMessage = Mensajes.Requerido)]
        public int? PeriodoOperativoId { get; set; }

        [Display(Name = "Etapa"), Required(ErrorMessage = Mensajes.Requerido)]
        public TipoEtapa? Tipo { get; set; }

        [Display(Name = "Fecha inicio"), Required(ErrorMessage = Mensajes.Requerido)]
        public DateOnly? FechaInicio { get; set; }

        [Display(Name = "Fecha fin"), Required(ErrorMessage = Mensajes.Requerido)]
        public DateOnly? FechaFin { get; set; }
    }
}
