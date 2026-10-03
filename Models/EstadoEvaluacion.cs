namespace BancaInti.Models
{
    public enum EstadoEvaluacion
    {
        Borrador = 0,      // El evaluador está registrando objetivos.
        Confirmado = 1,    // Objetivos cerrados y visibles para el colaborador.
        Evaluado = 2,      // Evaluación final cerrada.
    }
}
