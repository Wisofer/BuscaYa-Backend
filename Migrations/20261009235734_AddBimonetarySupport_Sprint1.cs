using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BuscaYa.Migrations
{
    /// <inheritdoc />
    public partial class AddBimonetarySupport_Sprint1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ExchangeRate",
                table: "Tiendas",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExchangeRateMode",
                table: "Tiendas",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "exchange_rate_logs",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    fecha = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    mode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    rate = table.Column<decimal>(type: "numeric(14,4)", nullable: false),
                    source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exchange_rate_logs", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "exchange_rate_logs");

            migrationBuilder.DropColumn(
                name: "ExchangeRate",
                table: "Tiendas");

            migrationBuilder.DropColumn(
                name: "ExchangeRateMode",
                table: "Tiendas");
        }
    }
}
