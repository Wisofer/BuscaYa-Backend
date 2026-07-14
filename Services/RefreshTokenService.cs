using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using BuscaYa.Data;
using BuscaYa.Models.Entities;
using BuscaYa.Services.IServices;

namespace BuscaYa.Services;

public class RefreshTokenService : IRefreshTokenService
{
    private readonly ApplicationDbContext _context;
    private readonly int _refreshTokenExpirationInDays;

    public RefreshTokenService(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        var jwtSettings = configuration.GetSection("JwtSettings");
        _refreshTokenExpirationInDays = int.Parse(jwtSettings["RefreshTokenExpirationInDays"] ?? "45");
    }

    public static string HashToken(string plainToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(plainToken));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string GeneratePlainToken()
    {
        var bytes = new byte[48];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    public async Task<string> CreateAsync(int userId)
    {
        var plain = GeneratePlainToken();
        var entity = new RefreshToken
        {
            UsuarioId = userId,
            TokenHash = HashToken(plain),
            ExpiresAt = DateTime.UtcNow.AddDays(_refreshTokenExpirationInDays),
            CreatedAt = DateTime.UtcNow
        };
        _context.RefreshTokens.Add(entity);
        await _context.SaveChangesAsync();
        return plain;
    }

    public async Task<(Usuario? user, string? newRefreshToken, string? error)> RotateAsync(string plainRefreshToken)
    {
        if (string.IsNullOrWhiteSpace(plainRefreshToken))
            return (null, null, "refresh_token es requerido.");

        var hash = HashToken(plainRefreshToken);
        var existing = await _context.RefreshTokens
            .Include(r => r.Usuario)
            .FirstOrDefaultAsync(r => r.TokenHash == hash);

        if (existing == null)
            return (null, null, "Refresh token inválido.");
        if (existing.RevokedAt != null)
            return (null, null, "Refresh token ya fue usado o revocado.");
        if (existing.ExpiresAt < DateTime.UtcNow)
            return (null, null, "Refresh token expirado.");

        existing.RevokedAt = DateTime.UtcNow;

        var newPlain = GeneratePlainToken();
        var newEntity = new RefreshToken
        {
            UsuarioId = existing.UsuarioId,
            TokenHash = HashToken(newPlain),
            ExpiresAt = DateTime.UtcNow.AddDays(_refreshTokenExpirationInDays),
            CreatedAt = DateTime.UtcNow
        };
        _context.RefreshTokens.Add(newEntity);
        await _context.SaveChangesAsync();

        return (existing.Usuario, newPlain, null);
    }

    public async Task RevokeAsync(string plainRefreshToken)
    {
        if (string.IsNullOrWhiteSpace(plainRefreshToken)) return;
        var hash = HashToken(plainRefreshToken);
        var existing = await _context.RefreshTokens.FirstOrDefaultAsync(r => r.TokenHash == hash);
        if (existing == null || existing.RevokedAt != null) return;
        existing.RevokedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task RevokeAllForUserAsync(int userId)
    {
        var now = DateTime.UtcNow;
        await _context.RefreshTokens
            .Where(r => r.UsuarioId == userId && r.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.RevokedAt, now));
    }
}
