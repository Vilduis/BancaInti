using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BancaInti.Data.Migrations
{
    /// <inheritdoc />
    public partial class Asignaciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Asignaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PeriodoOperativoId = table.Column<int>(type: "integer", nullable: false),
                    EvaluadorId = table.Column<int>(type: "integer", nullable: false),
                    EvaluadoId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Asignaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Asignaciones_PeriodosOperativos_PeriodoOperativoId",
                        column: x => x.PeriodoOperativoId,
                        principalTable: "PeriodosOperativos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Asignaciones_Trabajadores_EvaluadoId",
                        column: x => x.EvaluadoId,
                        principalTable: "Trabajadores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Asignaciones_Trabajadores_EvaluadorId",
                        column: x => x.EvaluadorId,
                        principalTable: "Trabajadores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Asignaciones_EvaluadoId",
                table: "Asignaciones",
                column: "EvaluadoId");

            migrationBuilder.CreateIndex(
                name: "IX_Asignaciones_EvaluadorId",
                table: "Asignaciones",
                column: "EvaluadorId");

            migrationBuilder.CreateIndex(
                name: "IX_Asignaciones_PeriodoOperativoId_EvaluadoId",
                table: "Asignaciones",
                columns: new[] { "PeriodoOperativoId", "EvaluadoId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Asignaciones");
        }
    }
}
