using BancaInti.Constants;
using System.ComponentModel.DataAnnotations;

namespace BancaInti.Models.ViewModels
{
    public class MiEquipoViewModel
    {
        public PeriodoOperativo? Periodo { get; set; }
        public TipoEtapa? Etapa { get; set; }
        public List<AsignacionEvaluador> Asignaciones { get; set; } = [];
        public Dictionary<int, Participacion> Participacion { get; set; } = [];
    }

    public class EvaluadoDetalleViewModel
    {
        public AsignacionEvaluador Asignacion { get; set; } = null!;
        public TipoEtapa? Etapa { get; set; }
        public ReglaEvaluacion Regla { get; set; } = null!;
        public Participacion Participacion { get; set; } = null!;
        public int SumaPesos => Asignacion.Objetivos.Sum(o => o.Peso);
    }

    public class ObjetivoForm
    {
        public int Id { get; set; }
        public int AsignacionId { get; set; }

        [Display(Name = "Descripción"), Required(ErrorMessage = Mensajes.Requerido), StringLength(500)]
        public string Descripcion { get; set; } = "";

        [Display(Name = "Indicador"), Required(ErrorMessage = Mensajes.Requerido), StringLength(200)]
        public string Indicador { get; set; } = "";

        [Display(Name = "Unidad de medida"), Required(ErrorMessage = Mensajes.Requerido), StringLength(30)]
        public string UnidadMedida { get; set; } = "";

        [Display(Name = "Peso (%)"), Required(ErrorMessage = Mensajes.Requerido), Range(1, 100, ErrorMessage = Mensajes.Rango)]
        public int? Peso { get; set; }

        [Display(Name = "Meta"), Required(ErrorMessage = Mensajes.Requerido), Range(0.01, 999999999, ErrorMessage = "La meta debe ser mayor que 0.")]
        public decimal? Meta { get; set; }
    }

    public class SeguimientoForm
    {
        public int ObjetivoId { get; set; }

        [Display(Name = "Avance acumulado"), Required(ErrorMessage = Mensajes.Requerido), Range(0, 999999999, ErrorMessage = "El avance no puede ser negativo.")]
        public decimal? Avance { get; set; }

        [Display(Name = "Comentario"), StringLength(500)]
        public string? Comentario { get; set; }
    }

    public class EvaluacionForm
    {
        public int AsignacionId { get; set; }
        public List<EvaluacionObjetivoForm> Objetivos { get; set; } = [];

        [Display(Name = "Comentario final"), Required(ErrorMessage = Mensajes.Requerido), StringLength(1000)]
        public string ComentarioFinal { get; set; } = "";
    }

    public class EvaluacionObjetivoForm
    {
        public int ObjetivoId { get; set; }

        [Display(Name = "Resultado"), Range(0, 999999999, ErrorMessage = "El resultado no puede ser negativo.")]
        public decimal? ResultadoFinal { get; set; }

        [Display(Name = "Cumplimiento (%)"), Required(ErrorMessage = Mensajes.Requerido), Range(0, 100, ErrorMessage = Mensajes.Rango)]
        public int? Cumplimiento { get; set; }
    }
}
