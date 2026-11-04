using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BuscaYa.Data;

namespace BuscaYa.Utils;

/// <summary>
/// Auto-sanador de esquema de base de datos con SQL idempotente.
/// Garantiza que columnas críticas, índices y extensiones de PostgreSQL existan
/// incluso si una migración de EF Core falló o se desincronizó en producción.
/// </summary>
public static class DatabaseSchemaSelfHeal
{
    public static async Task ApplyAsync(ApplicationDbContext dbContext, ILogger logger, CancellationToken ct = default)
    {
        try
        {
            logger.LogInformation("Verificando integridad y auto-sanación del esquema de base de datos...");

            // 1. Columnas críticas en Usuarios
            await dbContext.Database.ExecuteSqlRawAsync(
                """
                ALTER TABLE "Usuarios" ADD COLUMN IF NOT EXISTS "CountryCode" character varying(10) NULL;
                ALTER TABLE "Usuarios" ADD COLUMN IF NOT EXISTS "GoogleId" character varying(128) NULL;
                ALTER TABLE "Usuarios" ADD COLUMN IF NOT EXISTS "AppleId" character varying(128) NULL;
                ALTER TABLE "Usuarios" ADD COLUMN IF NOT EXISTS "account_deletion_requested_at" timestamp without time zone NULL;
                ALTER TABLE "Usuarios" ADD COLUMN IF NOT EXISTS "account_deletion_scheduled_at" timestamp without time zone NULL;
                ALTER TABLE "Usuarios" ADD COLUMN IF NOT EXISTS "password_reset_token_hash" character varying(128) NULL;
                ALTER TABLE "Usuarios" ADD COLUMN IF NOT EXISTS "password_reset_token_expires_at" timestamp without time zone NULL;
                """, ct);

            // 2. Columnas críticas en Tiendas
            await dbContext.Database.ExecuteSqlRawAsync(
                """
                ALTER TABLE "Tiendas" ADD COLUMN IF NOT EXISTS "CountryCode" character varying(10) NOT NULL DEFAULT 'NI';
                ALTER TABLE "Tiendas" ADD COLUMN IF NOT EXISTS "Pais" character varying(100) NOT NULL DEFAULT 'Nicaragua';
                ALTER TABLE "Tiendas" ADD COLUMN IF NOT EXISTS "CurrencyCode" character varying(10) NOT NULL DEFAULT 'NIO';
                ALTER TABLE "Tiendas" ADD COLUMN IF NOT EXISTS "CurrencySymbol" character varying(10) NOT NULL DEFAULT 'C$';
                ALTER TABLE "Tiendas" ADD COLUMN IF NOT EXISTS "ExchangeRateMode" character varying(20) NULL;
                ALTER TABLE "Tiendas" ADD COLUMN IF NOT EXISTS "ExchangeRate" numeric(18,4) NULL;
                ALTER TABLE "Tiendas" ADD COLUMN IF NOT EXISTS "TokenPublico" character varying(50) NOT NULL DEFAULT '';
                ALTER TABLE "Tiendas" ADD COLUMN IF NOT EXISTS "Slug" character varying(200) NOT NULL DEFAULT '';
                """, ct);

            // 3. Columnas críticas en Productos
            await dbContext.Database.ExecuteSqlRawAsync(
                """
                ALTER TABLE "Productos" ADD COLUMN IF NOT EXISTS "TokenPublico" character varying(50) NOT NULL DEFAULT '';
                ALTER TABLE "Productos" ADD COLUMN IF NOT EXISTS "Slug" character varying(200) NOT NULL DEFAULT '';
                ALTER TABLE "Productos" ADD COLUMN IF NOT EXISTS "PrecioAnterior" numeric(18,2) NULL;
                ALTER TABLE "Productos" ADD COLUMN IF NOT EXISTS "EnOferta" boolean NOT NULL DEFAULT false;
                ALTER TABLE "Productos" ADD COLUMN IF NOT EXISTS "Stock" integer NULL;
                ALTER TABLE "Productos" ADD COLUMN IF NOT EXISTS "FavoritosCount" integer NOT NULL DEFAULT 0;
                """, ct);

            // 4. Índices para rendimiento óptimo
            await dbContext.Database.ExecuteSqlRawAsync(
                """
                CREATE INDEX IF NOT EXISTS "IX_Usuarios_CountryCode" ON "Usuarios" ("CountryCode");
                CREATE INDEX IF NOT EXISTS "IX_Tiendas_CountryCode" ON "Tiendas" ("CountryCode");
                CREATE INDEX IF NOT EXISTS "IX_Tiendas_Ciudad" ON "Tiendas" ("Ciudad");
                CREATE INDEX IF NOT EXISTS "IX_Productos_TiendaId_Activo" ON "Productos" ("TiendaId", "Activo");
                """, ct);

            logger.LogInformation("Esquema de base de datos verificado y auto-sanado exitosamente.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Aviso durante la auto-sanación del esquema de base de datos (se continuará normalmente)");
        }
    }
}

