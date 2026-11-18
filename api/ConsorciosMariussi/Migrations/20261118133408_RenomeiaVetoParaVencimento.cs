using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConsorciosMariussi.Api.Migrations
{
    /// <inheritdoc />
    public partial class RenomeiaVetoParaVencimento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Veto",
                table: "Consorcios",
                newName: "Vencimento");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Vencimento",
                table: "Consorcios",
                newName: "Veto");
        }
    }
}
