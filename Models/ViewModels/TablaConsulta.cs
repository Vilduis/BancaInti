namespace BancaInti.Models.ViewModels
{
    // Resultado de una consulta: se muestra en la vista Tabla (DataTables + Excel en el navegador).
    public class TablaConsulta
    {
        public string Titulo { get; set; } = "";
        public string? Subtitulo { get; set; }

        // Nombre del archivo Excel (tablas.js le agrega la fecha).
        public string Archivo { get; set; } = "";

        // Partial con los filtros de la consulta (Areas/Admin/Views/Consultas/_Filtros*.cshtml).
        public string? Filtros { get; set; }

        public List<string> Columnas { get; set; } = [];

        // Columnas con lista desplegable de filtro en pantalla.
        public List<string> ColumnasFiltro { get; set; } = [];
        public List<object?[]> Filas { get; set; } = [];
    }
}
