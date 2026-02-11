using System.IdentityModel.Tokens.Jwt;
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

    public async Task<AppleTokenPayload> VerifyIdentityTokenAsync(string identityToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(identityToken))
            throw new ArgumentException("El identityToken es requerido.", nameof(identityToken));

        var clientId = _configuration["Apple:ClientId"]?.Trim();
        if (string.IsNullOrEmpty(clientId))
            _logger.LogWarning("Apple:ClientId no configurado. La validación de audiencia podría fallar. Use el Bundle ID o Services ID de su app.");

        var handler = new JwtSecurityTokenHandler();
        if (!handler.CanReadToken(identityToken))
            throw new UnauthorizedAccessException("Token de Apple inválido o mal formado.");

        var jwt = handler.ReadJwtToken(identityToken);
        var kid = jwt.Header.Kid;
        if (string.IsNullOrEmpty(kid))
            throw new UnauthorizedAccessException("Token de Apple no contiene 'kid' en el header.");

        var signingKeys = await GetAppleSigningKeysAsync(cancellationToken).ConfigureAwait(false);
        var key = signingKeys.FirstOrDefault(k => string.Equals(k.KeyId, kid, StringComparison.Ordinal));
        if (key == null)
            throw new UnauthorizedAccessException("No se encontró la clave de firma de Apple para este token.");

        var validationParameters = new TokenValidationParameters
        {
            ValidIssuer = AppleIssuer,
            ValidAudience = clientId,
            ValidateIssuer = true,
            ValidateAudience = !string.IsNullOrEmpty(clientId),
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ClockSkew = TimeSpan.FromMinutes(2)
        };

        try
        {
            var principal = handler.ValidateToken(identityToken, validationParameters, out _);
            var sub = principal.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(sub))
                throw new UnauthorizedAccessException("Token de Apple no contiene 'sub'.");

            var email = principal.FindFirst("email")?.Value;
            var nameClaim = principal.FindFirst("name")?.Value;
            return new AppleTokenPayload(sub, email, nameClaim);
        }
        catch (SecurityTokenExpiredException)
        {
            throw new UnauthorizedAccessException("Token de Apple expirado.");
        }
        catch (SecurityTokenInvalidAudienceException)
        {
            _logger.LogWarning("Token de Apple rechazado: aud no coincide con Apple:ClientId.");
            throw new UnauthorizedAccessException("Token de Apple no es válido para esta aplicación.");
        }
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
