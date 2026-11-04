using System.Security.Cryptography;
using System.Text;

namespace BuscaYa.Utils;

/// <summary>
/// Proveedor de hashing criptográfico seguro para contraseñas siguiendo estándares OWASP y Microsoft .NET 9.
/// Utiliza PBKDF2 (HMAC-SHA256) con salt aleatorio de 128 bits y 100,000 iteraciones,
/// con retrocompatibilidad automática para hashes heredados de SHA-256.
/// </summary>
public static class PasswordHelper
{
    private const int SaltSize = 16; // 128 bits
    private const int SubkeyLength = 32; // 256 bits
    private const int DefaultIterations = 100_000;
    private const string Pbkdf2Prefix = "pbkdf2:";

    /// <summary>
    /// Genera un hash seguro con PBKDF2 y salt criptográfico único.
    /// </summary>
    public static string HashPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("La contraseña no puede estar vacía", nameof(password));

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var subkey = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            DefaultIterations,
            HashAlgorithmName.SHA256,
            SubkeyLength);

        var saltBase64 = Convert.ToBase64String(salt);
        var subkeyBase64 = Convert.ToBase64String(subkey);

        return $"{Pbkdf2Prefix}{DefaultIterations}:{saltBase64}:{subkeyBase64}";
    }

    /// <summary>
    /// Verifica si la contraseña en texto plano coincide con el hash almacenado.
    /// Soporta tanto hashes modernos PBKDF2 como hashes heredados de SHA256.
    /// </summary>
    public static bool VerifyPassword(string password, string storedHash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedHash))
            return false;

        // 1. Verificación moderna PBKDF2
        if (storedHash.StartsWith(Pbkdf2Prefix, StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var parts = storedHash.Substring(Pbkdf2Prefix.Length).Split(':');
                if (parts.Length != 3) return false;

                if (!int.TryParse(parts[0], out var iterations) || iterations <= 0)
                    return false;

                var salt = Convert.FromBase64String(parts[1]);
                var expectedSubkey = Convert.FromBase64String(parts[2]);

                var actualSubkey = Rfc2898DeriveBytes.Pbkdf2(
                    password,
                    salt,
                    iterations,
                    HashAlgorithmName.SHA256,
                    expectedSubkey.Length);

                // Comparación en tiempo constante para mitigar ataques de temporización (timing attacks)
                return CryptographicOperations.FixedTimeEquals(actualSubkey, expectedSubkey);
            }
            catch
            {
                return false;
            }
        }

        // 2. Verificación retrocompatible: Legacy SHA-256
        return VerifyLegacySha256(password, storedHash);
    }

    /// <summary>
    /// Indica si el hash almacenado requiere ser migrado al formato seguro moderno PBKDF2.
    /// </summary>
    public static bool NeedsRehash(string storedHash)
    {
        if (string.IsNullOrEmpty(storedHash)) return true;
        return !storedHash.StartsWith(Pbkdf2Prefix, StringComparison.OrdinalIgnoreCase);
    }

    private static bool VerifyLegacySha256(string password, string storedHash)
    {
        try
        {
            using var sha256 = SHA256.Create();
            var computedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            var computedBase64 = Convert.ToBase64String(computedBytes);
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(computedBase64),
                Encoding.UTF8.GetBytes(storedHash));
        }
        catch
        {
            return false;
        }
    }
}
