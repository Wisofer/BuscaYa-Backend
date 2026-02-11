namespace BuscaYa.Services.IServices;

/// <summary>Resultado de verificar un identityToken de Sign in with Apple.</summary>
public record AppleTokenPayload(string AppleId, string? Email, string? Name);

/// <summary>Servicio para verificar identityTokens de Sign in with Apple (JWT con claves públicas de Apple).</summary>
public interface IAppleAuthService
{
    /// <summary>Verifica el identityToken con las claves públicas de Apple (JWKS) y devuelve sub (AppleId), email y name si vienen en el token.</summary>
    Task<AppleTokenPayload> VerifyIdentityTokenAsync(string identityToken, CancellationToken cancellationToken = default);
}
