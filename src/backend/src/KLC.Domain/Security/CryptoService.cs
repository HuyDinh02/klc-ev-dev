using System;
using System.Security.Cryptography;
using System.Text;

namespace KLC.Security;

/// <summary>
/// Centralized cryptographic utility methods for HMAC signatures and secure comparison.
/// Used by payment gateway integrations (VnPay, MoMo) to avoid duplicating crypto logic.
/// </summary>
public static class CryptoService
{
    /// <summary>
    /// Compute HMAC-SHA256 hash. Used by MoMo payment gateway.
    /// </summary>
    /// <param name="key">The secret key.</param>
    /// <param name="data">The data to sign.</param>
    /// <returns>Lowercase hex-encoded HMAC-SHA256 hash.</returns>
    public static string HmacSha256(string key, string data)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// Compute HMAC-SHA512 hash. Used by VnPay payment gateway.
    /// </summary>
    /// <param name="key">The secret key.</param>
    /// <param name="data">The data to sign.</param>
    /// <returns>Lowercase hex-encoded HMAC-SHA512 hash.</returns>
    public static string HmacSha512(string key, string data)
    {
        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// Constant-time string comparison to prevent timing attacks on HMAC signatures.
    /// Both strings are lowercased before comparison.
    /// </summary>
    public static bool ConstantTimeEquals(string a, string b)
    {
        var aBytes = Encoding.UTF8.GetBytes(a.ToLowerInvariant());
        var bBytes = Encoding.UTF8.GetBytes(b.ToLowerInvariant());
        return CryptographicOperations.FixedTimeEquals(aBytes, bBytes);
    }
}
