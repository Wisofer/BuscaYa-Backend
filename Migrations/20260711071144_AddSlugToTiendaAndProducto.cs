using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuscaYa.Migrations
{
    /// <inheritdoc />
    public partial class AddSlugToTiendaAndProducto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Tiendas",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Productos",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            // Backfill: generar slugs para tiendas existentes a partir del TokenPublico
            // Usamos el token como slug temporal para garantizar unicidad; la app
            // generará slugs legibles para nuevos registros.
            migrationBuilder.Sql(@"
                UPDATE ""Tiendas""
                SET ""Slug"" = LOWER(
                    REGEXP_REPLACE(
                        REGEXP_REPLACE(
                            TRANSLATE(
                                LOWER(""Nombre""),
                                'áàäâãéèëêíìïîóòöôõúùüûñç',
                                'aaaaaeeeeiiiioooooouuuunc'
                            ),
                            '[^a-z0-9\s\-]', '', 'g'
                        ),
                        '\s+', '-', 'g'
                    )
                ) || '-' || ""Id""
                WHERE ""Slug"" = '';
            ");

            // Backfill: generar slugs para productos existentes a partir del nombre + id
            migrationBuilder.Sql(@"
                UPDATE ""Productos""
                SET ""Slug"" = LOWER(
                    REGEXP_REPLACE(
                        REGEXP_REPLACE(
                            TRANSLATE(
                                LOWER(""Nombre""),
                                'áàäâãéèëêíìïîóòöôõúùüûñç',
                                'aaaaaeeeeiiiioooooouuuunc'
                            ),
                            '[^a-z0-9\s\-]', '', 'g'
                        ),
                        '\s+', '-', 'g'
                    )
                ) || '-' || ""Id""
                WHERE ""Slug"" = '';
            ");

            migrationBuilder.CreateIndex(
                name: "IX_Tiendas_Slug",
                table: "Tiendas",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Productos_TiendaId_Slug",
                table: "Productos",
                columns: new[] { "TiendaId", "Slug" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tiendas_Slug",
                table: "Tiendas");

            migrationBuilder.DropIndex(
                name: "IX_Productos_TiendaId_Slug",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Tiendas");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Productos");
        }
    }
}
