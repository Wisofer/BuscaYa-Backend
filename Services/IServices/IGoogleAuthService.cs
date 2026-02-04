namespace BuscaYa.Services.IServices;

/// <summary>
/// Resultado de verificar un idToken de Google (Firebase).
/// </summary>
public record GoogleTokenPayload(string GoogleId, string? Email, string? Name, string? PictureUrl);

/// <summary>
/// Servicio para verificar idTokens de Google/Firebase.
/// </summary>
public interface IGoogleAuthService
{
    /// <summary>
    /// Verifica el idToken con Firebase y devuelve el payload (sub, email, name, picture).
    /// Lanza si el token es inválido o expirado.
    /// </summary>
    Task<GoogleTokenPayload> VerifyIdTokenAsync(string idToken, CancellationToken cancellationToken = default);
}
