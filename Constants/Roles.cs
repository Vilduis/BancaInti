namespace BancaInti.Constants
{
    public static class Roles
    {
        public const string Admin = "Admin";
        public const string Evaluador = "Evaluador";
        public const string Colaborador = "Colaborador";

        public static readonly string[] Todos = [Admin, Evaluador, Colaborador];
    }
}
