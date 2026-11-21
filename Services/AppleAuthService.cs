using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using BuscaYa.Services.IServices;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace BuscaYa.Services;

/// <summary>
/// Verifica identityTokens de Sign in with Apple usando las claves públicas (JWKS) de Apple.
/// No requiere Firebase. Válido para iOS, Android y Web con Sign in with Apple.
/// </summary>
public class AppleAuthService : IAppleAuthService
{
    private const string AppleKeysUrl = "https://appleid.apple.com/auth/keys";
    private const string AppleIssuer = "https://appleid.apple.com";
    private static readonly TimeSpan JwksCacheDuration = TimeSpan.FromHours(24);

    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AppleAuthService> _logger;
    private readonly IMemoryCache _cache;

    public AppleAuthService(HttpClient httpClient, IConfiguration configuration, ILogger<AppleAuthService> logger, IMemoryCache cache)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _cache = cache;
    }

    public async Task<AppleTokenPayload> VerifyIdentityTokenAsync(string identityToken, string? userIdentifier = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(identityToken))
            throw new ArgumentException("El identityToken es requerido.", nameof(identityToken));

        var cleanToken = identityToken.Trim().Trim('"').Trim();

        var clientId = _configuration["Apple:ClientId"]?.Trim();
        var validAudiences = _configuration.GetSection("Apple:ValidAudiences").Get<string[]>()
                             ?? (!string.IsNullOrEmpty(clientId)
                                 ? clientId.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                 : Array.Empty<string>());

        var handler = new JwtSecurityTokenHandler
        {
            MapInboundClaims = false
        };

        if (!handler.CanReadToken(cleanToken))
            throw new UnauthorizedAccessException("Token de Apple inválido o mal formado.");

        var jwt = handler.ReadJwtToken(cleanToken);
        var kid = jwt.Header.Kid;
        if (string.IsNullOrEmpty(kid))
            throw new UnauthorizedAccessException("Token de Apple no contiene 'kid' en el header.");

        var signingKeys = await GetAppleSigningKeysAsync(cancellationToken).ConfigureAwait(false);
        var key = signingKeys.FirstOrDefault(k => string.Equals(k.KeyId, kid, StringComparison.Ordinal));
        if (key == null)
        {
            // Apple puede rotar sus claves JWKS; invalidar caché y reintentar una vez
            _cache.Remove("Apple_JWKS");
            signingKeys = await GetAppleSigningKeysAsync(cancellationToken).ConfigureAwait(false);
            key = signingKeys.FirstOrDefault(k => string.Equals(k.KeyId, kid, StringComparison.Ordinal));
        }
        if (key == null)
            throw new UnauthorizedAccessException("No se encontró la clave de firma de Apple para este token.");

        var validationParameters = new TokenValidationParameters
        {
            ValidIssuer = AppleIssuer,
            ValidateIssuer = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ClockSkew = TimeSpan.FromMinutes(5)
        };

        if (validAudiences.Length > 0)
        {
            validationParameters.ValidateAudience = true;
            validationParameters.ValidAudiences = validAudiences;
        }
        else
        {
            validationParameters.ValidateAudience = false;
        }

        try
        {
            var principal = handler.ValidateToken(cleanToken, validationParameters, out var validatedToken);

            // ================= EXTRACCIÓN ULTRA-ROBUSTA DE 'sub' (AppleId) =================
            var sub = ExtractSub(principal, validatedToken, jwt, userIdentifier);

            if (string.IsNullOrWhiteSpace(sub))
            {
                _logger.LogError("Token de Apple verificado con éxito pero no se pudo extraer el identificador 'sub'. Claims: {Claims}",
                    string.Join("; ", principal?.Claims.Select(c => $"{c.Type}={c.Value}") ?? Array.Empty<string>()));
                throw new UnauthorizedAccessException("Token de Apple no contiene 'sub'.");
            }

            // ================= EXTRACCIÓN ULTRA-ROBUSTA DE 'email' =================
            var email = ExtractEmail(principal, jwt);

            // ================= EXTRACCIÓN ULTRA-ROBUSTA DE 'name' =================
            var name = ExtractName(principal, jwt);

            _logger.LogInformation("Sign In with Apple verificado exitosamente para AppleId: {AppleId}, Email: {Email}", sub, email ?? "N/A");
            return new AppleTokenPayload(sub, email, name);
        }
        catch (SecurityTokenExpiredException)
        {
            throw new UnauthorizedAccessException("Token de Apple expirado.");
        }
        catch (SecurityTokenInvalidAudienceException)
        {
            _logger.LogWarning("Token de Apple rechazado: aud no coincide con las audiencias válidas configuradas.");
            throw new UnauthorizedAccessException("Token de Apple no es válido para esta aplicación.");
        }
    }

    private static string? ExtractSub(ClaimsPrincipal? principal, SecurityToken? validatedToken, JwtSecurityToken jwt, string? userIdentifier)
    {
        // 1. ClaimsPrincipal FindFirst directo
        var sub = ExtractClaimString(principal?.FindFirst("sub")?.Value)
                  ?? ExtractClaimString(principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value);

        // 2. Búsqueda insensible a mayúsculas o sufijos en ClaimsPrincipal
        if (string.IsNullOrWhiteSpace(sub) && principal != null)
        {
            sub = ExtractClaimString(principal.Claims.FirstOrDefault(c =>
                c.Type.Equals("sub", StringComparison.OrdinalIgnoreCase) ||
                c.Type.EndsWith("/nameidentifier", StringComparison.OrdinalIgnoreCase) ||
                c.Type.EndsWith(":nameidentifier", StringComparison.OrdinalIgnoreCase))?.Value);
        }

        // 3. Subject del validatedToken
        if (string.IsNullOrWhiteSpace(sub) && validatedToken != null)
        {
            if (validatedToken is JwtSecurityToken jst)
                sub = ExtractClaimString(jst.Subject);
            else if (validatedToken is Microsoft.IdentityModel.JsonWebTokens.JsonWebToken jwToken)
                sub = ExtractClaimString(jwToken.Subject);
        }

        // 4. Token ya parseado por ReadJwtToken
        if (string.IsNullOrWhiteSpace(sub) && jwt != null)
        {
            sub = ExtractClaimString(jwt.Subject);

            if (string.IsNullOrWhiteSpace(sub) && jwt.Payload != null)
            {
                if (jwt.Payload.TryGetValue("sub", out var rawSub))
                    sub = ExtractClaimString(rawSub);
                else if (jwt.Payload.TryGetValue(ClaimTypes.NameIdentifier, out var rawNameId))
                    sub = ExtractClaimString(rawNameId);
            }

            if (string.IsNullOrWhiteSpace(sub) && jwt.Claims != null)
            {
                sub = ExtractClaimString(jwt.Claims.FirstOrDefault(c =>
                    c.Type.Equals("sub", StringComparison.OrdinalIgnoreCase) ||
                    c.Type.EndsWith("/nameidentifier", StringComparison.OrdinalIgnoreCase))?.Value);
            }
        }

        // 5. Fallback a userIdentifier enviado por Apple Credential (si el token fue firmado por Apple)
        if (string.IsNullOrWhiteSpace(sub) && !string.IsNullOrWhiteSpace(userIdentifier))
        {
            sub = userIdentifier.Trim();
        }

        return sub;
    }

    private static string? ExtractEmail(ClaimsPrincipal? principal, JwtSecurityToken jwt)
    {
        var email = ExtractClaimString(principal?.FindFirst("email")?.Value)
                    ?? ExtractClaimString(principal?.FindFirst(ClaimTypes.Email)?.Value);

        if (string.IsNullOrWhiteSpace(email) && principal != null)
        {
            email = ExtractClaimString(principal.Claims.FirstOrDefault(c =>
                c.Type.Equals("email", StringComparison.OrdinalIgnoreCase) ||
                c.Type.EndsWith("/emailaddress", StringComparison.OrdinalIgnoreCase))?.Value);
        }

        if (string.IsNullOrWhiteSpace(email) && jwt != null)
        {
            if (jwt.Payload != null && jwt.Payload.TryGetValue("email", out var rawEmail))
                email = ExtractClaimString(rawEmail);

            if (string.IsNullOrWhiteSpace(email) && jwt.Claims != null)
            {
                email = ExtractClaimString(jwt.Claims.FirstOrDefault(c =>
                    c.Type.Equals("email", StringComparison.OrdinalIgnoreCase) ||
                    c.Type.EndsWith("/emailaddress", StringComparison.OrdinalIgnoreCase))?.Value);
            }
        }

        return email;
    }

    private static string? ExtractName(ClaimsPrincipal? principal, JwtSecurityToken jwt)
    {
        var name = ExtractClaimString(principal?.FindFirst("name")?.Value)
                   ?? ExtractClaimString(principal?.FindFirst(ClaimTypes.Name)?.Value);

        if (string.IsNullOrWhiteSpace(name) && principal != null)
        {
            name = ExtractClaimString(principal.Claims.FirstOrDefault(c =>
                c.Type.Equals("name", StringComparison.OrdinalIgnoreCase) ||
                c.Type.EndsWith("/name", StringComparison.OrdinalIgnoreCase))?.Value);
        }

        if (string.IsNullOrWhiteSpace(name) && jwt != null)
        {
            if (jwt.Payload != null && jwt.Payload.TryGetValue("name", out var rawName))
                name = ExtractClaimString(rawName);

            if (string.IsNullOrWhiteSpace(name) && jwt.Claims != null)
            {
                name = ExtractClaimString(jwt.Claims.FirstOrDefault(c =>
                    c.Type.Equals("name", StringComparison.OrdinalIgnoreCase) ||
                    c.Type.EndsWith("/name", StringComparison.OrdinalIgnoreCase))?.Value);
            }
        }

        return name;
    }

    private static string? ExtractClaimString(object? val)
    {
        if (val == null) return null;
        if (val is string s) return string.IsNullOrWhiteSpace(s) ? null : s.Trim();
        if (val is JsonElement je)
        {
            if (je.ValueKind == JsonValueKind.String)
            {
                var str = je.GetString();
                return string.IsNullOrWhiteSpace(str) ? null : str.Trim();
            }
            if (je.ValueKind != JsonValueKind.Null && je.ValueKind != JsonValueKind.Undefined)
            {
                var str = je.ToString();
                return string.IsNullOrWhiteSpace(str) ? null : str.Trim();
            }
            return null;
        }
        var result = val.ToString()?.Trim();
        return string.IsNullOrWhiteSpace(result) ? null : result;
    }

    private async Task<IEnumerable<SecurityKey>> GetAppleSigningKeysAsync(CancellationToken cancellationToken)
    {
        return await _cache.GetOrCreateAsync("Apple_JWKS", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = JwksCacheDuration;
            var json = await _httpClient.GetStringAsync(AppleKeysUrl, cancellationToken).ConfigureAwait(false);
            var jwks = new JsonWebKeySet(json);
            return jwks.GetSigningKeys();
        }).ConfigureAwait(false) ?? Array.Empty<SecurityKey>();
    }
}
