using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuscaYa.Migrations
{
    /// <inheritdoc />
    public partial class AddFavoritosCountToProducto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FavoritosCount",
                table: "Productos",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FavoritosCount",
                table: "Productos");
        }
    }
}
