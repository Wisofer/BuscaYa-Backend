using System.Text.Json;
using BuscaYa.Services.IServices;
using Microsoft.Extensions.Configuration;

namespace BuscaYa.Services;

/// <summary>
/// Verifica idTokens de Google usando el endpoint tokeninfo de OAuth2.
/// No requiere Firebase Admin; válido para tokens de Google Sign-In (iOS, Android, Web).
/// </summary>
public class GoogleAuthService : IGoogleAuthService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private const string TokenInfoUrl = "https://oauth2.googleapis.com/tokeninfo?id_token=";

    public GoogleAuthService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<GoogleTokenPayload> VerifyIdTokenAsync(string idToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idToken))
            throw new ArgumentException("El idToken es requerido.", nameof(idToken));

        var response = await _httpClient.GetAsync(TokenInfoUrl + Uri.EscapeDataString(idToken), cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new UnauthorizedAccessException(
                response.StatusCode == System.Net.HttpStatusCode.BadRequest
                    ? "Token de Google inválido o expirado."
                    : $"Error al verificar token: {response.StatusCode}. {body}");
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        string? sub = GetString(root, "sub");
        if (string.IsNullOrEmpty(sub))
            throw new UnauthorizedAccessException("Token de Google no contiene 'sub'.");

        // Validar que el token sea para nuestra app (aud debe ser uno de nuestros client IDs)
        var allowedAudiences = GetAllowedAudiences();
        if (allowedAudiences.Count > 0)
        {
            var aud = GetString(root, "aud");
            if (string.IsNullOrEmpty(aud) || !allowedAudiences.Contains(aud))
                throw new UnauthorizedAccessException("Token de Google no es válido para esta aplicación.");
        }

        string? email = GetString(root, "email");
        string? name = GetString(root, "name");
        string? picture = GetString(root, "picture");

        return new GoogleTokenPayload(sub, email, name, picture);
    }

    private List<string> GetAllowedAudiences()
    {
        var list = new List<string>();
        var section = _configuration.GetSection("Google:ClientIds");
        foreach (var child in section.GetChildren())
        {
            var value = child.Value;
            if (!string.IsNullOrWhiteSpace(value))
                list.Add(value.Trim());
        }
        // Si no hay configuración, aceptamos cualquier aud (útil en dev)
        return list;
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop))
            return prop.GetString();
        return null;
    }
}
