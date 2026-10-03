using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BancaInti.Data.Migrations
{
    /// <inheritdoc />
    public partial class MensajeColaborador : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MensajeColaborador",
                table: "PeriodosOperativos",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MensajeColaborador",
                table: "PeriodosOperativos");
        }
    }
}
