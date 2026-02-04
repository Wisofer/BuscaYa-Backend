using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuscaYa.Migrations
{
    /// <inheritdoc />
    public partial class AgregarGoogleIdUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GoogleId",
                table: "Usuarios",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_GoogleId",
                table: "Usuarios",
                column: "GoogleId",
                unique: true,
                filter: "\"GoogleId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Usuarios_GoogleId",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "GoogleId",
                table: "Usuarios");
        }
    }
}
