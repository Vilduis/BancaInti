using BancaInti.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BancaInti.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext(options)
    {
        public DbSet<Trabajador> Trabajadores => Set<Trabajador>();
        public DbSet<TipoJustificacion> TiposJustificacion => Set<TipoJustificacion>();
        public DbSet<JustificacionTrabajador> JustificacionesTrabajador => Set<JustificacionTrabajador>();
        public DbSet<PeriodoEstrategico> PeriodosEstrategicos => Set<PeriodoEstrategico>();
        public DbSet<PeriodoOperativo> PeriodosOperativos => Set<PeriodoOperativo>();
        public DbSet<Etapa> Etapas => Set<Etapa>();
        public DbSet<PeriodoJustificacion> PeriodosJustificacion => Set<PeriodoJustificacion>();
        public DbSet<ReglaEvaluacion> ReglasEvaluacion => Set<ReglaEvaluacion>();
        public DbSet<RangoCalificacion> RangosCalificacion => Set<RangoCalificacion>();
        public DbSet<AsignacionEvaluador> Asignaciones => Set<AsignacionEvaluador>();
        public DbSet<Objetivo> Objetivos => Set<Objetivo>();
        public DbSet<Seguimiento> Seguimientos => Set<Seguimiento>();
        public DbSet<Comentario> Comentarios => Set<Comentario>();
        public DbSet<LogAuditoria> LogsAuditoria => Set<LogAuditoria>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Trabajador>(e =>
            {
                e.HasIndex(t => t.Codigo).IsUnique();
                e.HasIndex(t => t.Dni).IsUnique();
                e.HasIndex(t => t.UserId).IsUnique();
                e.HasOne(t => t.User).WithMany().HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.SetNull);
            });

            builder.Entity<TipoJustificacion>().HasIndex(t => t.Nombre).IsUnique();

            builder.Entity<JustificacionTrabajador>(e =>
            {
                e.HasOne(j => j.Trabajador).WithMany(t => t.Justificaciones).HasForeignKey(j => j.TrabajadorId);
                e.HasOne(j => j.TipoJustificacion).WithMany().HasForeignKey(j => j.TipoJustificacionId).OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<PeriodoOperativo>(e =>
            {
                e.HasIndex(p => p.Anio).IsUnique();
                e.Property(p => p.MensajeColaborador).HasMaxLength(1000);
                e.HasOne(p => p.PeriodoEstrategico).WithMany(p => p.PeriodosOperativos)
                    .HasForeignKey(p => p.PeriodoEstrategicoId).OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<Etapa>(e =>
            {
                e.HasIndex(x => new { x.PeriodoOperativoId, x.Tipo }).IsUnique();
                e.HasOne(x => x.PeriodoOperativo).WithMany(p => p.Etapas).HasForeignKey(x => x.PeriodoOperativoId);
            });

            builder.Entity<PeriodoJustificacion>(e =>
            {
                e.HasKey(x => new { x.PeriodoOperativoId, x.TipoJustificacionId });
                e.HasOne(x => x.PeriodoOperativo).WithMany(p => p.Justificaciones).HasForeignKey(x => x.PeriodoOperativoId);
                e.HasOne(x => x.TipoJustificacion).WithMany().HasForeignKey(x => x.TipoJustificacionId).OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<ReglaEvaluacion>(e =>
            {
                e.HasIndex(x => x.PeriodoOperativoId).IsUnique();
                e.HasOne(x => x.PeriodoOperativo).WithOne(p => p.Regla).HasForeignKey<ReglaEvaluacion>(x => x.PeriodoOperativoId);
                e.HasMany(x => x.Rangos).WithOne(r => r.ReglaEvaluacion).HasForeignKey(r => r.ReglaEvaluacionId);
            });

            builder.Entity<RangoCalificacion>().Property(r => r.Nombre).HasMaxLength(40);

            builder.Entity<AsignacionEvaluador>(e =>
            {
                e.HasIndex(x => new { x.PeriodoOperativoId, x.EvaluadoId }).IsUnique();
                e.HasOne(x => x.PeriodoOperativo).WithMany().HasForeignKey(x => x.PeriodoOperativoId);
                e.HasOne(x => x.Evaluador).WithMany().HasForeignKey(x => x.EvaluadorId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.Evaluado).WithMany().HasForeignKey(x => x.EvaluadoId).OnDelete(DeleteBehavior.Restrict);
                e.Property(x => x.PuntajeFinal).HasPrecision(5, 2);
                e.Property(x => x.Calificacion).HasMaxLength(40);
                e.Property(x => x.ComentarioFinal).HasMaxLength(1000);
            });

            builder.Entity<Objetivo>(e =>
            {
                e.HasOne(x => x.Asignacion).WithMany(a => a.Objetivos).HasForeignKey(x => x.AsignacionId);
                e.Property(x => x.Descripcion).HasMaxLength(500);
                e.Property(x => x.Indicador).HasMaxLength(200);
                e.Property(x => x.UnidadMedida).HasMaxLength(30);
                e.Property(x => x.Meta).HasPrecision(18, 2);
                e.Property(x => x.ResultadoFinal).HasPrecision(18, 2);
            });

            builder.Entity<Seguimiento>(e =>
            {
                e.HasOne(x => x.Objetivo).WithMany(o => o.Seguimientos).HasForeignKey(x => x.ObjetivoId);
                e.Property(x => x.Avance).HasPrecision(18, 2);
                e.Property(x => x.Comentario).HasMaxLength(500);
            });

            builder.Entity<Comentario>(e =>
            {
                e.HasOne(x => x.Asignacion).WithMany(a => a.Comentarios).HasForeignKey(x => x.AsignacionId);
                e.HasOne(x => x.Objetivo).WithMany().HasForeignKey(x => x.ObjetivoId).OnDelete(DeleteBehavior.SetNull);
                e.HasOne(x => x.Autor).WithMany().HasForeignKey(x => x.AutorId).OnDelete(DeleteBehavior.Restrict);
                e.Property(x => x.Texto).HasMaxLength(1000);
            });

            builder.Entity<LogAuditoria>(e =>
            {
                e.HasIndex(x => x.Fecha);
                e.Property(x => x.Usuario).HasMaxLength(256);
                e.Property(x => x.Accion).HasMaxLength(30);
                e.Property(x => x.Entidad).HasMaxLength(60);
                e.Property(x => x.EntidadId).HasMaxLength(60);
                e.Property(x => x.Ip).HasMaxLength(45);
                e.Property(x => x.ValoresAnteriores).HasColumnType("jsonb");
                e.Property(x => x.ValoresNuevos).HasColumnType("jsonb");
            });
        }
    }
}
