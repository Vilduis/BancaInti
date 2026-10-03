using System.ComponentModel.DataAnnotations;

namespace BancaInti.Models
{
    // El valor define el orden obligatorio de las etapas.
    public enum TipoEtapa
    {
        Registro = 1,
        Seguimiento = 2,
        [Display(Name = "Evaluación")]
        Evaluacion = 3,
    }
}
