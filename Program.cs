using BancaInti.Data;
using BancaInti.Models;
using BancaInti.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<AuditoriaInterceptor>();
builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
    options.UseNpgsql(connectionString)
           .AddInterceptors(sp.GetRequiredService<AuditoriaInterceptor>()));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;

        // Contraseñas = iniciales del trabajador (ver docs/negocio.md). Solo para desarrollo.
        if (builder.Environment.IsDevelopment())
        {
            options.Password.RequiredLength = 2;
            options.Password.RequiredUniqueChars = 1;
            options.Password.RequireDigit = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireNonAlphanumeric = false;
        }
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.ConfigureApplicationCookie(options =>
{
    // Auditoría de inicios de sesión.
    options.Events.OnSignedIn = async ctx =>
    {
        var sp = ctx.HttpContext.RequestServices;
        var log = sp.GetRequiredService<AuditoriaInterceptor>().NuevoLog("Sesión", null);
        log.Usuario = ctx.Principal?.Identity?.Name ?? log.Usuario;
        log.Accion = AccionAuditoria.InicioSesion;
        var db = sp.GetRequiredService<ApplicationDbContext>();
        db.LogsAuditoria.Add(log);
        await db.SaveChangesAsync();
    };
});
builder.Services.AddControllersWithViews(options =>
{
    // Mensajes de model binding en español (los de MVC vienen en inglés).
    var m = options.ModelBindingMessageProvider;
    m.SetValueMustBeANumberAccessor(campo => $"El campo {campo} debe ser un número.");
    m.SetNonPropertyValueMustBeANumberAccessor(() => "El valor debe ser un número.");
    m.SetAttemptedValueIsInvalidAccessor((valor, campo) => $"El valor '{valor}' no es válido para {campo}.");
    m.SetNonPropertyAttemptedValueIsInvalidAccessor(valor => $"El valor '{valor}' no es válido.");
    m.SetUnknownValueIsInvalidAccessor(campo => $"El valor ingresado no es válido para {campo}.");
    m.SetNonPropertyUnknownValueIsInvalidAccessor(() => "El valor ingresado no es válido.");
    m.SetValueIsInvalidAccessor(valor => $"El valor '{valor}' no es válido.");
    m.SetValueMustNotBeNullAccessor(campo => $"El campo {campo} es obligatorio.");
    m.SetMissingBindRequiredValueAccessor(campo => $"Falta el valor del campo {campo}.");
    m.SetMissingKeyOrValueAccessor(() => "Falta un valor obligatorio.");
    m.SetMissingRequestBodyRequiredValueAccessor(() => "La solicitud no tiene contenido.");
});
builder.Services.AddScoped<PeriodoService>();
builder.Services.AddScoped<ReglaService>();
builder.Services.AddScoped<ParticipacionService>();
builder.Services.AddScoped<AsignacionService>();
builder.Services.AddScoped<JustificacionService>();
builder.Services.AddScoped<EvaluacionService>();
builder.Services.AddScoped<ConsultaService>();
builder.Services.AddScoped<ReportePdfService>();
builder.Services.AddSingleton(TimeProvider.System);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/Home/Estado", "?codigo={0}");
app.UseHttpsRedirection();

// Cultura fija es-PE: fechas dd/MM/yyyy y punto decimal (igual que <input type="number">).
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("es-PE"),
    RequestCultureProviders = [],
});
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();

await DbSeeder.SeedAsync(app.Services);

app.Run();
