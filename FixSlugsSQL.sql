-- Script para corregir slugs en producción
-- Ejecutar contra la base de datos Neon Tech

-- Ver slugs actuales
SELECT "Id", "Nombre", "Slug" 
FROM "Tiendas" 
WHERE "Slug" IS NOT NULL AND "Slug" != '' 
ORDER BY "Id";

-- Crear función para extraer slug base (sin -X al final)
CREATE OR REPLACE FUNCTION get_base_slug(slug TEXT) RETURNS TEXT AS $$
BEGIN
    -- Si termina en -X (donde X es número), eliminar el número
    IF slug ~ '-[0-9]+$' THEN
        RETURN regexp_replace(slug, '-[0-9]+$', '');
    END IF;
    RETURN slug;
END;
$$ LANGUAGE plpgsql;

-- Actualizar tiendas: extraer slug base
UPDATE "Tiendas" 
SET "Slug" = get_base_slug("Slug")
WHERE "Slug" IS NOT NULL AND "Slug" != '' AND "Slug" ~ '-[0-9]+$';

-- Ver resultados
SELECT "Id", "Nombre", "Slug" 
FROM "Tiendas" 
WHERE "Slug" IS NOT NULL AND "Slug" != '' 
ORDER BY "Id";

-- Verificar duplicados (debería estar vacío)
SELECT "Slug", COUNT(*) as count
FROM "Tiendas"
WHERE "Slug" IS NOT NULL AND "Slug" != ''
GROUP BY "Slug"
HAVING COUNT(*) > 1;
