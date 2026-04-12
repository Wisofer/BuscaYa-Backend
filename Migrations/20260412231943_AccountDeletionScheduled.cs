using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuscaYa.Migrations
{
    /// <inheritdoc />
    public partial class AccountDeletionScheduled : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "account_deletion_requested_at",
                table: "Usuarios",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "account_deletion_scheduled_at",
                table: "Usuarios",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_account_deletion_scheduled_at",
                table: "Usuarios",
                column: "account_deletion_scheduled_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Usuarios_account_deletion_scheduled_at",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "account_deletion_requested_at",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "account_deletion_scheduled_at",
                table: "Usuarios");
        }
    }
}
