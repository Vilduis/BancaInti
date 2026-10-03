using System.ComponentModel.DataAnnotations;

namespace BancaInti.Models
{
    public class TipoJustificacion
    {
        public int Id { get; set; }

        [Required, StringLength(80)]
        public string Nombre { get; set; } = "";

        public bool Activo { get; set; } = true;
    }
}
