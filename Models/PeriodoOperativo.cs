using System.ComponentModel.DataAnnotations;

namespace BancaInti.Models
{
    public class PeriodoOperativo
    {
        public int Id { get; set; }

        public int PeriodoEstrategicoId { get; set; }
        public PeriodoEstrategico? PeriodoEstrategico { get; set; }

        [Display(Name = "Año")]
        public int Anio { get; set; }

        [Display(Name = "Fecha inicio")]
        public DateOnly FechaInicio { get; set; }

        [Display(Name = "Fecha fin")]
        public DateOnly FechaFin { get; set; }

        // Solo un periodo operativo activo a la vez (docs/negocio.md).
        public bool Activo { get; set; }

        // Texto plano que ve el colaborador al ingresar; si es null se usa MensajePorDefecto.
        public string? MensajeColaborador { get; set; }

        public const string MensajePorDefecto =
            "Bienvenido al proceso de Evaluación de Desempeño. Revise sus objetivos y su avance. " +
            "Ante cualquier consulta, comuníquese con su evaluador o con Recursos Humanos.";

        public List<Etapa> Etapas { get; set; } = [];
        public List<PeriodoJustificacion> Justificaciones { get; set; } = [];
        public ReglaEvaluacion? Regla { get; set; }
    }
}
