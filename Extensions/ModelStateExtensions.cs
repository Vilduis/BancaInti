using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace BancaInti
{
    public static class ModelStateExtensions
    {
        // Errores de negocio de los servicios; se muestran en el resumen de validación.
        public static void AgregarErrores(this ModelStateDictionary modelState, IEnumerable<string> errores)
        {
            foreach (var error in errores)
                modelState.AddModelError(string.Empty, error);
        }
    }
}
