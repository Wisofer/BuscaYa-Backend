using BuscaYa.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuscaYa.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260209200000_AddNotificationTriggersAndProductStock")]
    public partial class AddNotificationTriggersAndProductStock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Stock",
                table: "Productos",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NotificationType",
                table: "NotificationLogs",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EntityId",
                table: "NotificationLogs",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationLogs_UsuarioId_NotificationType_EntityId",
                table: "NotificationLogs",
                columns: new[] { "UsuarioId", "NotificationType", "EntityId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_NotificationLogs_UsuarioId_NotificationType_EntityId",
                table: "NotificationLogs");
            migrationBuilder.DropColumn(name: "Stock", table: "Productos");
            migrationBuilder.DropColumn(name: "NotificationType", table: "NotificationLogs");
            migrationBuilder.DropColumn(name: "EntityId", table: "NotificationLogs");
        }
    }
}
