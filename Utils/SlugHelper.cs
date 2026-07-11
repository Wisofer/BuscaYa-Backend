using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;

namespace BuscaYa.Utils;

public static class SlugHelper
{
    /// <summary>
    /// Genera un slug limpio a partir de un nombre.
    /// Ej: "Ferretería López & Hijos" → "ferreteria-lopez-hijos"
    /// </summary>
    public static string Generar(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre)) return string.Empty;

        // 1. Normalizar a NFD para separar caracteres base de diacríticos
        var normalizado = nombre.Normalize(NormalizationForm.FormD);

        // 2. Eliminar diacríticos (tildes, ñ→n, etc.)
        var sb = new StringBuilder();
        foreach (var c in normalizado)
        {
            var categoria = CharUnicodeInfo.GetUnicodeCategory(c);
            if (categoria != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        var resultado = sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();

        // 3. Reemplazar caracteres especiales comunes por guión
        resultado = resultado.Replace("&", "y").Replace("/", "-").Replace("\\", "-");

        // 4. Eliminar todo lo que no sea letra, número o guión
        resultado = Regex.Replace(resultado, @"[^a-z0-9\-]", "-");

        // 5. Colapsar múltiples guiones consecutivos en uno solo
        resultado = Regex.Replace(resultado, @"-{2,}", "-");

        // 6. Eliminar guiones al inicio y al final
        resultado = resultado.Trim('-');

        return resultado;
    }

    /// <summary>
    /// Genera un slug único verificando contra la BD.
    /// Si "ferreteria-lopez" ya existe, devuelve "ferreteria-lopez-2", etc.
    /// </summary>
    public static string GenerarUnico<T>(
        string nombre,
        IQueryable<T> dbSet,
        Func<T, string> slugSelector,
        int? excludeId = null,
        Func<T, int>? idSelector = null)
    {
        var baseSlug = Generar(nombre);
        if (string.IsNullOrEmpty(baseSlug)) baseSlug = "negocio";

        var slugActual = baseSlug;
        var contador = 2;

        while (true)
        {
            var localSlug = slugActual;
            var query = dbSet.Where(e => slugSelector(e) == localSlug);

            // Excluir el propio registro en actualizaciones
            if (excludeId.HasValue && idSelector != null)
                query = query.Where(e => idSelector(e) != excludeId.Value);

            var existe = query.Any();
            if (!existe) break;

            slugActual = $"{baseSlug}-{contador}";
            contador++;
        }

        return slugActual;
    }
}
