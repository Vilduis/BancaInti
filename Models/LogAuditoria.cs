namespace BancaInti.Models
{
    public class LogAuditoria
    {
        public long Id { get; set; }
        public DateTime Fecha { get; set; }
        public string Usuario { get; set; } = "";
        public string Accion { get; set; } = "";
        public string Entidad { get; set; } = "";
        public string? EntidadId { get; set; }

        // JSON con los campos afectados.
        public string? ValoresAnteriores { get; set; }
        public string? ValoresNuevos { get; set; }

        public string? Ip { get; set; }
    }

    public static class AccionAuditoria
    {
        public const string Crear = "Crear";
        public const string Editar = "Editar";
        public const string Eliminar = "Eliminar";
        public const string InicioSesion = "Inicio de sesión";
    }
}
