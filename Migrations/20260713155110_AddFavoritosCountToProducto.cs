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

            // Poblar registros existentes con la cantidad real de favoritos
            migrationBuilder.Sql("UPDATE \"Productos\" SET \"FavoritosCount\" = (SELECT COUNT(*) FROM \"Favoritos\" WHERE \"Favoritos\".\"ProductoId\" = \"Productos\".\"Id\")");
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
