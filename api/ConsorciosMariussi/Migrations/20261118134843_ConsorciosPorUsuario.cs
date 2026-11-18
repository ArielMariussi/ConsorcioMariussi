using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConsorciosMariussi.Api.Migrations
{
    /// <inheritdoc />
    public partial class ConsorciosPorUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Os consórcios existentes não têm dono: são apagados (decisão do projeto).
            migrationBuilder.Sql("DELETE FROM \"Consorcios\";");

            migrationBuilder.DropIndex(
                name: "IX_Consorcios_Administradora_Grupo_Cota",
                table: "Consorcios");

            migrationBuilder.AddColumn<string>(
                name: "UsuarioId",
                table: "Consorcios",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Consorcios_UsuarioId_Administradora_Grupo_Cota",
                table: "Consorcios",
                columns: new[] { "UsuarioId", "Administradora", "Grupo", "Cota" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Consorcios_AspNetUsers_UsuarioId",
                table: "Consorcios",
                column: "UsuarioId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Consorcios_AspNetUsers_UsuarioId",
                table: "Consorcios");

            migrationBuilder.DropIndex(
                name: "IX_Consorcios_UsuarioId_Administradora_Grupo_Cota",
                table: "Consorcios");

            migrationBuilder.DropColumn(
                name: "UsuarioId",
                table: "Consorcios");

            migrationBuilder.CreateIndex(
                name: "IX_Consorcios_Administradora_Grupo_Cota",
                table: "Consorcios",
                columns: new[] { "Administradora", "Grupo", "Cota" },
                unique: true);
        }
    }
}
