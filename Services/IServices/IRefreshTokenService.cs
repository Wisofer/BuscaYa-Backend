using BuscaYa.Models.Entities;

namespace BuscaYa.Services.IServices;

public interface IRefreshTokenService
{
    /// <summary>Crea un refresh token para el usuario y devuelve el valor en claro (solo para enviar al cliente una vez).</summary>
    Task<string> CreateAsync(int userId);

    /// <summary>Valida el refresh token, revoca el usado y emite uno nuevo (rotación). Devuelve el usuario y el nuevo refresh en claro.</summary>
    Task<(Usuario? user, string? newRefreshToken, string? error)> RotateAsync(string plainRefreshToken);

    /// <summary>Revoca un refresh token concreto (logout en un dispositivo).</summary>
    Task RevokeAsync(string plainRefreshToken);

    /// <summary>Revoca todos los refresh tokens del usuario (logout en todos los dispositivos).</summary>
    Task RevokeAllForUserAsync(int userId);
}
