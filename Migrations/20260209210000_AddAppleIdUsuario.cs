using BuscaYa.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuscaYa.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260209210000_AddAppleIdUsuario")]
    public partial class AddAppleIdUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AppleId",
                table: "Usuarios",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_AppleId",
                table: "Usuarios",
                column: "AppleId",
                unique: true,
                filter: "\"AppleId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_Usuarios_AppleId", table: "Usuarios");
            migrationBuilder.DropColumn(name: "AppleId", table: "Usuarios");
        }
    }
}
