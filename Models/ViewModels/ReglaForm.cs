using BancaInti.Constants;
using System.ComponentModel.DataAnnotations;

namespace BancaInti.Models.ViewModels
{
    public class ReglaForm
    {
        public int PeriodoOperativoId { get; set; }
        public string PeriodoNombre { get; set; } = "";

        [Display(Name = "Mínimo de objetivos"), Required(ErrorMessage = Mensajes.Requerido), Range(1, 20, ErrorMessage = Mensajes.Rango)]
        public int MinObjetivos { get; set; }

        [Display(Name = "Máximo de objetivos"), Required(ErrorMessage = Mensajes.Requerido), Range(1, 20, ErrorMessage = Mensajes.Rango)]
        public int MaxObjetivos { get; set; }

        [Display(Name = "Peso mínimo (%)"), Required(ErrorMessage = Mensajes.Requerido), Range(1, 100, ErrorMessage = Mensajes.Rango)]
        public int PesoMinimo { get; set; }

        [Display(Name = "Peso máximo (%)"), Required(ErrorMessage = Mensajes.Requerido), Range(1, 100, ErrorMessage = Mensajes.Rango)]
        public int PesoMaximo { get; set; }

        [Display(Name = "Días mínimos de participación"), Required(ErrorMessage = Mensajes.Requerido), Range(0, 366, ErrorMessage = Mensajes.Rango)]
        public int DiasMinimos { get; set; }

        public List<RangoForm> Rangos { get; set; } = [];
    }

    public class RangoForm
    {
        [Display(Name = "Calificación"), Required(ErrorMessage = Mensajes.Requerido), StringLength(40)]
        public string Nombre { get; set; } = "";

        [Display(Name = "Desde"), Required(ErrorMessage = Mensajes.Requerido), Range(0, 100, ErrorMessage = Mensajes.Rango)]
        public int PuntajeMinimo { get; set; }

        [Display(Name = "Hasta"), Required(ErrorMessage = Mensajes.Requerido), Range(0, 100, ErrorMessage = Mensajes.Rango)]
        public int PuntajeMaximo { get; set; }
    }
}
