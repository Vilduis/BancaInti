using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BancaInti.Models
{
    public class JustificacionTrabajador
    {
        public int Id { get; set; }

        public int TrabajadorId { get; set; }
        public Trabajador? Trabajador { get; set; }

        public int TipoJustificacionId { get; set; }
        public TipoJustificacion? TipoJustificacion { get; set; }

        [Display(Name = "Fecha inicio")]
        public DateOnly FechaInicio { get; set; }

        [Display(Name = "Fecha fin")]
        public DateOnly FechaFin { get; set; }

        [StringLength(250)]
        public string? Observacion { get; set; }

        // Días calendario, ambos extremos incluidos.
        [NotMapped]
        public int Dias => FechaFin.DayNumber - FechaInicio.DayNumber + 1;
    }
}
