using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BancaInti.Data.Migrations
{
    /// <inheritdoc />
    public partial class PeriodosYReglas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PeriodosEstrategicos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    FechaInicio = table.Column<DateOnly>(type: "date", nullable: false),
                    FechaFin = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PeriodosEstrategicos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PeriodosOperativos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PeriodoEstrategicoId = table.Column<int>(type: "integer", nullable: false),
                    Anio = table.Column<int>(type: "integer", nullable: false),
                    FechaInicio = table.Column<DateOnly>(type: "date", nullable: false),
                    FechaFin = table.Column<DateOnly>(type: "date", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PeriodosOperativos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PeriodosOperativos_PeriodosEstrategicos_PeriodoEstrategicoId",
                        column: x => x.PeriodoEstrategicoId,
                        principalTable: "PeriodosEstrategicos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Etapas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PeriodoOperativoId = table.Column<int>(type: "integer", nullable: false),
                    Tipo = table.Column<int>(type: "integer", nullable: false),
                    FechaInicio = table.Column<DateOnly>(type: "date", nullable: false),
                    FechaFin = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Etapas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Etapas_PeriodosOperativos_PeriodoOperativoId",
                        column: x => x.PeriodoOperativoId,
                        principalTable: "PeriodosOperativos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PeriodosJustificacion",
                columns: table => new
                {
                    PeriodoOperativoId = table.Column<int>(type: "integer", nullable: false),
                    TipoJustificacionId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PeriodosJustificacion", x => new { x.PeriodoOperativoId, x.TipoJustificacionId });
                    table.ForeignKey(
                        name: "FK_PeriodosJustificacion_PeriodosOperativos_PeriodoOperativoId",
                        column: x => x.PeriodoOperativoId,
                        principalTable: "PeriodosOperativos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PeriodosJustificacion_TiposJustificacion_TipoJustificacionId",
                        column: x => x.TipoJustificacionId,
                        principalTable: "TiposJustificacion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReglasEvaluacion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PeriodoOperativoId = table.Column<int>(type: "integer", nullable: false),
                    MinObjetivos = table.Column<int>(type: "integer", nullable: false),
                    MaxObjetivos = table.Column<int>(type: "integer", nullable: false),
                    PesoMinimo = table.Column<int>(type: "integer", nullable: false),
                    PesoMaximo = table.Column<int>(type: "integer", nullable: false),
                    DiasMinimos = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReglasEvaluacion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReglasEvaluacion_PeriodosOperativos_PeriodoOperativoId",
                        column: x => x.PeriodoOperativoId,
                        principalTable: "PeriodosOperativos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RangosCalificacion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ReglaEvaluacionId = table.Column<int>(type: "integer", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    PuntajeMinimo = table.Column<int>(type: "integer", nullable: false),
                    PuntajeMaximo = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RangosCalificacion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RangosCalificacion_ReglasEvaluacion_ReglaEvaluacionId",
                        column: x => x.ReglaEvaluacionId,
                        principalTable: "ReglasEvaluacion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Etapas_PeriodoOperativoId_Tipo",
                table: "Etapas",
                columns: new[] { "PeriodoOperativoId", "Tipo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PeriodosJustificacion_TipoJustificacionId",
                table: "PeriodosJustificacion",
                column: "TipoJustificacionId");

            migrationBuilder.CreateIndex(
                name: "IX_PeriodosOperativos_Anio",
                table: "PeriodosOperativos",
                column: "Anio",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PeriodosOperativos_PeriodoEstrategicoId",
                table: "PeriodosOperativos",
                column: "PeriodoEstrategicoId");

            migrationBuilder.CreateIndex(
                name: "IX_RangosCalificacion_ReglaEvaluacionId",
                table: "RangosCalificacion",
                column: "ReglaEvaluacionId");

            migrationBuilder.CreateIndex(
                name: "IX_ReglasEvaluacion_PeriodoOperativoId",
                table: "ReglasEvaluacion",
                column: "PeriodoOperativoId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Etapas");

            migrationBuilder.DropTable(
                name: "PeriodosJustificacion");

            migrationBuilder.DropTable(
                name: "RangosCalificacion");

            migrationBuilder.DropTable(
                name: "ReglasEvaluacion");

            migrationBuilder.DropTable(
                name: "PeriodosOperativos");

            migrationBuilder.DropTable(
                name: "PeriodosEstrategicos");
        }
    }
}
