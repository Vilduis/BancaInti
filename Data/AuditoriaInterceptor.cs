using BancaInti.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Text.Json;

namespace BancaInti.Data
{
    // Registra en LogAuditoria cada alta, cambio y baja de las entidades del dominio (BancaInti.Models).
    // Las tablas de Identity no se auditan (contienen hashes y tokens).
    public class AuditoriaInterceptor(IHttpContextAccessor httpContextAccessor) : SaveChangesInterceptor
    {
        private static readonly JsonSerializerOptions Json = new() { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

        // Altas: el Id se conoce recién después de guardar.
        private readonly List<(EntityEntry Entry, LogAuditoria Log)> _pendientes = [];

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context is ApplicationDbContext db)
                Registrar(db);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            if (eventData.Context is ApplicationDbContext db)
                Registrar(db);
            return base.SavingChanges(eventData, result);
        }

        public override async ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context is ApplicationDbContext db && _pendientes.Count > 0)
            {
                CompletarAltas(db);
                await db.SaveChangesAsync(cancellationToken);
            }
            return await base.SavedChangesAsync(eventData, result, cancellationToken);
        }

        public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
        {
            if (eventData.Context is ApplicationDbContext db && _pendientes.Count > 0)
            {
                CompletarAltas(db);
                db.SaveChanges();
            }
            return base.SavedChanges(eventData, result);
        }

        private void Registrar(ApplicationDbContext db)
        {
            db.ChangeTracker.DetectChanges();
            var entradas = db.ChangeTracker.Entries()
                .Where(e => e.Entity is not LogAuditoria
                            && e.Entity.GetType().Namespace == typeof(Trabajador).Namespace
                            && e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
                .ToList();

            foreach (var e in entradas)
            {
                var log = NuevoLog(e.Metadata.ClrType.Name, null);
                switch (e.State)
                {
                    case EntityState.Added:
                        log.Accion = AccionAuditoria.Crear;
                        _pendientes.Add((e, log));
                        continue;
                    case EntityState.Modified:
                        var cambios = e.Properties.Where(p => p.IsModified && !Equals(p.OriginalValue, p.CurrentValue)).ToList();
                        if (cambios.Count == 0) continue;
                        log.Accion = AccionAuditoria.Editar;
                        log.ValoresAnteriores = Serializar(cambios.ToDictionary(p => p.Metadata.Name, p => p.OriginalValue));
                        log.ValoresNuevos = Serializar(cambios.ToDictionary(p => p.Metadata.Name, p => p.CurrentValue));
                        break;
                    case EntityState.Deleted:
                        log.Accion = AccionAuditoria.Eliminar;
                        log.ValoresAnteriores = Serializar(e.Properties.ToDictionary(p => p.Metadata.Name, p => p.OriginalValue));
                        break;
                }
                log.EntidadId = Clave(e);
                db.Set<LogAuditoria>().Add(log);
            }
        }

        private void CompletarAltas(ApplicationDbContext db)
        {
            foreach (var (e, log) in _pendientes)
            {
                log.EntidadId = Clave(e);
                log.ValoresNuevos = Serializar(e.Properties.ToDictionary(p => p.Metadata.Name, p => p.CurrentValue));
                db.Set<LogAuditoria>().Add(log);
            }
            _pendientes.Clear();
        }

        public LogAuditoria NuevoLog(string entidad, string? entidadId)
        {
            var http = httpContextAccessor.HttpContext;
            return new LogAuditoria
            {
                Fecha = DateTime.UtcNow,
                Usuario = http?.User.Identity?.Name ?? "sistema",
                Entidad = entidad,
                EntidadId = entidadId,
                Ip = http?.Connection.RemoteIpAddress?.ToString(),
            };
        }

        private static string Clave(EntityEntry e) =>
            string.Join("-", e.Metadata.FindPrimaryKey()!.Properties.Select(p => e.Property(p.Name).CurrentValue));

        private static string Serializar(Dictionary<string, object?> valores) => JsonSerializer.Serialize(valores, Json);
    }
}
