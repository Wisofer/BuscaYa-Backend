using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuscaYa.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiCountryLatamSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CountryCode",
                table: "Usuarios",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CountryCode",
                table: "Tiendas",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "NI");

            migrationBuilder.AddColumn<string>(
                name: "CurrencyCode",
                table: "Tiendas",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "NIO");

            migrationBuilder.AddColumn<string>(
                name: "CurrencySymbol",
                table: "Tiendas",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "C$");

            migrationBuilder.AddColumn<string>(
                name: "Pais",
                table: "Tiendas",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "Nicaragua");

            migrationBuilder.AddColumn<string>(
                name: "CountryCode",
                table: "Publicidades",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CountryCode",
                table: "HistorialBusquedas",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_CountryCode",
                table: "Usuarios",
                column: "CountryCode");

            migrationBuilder.CreateIndex(
                name: "IX_Tiendas_CountryCode",
                table: "Tiendas",
                column: "CountryCode");

            migrationBuilder.CreateIndex(
                name: "IX_Publicidades_CountryCode",
                table: "Publicidades",
                column: "CountryCode");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialBusquedas_CountryCode",
                table: "HistorialBusquedas",
                column: "CountryCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Usuarios_CountryCode",
                table: "Usuarios");

            migrationBuilder.DropIndex(
                name: "IX_Tiendas_CountryCode",
                table: "Tiendas");

            migrationBuilder.DropIndex(
                name: "IX_Publicidades_CountryCode",
                table: "Publicidades");

            migrationBuilder.DropIndex(
                name: "IX_HistorialBusquedas_CountryCode",
                table: "HistorialBusquedas");

            migrationBuilder.DropColumn(
                name: "CountryCode",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "CountryCode",
                table: "Tiendas");

            migrationBuilder.DropColumn(
                name: "CurrencyCode",
                table: "Tiendas");

            migrationBuilder.DropColumn(
                name: "CurrencySymbol",
                table: "Tiendas");

            migrationBuilder.DropColumn(
                name: "Pais",
                table: "Tiendas");

            migrationBuilder.DropColumn(
                name: "CountryCode",
                table: "Publicidades");

            migrationBuilder.DropColumn(
                name: "CountryCode",
                table: "HistorialBusquedas");
        }
    }
}
