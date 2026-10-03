using BancaInti.Constants;
using BancaInti.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;

namespace BancaInti.Data
{
    public static class DbSeeder
    {
        public const string AdminEmail = "admin@bancainti.pe";
        private const string AdminPassword = "AD";
        private const string Dominio = "@bancainti.pe";

        private record TrabajadorSeed(string Nombres, string Apellidos, string Cargo, string Area,
            DateOnly Nacimiento, DateOnly Ingreso, string[] Roles, DateOnly? Cese = null);

        // Datos ficticios (docs/negocio.md#datos-de-prueba). Incluye casos para probar las consultas:
        // mayores de 60, ingresos recientes (< 90 días) y un cesado.
        private static readonly TrabajadorSeed[] Trabajadores =
        [
            new("Rosa Elena", "Quispe Huamán", "Jefa de RRHH", "Recursos Humanos", new(1980, 4, 12), new(2012, 3, 1), [Roles.Admin, Roles.Colaborador]),
            new("Carlos Alberto", "Mendoza Ríos", "Gerente de Créditos", "Créditos", new(1964, 9, 3), new(2005, 6, 15), [Roles.Evaluador, Roles.Colaborador]),
            new("María Fernanda", "Torres Salazar", "Jefa de Operaciones", "Operaciones", new(1978, 1, 25), new(2010, 2, 1), [Roles.Evaluador, Roles.Colaborador]),
            new("Jorge Luis", "Paredes Vega", "Jefe de Tecnología", "Tecnología", new(1983, 7, 19), new(2014, 8, 4), [Roles.Evaluador, Roles.Colaborador]),
            new("Ana Lucía", "Flores Castillo", "Jefa Comercial", "Comercial", new(1985, 11, 8), new(2015, 1, 12), [Roles.Evaluador, Roles.Colaborador]),
            new("Luis Miguel", "Rojas Campos", "Analista de Créditos", "Créditos", new(1990, 5, 30), new(2018, 4, 2), [Roles.Colaborador]),
            new("Patricia", "Chávez Núñez", "Analista de Créditos", "Créditos", new(1992, 2, 14), new(2019, 9, 16), [Roles.Colaborador]),
            new("Víctor Hugo", "Ramírez Soto", "Asesor de Créditos", "Créditos", new(1963, 12, 1), new(1998, 5, 4), [Roles.Colaborador]),
            new("Carmen", "Gutiérrez León", "Asistente de Créditos", "Créditos", new(1995, 8, 22), new(2021, 3, 1), [Roles.Colaborador]),
            new("Pedro", "Sánchez Díaz", "Cajero", "Operaciones", new(1993, 10, 5), new(2017, 7, 10), [Roles.Colaborador]),
            new("Lucía", "Vargas Morales", "Cajera", "Operaciones", new(1996, 3, 17), new(2020, 1, 6), [Roles.Colaborador]),
            new("Manuel", "Castro Herrera", "Supervisor de Caja", "Operaciones", new(1961, 6, 11), new(1995, 10, 2), [Roles.Colaborador]),
            new("Diana", "Ruiz Espinoza", "Cajera", "Operaciones", new(2000, 9, 9), new(2026, 8, 1), [Roles.Colaborador]),
            new("Andrés", "Silva Medina", "Desarrollador", "Tecnología", new(1991, 4, 3), new(2016, 11, 14), [Roles.Colaborador]),
            new("Sofía", "Ramos Cárdenas", "Analista de Sistemas", "Tecnología", new(1994, 12, 27), new(2019, 2, 18), [Roles.Colaborador]),
            new("Diego", "Herrera Lozano", "Soporte Técnico", "Tecnología", new(1999, 1, 15), new(2026, 7, 15), [Roles.Colaborador]),
            new("Gabriela", "Ortiz Peña", "Ejecutiva de Ventas", "Comercial", new(1989, 7, 7), new(2016, 5, 23), [Roles.Colaborador]),
            new("Raúl", "Delgado Aguirre", "Ejecutivo de Ventas", "Comercial", new(1965, 2, 28), new(2001, 9, 3), [Roles.Colaborador]),
            new("Elena", "Navarro Ibáñez", "Ejecutiva de Ventas", "Comercial", new(1997, 6, 19), new(2022, 4, 4), [Roles.Colaborador], Cese: new(2026, 9, 15)),
            new("Fernando", "Romero Zapata", "Promotor de Negocios", "Comercial", new(1998, 11, 30), new(2023, 2, 13), [Roles.Colaborador]),
        ];

        private static readonly string[] TiposJustificacion =
            ["Descanso médico", "Licencia por maternidad", "Licencia por paternidad", "Licencia sin goce de haber", "Vacaciones"];

        public static async Task SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var sp = scope.ServiceProvider;
            var db = sp.GetRequiredService<ApplicationDbContext>();
            var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = sp.GetRequiredService<UserManager<IdentityUser>>();

            await db.Database.MigrateAsync();

            foreach (var rol in Roles.Todos)
            {
                if (!await roleManager.RoleExistsAsync(rol))
                    await roleManager.CreateAsync(new IdentityRole(rol));
            }

            await CrearUsuarioAsync(userManager, AdminEmail, AdminPassword, [Roles.Admin]);

            if (!await db.Trabajadores.AnyAsync())
                await SeedTrabajadoresAsync(db, userManager);

            if (!await db.TiposJustificacion.AnyAsync())
            {
                db.TiposJustificacion.AddRange(TiposJustificacion.Select(n => new TipoJustificacion { Nombre = n }));
                await db.SaveChangesAsync();
                await SeedJustificacionesAsync(db);
            }

            if (!await db.PeriodosEstrategicos.AnyAsync())
                await SeedPeriodoAsync(db);
        }

        // Evaluador → evaluados del periodo 2026: cada jefe evalúa a su área y el gerente (T002) a los otros jefes.
        private static readonly Dictionary<string, string[]> Equipos2026 = new()
        {
            ["T002"] = ["T003", "T004", "T005", "T006", "T007", "T008", "T009"],
            ["T003"] = ["T010", "T011", "T012", "T013"],
            ["T004"] = ["T014", "T015", "T016"],
            ["T005"] = ["T017", "T018", "T019", "T020"],
        };

        private static async Task SeedPeriodoAsync(ApplicationDbContext db)
        {
            var estrategico = new PeriodoEstrategico { Nombre = "Plan Estratégico 2025-2028", FechaInicio = new(2025, 1, 1), FechaFin = new(2028, 12, 31) };
            var periodo = new PeriodoOperativo
            {
                PeriodoEstrategico = estrategico,
                Anio = 2026,
                FechaInicio = new(2026, 1, 1),
                FechaFin = new(2026, 12, 31),
                Activo = true,
                Etapas =
                [
                    new() { Tipo = TipoEtapa.Registro, FechaInicio = new(2026, 1, 1), FechaFin = new(2026, 3, 31) },
                    new() { Tipo = TipoEtapa.Seguimiento, FechaInicio = new(2026, 4, 1), FechaFin = new(2026, 10, 31) },
                    new() { Tipo = TipoEtapa.Evaluacion, FechaInicio = new(2026, 11, 1), FechaFin = new(2026, 12, 31) },
                ],
                Justificaciones = await db.TiposJustificacion.Select(t => new PeriodoJustificacion { TipoJustificacionId = t.Id }).ToListAsync(),
                Regla = new Services.ReglaService().ReglaPorDefecto(),
            };
            db.PeriodosOperativos.Add(periodo);

            var trab = await db.Trabajadores.ToDictionaryAsync(t => t.Codigo, t => t.Id);
            foreach (var (evaluador, evaluados) in Equipos2026)
            {
                foreach (var evaluado in evaluados)
                    db.Asignaciones.Add(new AsignacionEvaluador { PeriodoOperativo = periodo, EvaluadorId = trab[evaluador], EvaluadoId = trab[evaluado] });
            }
            await db.SaveChangesAsync();
        }

        private static async Task SeedTrabajadoresAsync(ApplicationDbContext db, UserManager<IdentityUser> userManager)
        {
            for (int i = 0; i < Trabajadores.Length; i++)
            {
                var t = Trabajadores[i];
                var email = $"{Normalizar(t.Nombres)}.{Normalizar(t.Apellidos)}{Dominio}";
                var user = await CrearUsuarioAsync(userManager, email, Iniciales(t.Nombres, t.Apellidos), t.Roles);

                db.Trabajadores.Add(new Trabajador
                {
                    Codigo = $"T{i + 1:D3}",
                    Dni = (40000001 + i * 137).ToString(),
                    Nombres = t.Nombres,
                    Apellidos = t.Apellidos,
                    Cargo = t.Cargo,
                    Area = t.Area,
                    FechaNacimiento = t.Nacimiento,
                    FechaIngreso = t.Ingreso,
                    FechaCese = t.Cese,
                    UserId = user.Id,
                });
            }
            await db.SaveChangesAsync();
        }

        private static async Task SeedJustificacionesAsync(ApplicationDbContext db)
        {
            var tipos = await db.TiposJustificacion.ToDictionaryAsync(t => t.Nombre, t => t.Id);
            var trab = await db.Trabajadores.ToDictionaryAsync(t => t.Codigo, t => t.Id);

            JustificacionTrabajador J(string codigo, string tipo, DateOnly inicio, DateOnly fin) =>
                new() { TrabajadorId = trab[codigo], TipoJustificacionId = tipos[tipo], FechaInicio = inicio, FechaFin = fin };

            db.JustificacionesTrabajador.AddRange(
                J("T007", "Licencia por maternidad", new(2026, 3, 2), new(2026, 5, 30)),
                J("T011", "Descanso médico", new(2026, 4, 6), new(2026, 5, 5)),
                J("T010", "Descanso médico", new(2026, 2, 9), new(2026, 2, 18)),
                J("T014", "Licencia sin goce de haber", new(2026, 1, 5), new(2026, 9, 30)),
                J("T017", "Vacaciones", new(2026, 7, 1), new(2026, 7, 30)),
                J("T006", "Licencia por paternidad", new(2026, 6, 8), new(2026, 6, 17)),
                // Ingresó el 01/08/2026 (153 días en 2026) − 72 justificados = 81 días: no participa.
                J("T013", "Descanso médico", new(2026, 8, 10), new(2026, 10, 20)));
            await db.SaveChangesAsync();
        }

        private static async Task<IdentityUser> CrearUsuarioAsync(UserManager<IdentityUser> userManager, string email, string password, string[] roles)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user is not null)
                return user;

            user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
                throw new InvalidOperationException($"{email}: " + string.Join("; ", result.Errors.Select(e => e.Description)));
            await userManager.AddToRolesAsync(user, roles);
            return user;
        }

        // "Rosa Elena" + "Quispe Huamán" -> "RQ"
        private static string Iniciales(string nombres, string apellidos) =>
            $"{Normalizar(nombres)[0]}{Normalizar(apellidos)[0]}".ToUpperInvariant();

        // Primera palabra, minúsculas, sin tildes: "Ana Lucía" -> "ana", "Núñez" -> "nunez"
        private static string Normalizar(string texto)
        {
            var palabra = texto.Split(' ')[0].Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var c in palabra)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(char.ToLowerInvariant(c));
            }
            return sb.ToString();
        }
    }
}
