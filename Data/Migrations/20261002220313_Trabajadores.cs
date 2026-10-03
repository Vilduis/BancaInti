using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BancaInti.Data.Migrations
{
    /// <inheritdoc />
    public partial class Trabajadores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TiposJustificacion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TiposJustificacion", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Trabajadores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Dni = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    Nombres = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Apellidos = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Cargo = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Area = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    FechaNacimiento = table.Column<DateOnly>(type: "date", nullable: false),
                    FechaIngreso = table.Column<DateOnly>(type: "date", nullable: false),
                    FechaCese = table.Column<DateOnly>(type: "date", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trabajadores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Trabajadores_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "JustificacionesTrabajador",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TrabajadorId = table.Column<int>(type: "integer", nullable: false),
                    TipoJustificacionId = table.Column<int>(type: "integer", nullable: false),
                    FechaInicio = table.Column<DateOnly>(type: "date", nullable: false),
                    FechaFin = table.Column<DateOnly>(type: "date", nullable: false),
                    Observacion = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JustificacionesTrabajador", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JustificacionesTrabajador_TiposJustificacion_TipoJustificac~",
                        column: x => x.TipoJustificacionId,
                        principalTable: "TiposJustificacion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JustificacionesTrabajador_Trabajadores_TrabajadorId",
                        column: x => x.TrabajadorId,
                        principalTable: "Trabajadores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JustificacionesTrabajador_TipoJustificacionId",
                table: "JustificacionesTrabajador",
                column: "TipoJustificacionId");

            migrationBuilder.CreateIndex(
                name: "IX_JustificacionesTrabajador_TrabajadorId",
                table: "JustificacionesTrabajador",
                column: "TrabajadorId");

            migrationBuilder.CreateIndex(
                name: "IX_TiposJustificacion_Nombre",
                table: "TiposJustificacion",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Trabajadores_Codigo",
                table: "Trabajadores",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Trabajadores_Dni",
                table: "Trabajadores",
                column: "Dni",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Trabajadores_UserId",
                table: "Trabajadores",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JustificacionesTrabajador");

            migrationBuilder.DropTable(
                name: "TiposJustificacion");

            migrationBuilder.DropTable(
                name: "Trabajadores");
        }
    }
}
