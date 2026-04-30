using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuscaYa.Migrations
{
    /// <inheritdoc />
    public partial class AddPasswordRecoveryToUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "password_reset_token_expires_at",
                table: "Usuarios",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "password_reset_token_hash",
                table: "Usuarios",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_password_reset_token_expires_at",
                table: "Usuarios",
                column: "password_reset_token_expires_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Usuarios_password_reset_token_expires_at",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "password_reset_token_expires_at",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "password_reset_token_hash",
                table: "Usuarios");
        }
    }
}
