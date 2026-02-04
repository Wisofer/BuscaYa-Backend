using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuscaYa.Migrations
{
    /// <inheritdoc />
    public partial class AjusteSincronizacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // No-op: GoogleId ya aplicado en AgregarGoogleIdUsuario; EstaAbiertaManual ya aplicado en AgregarEstaAbiertaManualTienda.
            // Esta migración solo sincroniza el historial para entornos donde se aplicaron cambios fuera de orden.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op
        }
    }
}
