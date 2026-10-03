using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BancaInti.Data.Migrations
{
    /// <inheritdoc />
    public partial class Auditoria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LogsAuditoria",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Usuario = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Accion = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Entidad = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    EntidadId = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    ValoresAnteriores = table.Column<string>(type: "jsonb", nullable: true),
                    ValoresNuevos = table.Column<string>(type: "jsonb", nullable: true),
                    Ip = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogsAuditoria", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LogsAuditoria_Fecha",
                table: "LogsAuditoria",
                column: "Fecha");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LogsAuditoria");
        }
    }
}
