using System.Security.Cryptography;
using System.Text;

namespace BuscaYa.Utils;

public static class TokenHelper
{
    private static readonly char[] Alphabet = 
        "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();

    public static string GenerarToken(int size = 24)
    {
        var result = new StringBuilder(size);
        for (int i = 0; i < size; i++)
        {
            int index = RandomNumberGenerator.GetInt32(Alphabet.Length);
            result.Append(Alphabet[index]);
        }
        return result.ToString();
    }
}
