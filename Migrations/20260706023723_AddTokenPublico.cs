using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuscaYa.Migrations
{
    /// <inheritdoc />
    public partial class AddTokenPublico : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TokenPublico",
                table: "Tiendas",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TokenPublico",
                table: "Productos",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            // Rellenar valores aleatorios para registros existentes para evitar violación de UNIQUE
            migrationBuilder.Sql("UPDATE \"Tiendas\" SET \"TokenPublico\" = substring(md5(random()::text) from 1 for 10) WHERE \"TokenPublico\" = '' OR \"TokenPublico\" IS NULL;");
            migrationBuilder.Sql("UPDATE \"Productos\" SET \"TokenPublico\" = substring(md5(random()::text) from 1 for 10) WHERE \"TokenPublico\" = '' OR \"TokenPublico\" IS NULL;");

            migrationBuilder.CreateIndex(
                name: "IX_Tiendas_TokenPublico",
                table: "Tiendas",
                column: "TokenPublico",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Productos_TokenPublico",
                table: "Productos",
                column: "TokenPublico",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tiendas_TokenPublico",
                table: "Tiendas");

            migrationBuilder.DropIndex(
                name: "IX_Productos_TokenPublico",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "TokenPublico",
                table: "Tiendas");

            migrationBuilder.DropColumn(
                name: "TokenPublico",
                table: "Productos");
        }
    }
}
