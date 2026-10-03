using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BancaInti.Models
{
    public class Trabajador
    {
        public int Id { get; set; }

        [Required, StringLength(10)]
        public string Codigo { get; set; } = "";

        [Required, StringLength(8)]
        public string Dni { get; set; } = "";

        [Required, StringLength(80)]
        public string Nombres { get; set; } = "";

        [Required, StringLength(80)]
        public string Apellidos { get; set; } = "";

        [Required, StringLength(80)]
        public string Cargo { get; set; } = "";

        [Required, StringLength(80)]
        public string Area { get; set; } = "";

        [Display(Name = "Fecha de nacimiento")]
        public DateOnly FechaNacimiento { get; set; }

        [Display(Name = "Fecha de ingreso")]
        public DateOnly FechaIngreso { get; set; }

        [Display(Name = "Fecha de cese")]
        public DateOnly? FechaCese { get; set; }

        public string? UserId { get; set; }
        public IdentityUser? User { get; set; }

        public List<JustificacionTrabajador> Justificaciones { get; set; } = [];

        [NotMapped]
        public string NombreCompleto => $"{Nombres} {Apellidos}";
    }
}
