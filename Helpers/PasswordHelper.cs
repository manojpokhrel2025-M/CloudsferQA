using System.Security.Cryptography;

namespace CloudsferQA.Helpers;

public static class PasswordHelper
{
    private const int Iterations = 100_000;
    private const int KeyLength  = 32;

    /// <summary>Returns a salted PBKDF2-SHA256 hash in the format "base64salt:base64hash".</summary>
    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var key  = Rfc2898DeriveBytes.Pbkdf2(
            password, salt, Iterations, HashAlgorithmName.SHA256, KeyLength);
        return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(key)}";
    }

    /// <summary>Verifies a plaintext password against a stored hash. Constant-time comparison.</summary>
    public static bool Verify(string password, string storedHash)
    {
        var parts = storedHash.Split(':');
        if (parts.Length != 2) return false;
        try
        {
            var salt    = Convert.FromBase64String(parts[0]);
            var hash    = Convert.FromBase64String(parts[1]);
            var attempt = Rfc2898DeriveBytes.Pbkdf2(
                password, salt, Iterations, HashAlgorithmName.SHA256, KeyLength);
            return CryptographicOperations.FixedTimeEquals(hash, attempt);
        }
        catch { return false; }
    }
}
