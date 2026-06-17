using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BuscaYa.Migrations
{
    /// <inheritdoc />
    public partial class AgregarPublicidad : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Publicidades",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Titulo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Subtitulo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AccionUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Orden = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    Activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Publicidades", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Publicidades_Activo",
                table: "Publicidades",
                column: "Activo");

            migrationBuilder.CreateIndex(
                name: "IX_Publicidades_Orden",
                table: "Publicidades",
                column: "Orden");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Publicidades");
        }
    }
}
