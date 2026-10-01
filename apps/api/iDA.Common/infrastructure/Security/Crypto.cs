using System.Security.Cryptography;
using System.Text;
using Ida.Application.Common;
using Microsoft.Extensions.Configuration;

namespace Ida.Infrastructure.Security;

public class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int Iterations = 210_000;
    private const int SaltBytes = 16;
    private const int HashBytes = 32;

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltBytes);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations,
            HashAlgorithmName.SHA256, HashBytes);
        return $"{Iterations}.{System.Convert.ToBase64String(salt)}.{System.Convert.ToBase64String(hash)}";
    }

    public bool Verify(string password, string hash)
    {
        var parts = hash.Split('.');
        if (parts.Length != 3 || !int.TryParse(parts[0], out var iterations)) return false;

        try
        {
            var salt = System.Convert.FromBase64String(parts[1]);
            var expected = System.Convert.FromBase64String(parts[2]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations,
                HashAlgorithmName.SHA256, expected.Length);

            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}

public class AesSecretProtector : ISecretProtector
{
    private const int NonceBytes = 12;
    private const int TagBytes = 16;

    private readonly byte[] _key;
    private readonly byte[] _hashSalt;

    public AesSecretProtector(IConfiguration config)
    {
        var key = config["Security:DataProtectionKey"];
        if (string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException(
                "Security:DataProtectionKey is not configured. Set a 32-byte base64 key " +
                "in appsettings.local.json or the Security__DataProtectionKey environment " +
                "variable — national ids and bank account numbers cannot be stored without it.");

        _key = System.Convert.FromBase64String(key);
        if (_key.Length != 32)
            throw new InvalidOperationException(
                "Security:DataProtectionKey must decode to exactly 32 bytes (AES-256).");

        _hashSalt = Encoding.UTF8.GetBytes(config["Security:HashSalt"] ?? "ida-hash-salt");
    }

    public byte[] Encrypt(string plaintext)
    {
        var nonce = RandomNumberGenerator.GetBytes(NonceBytes);
        var plain = Encoding.UTF8.GetBytes(plaintext);
        var cipher = new byte[plain.Length];
        var tag = new byte[TagBytes];

        using var aes = new AesGcm(_key, TagBytes);
        aes.Encrypt(nonce, plain, cipher, tag);

        var result = new byte[NonceBytes + TagBytes + cipher.Length];
        nonce.CopyTo(result, 0);
        tag.CopyTo(result, NonceBytes);
        cipher.CopyTo(result, NonceBytes + TagBytes);
        return result;
    }

    public string Decrypt(byte[] ciphertext)
    {
        if (ciphertext.Length < NonceBytes + TagBytes)
            throw new ArgumentException("Ciphertext is too short to contain a nonce and tag.",
                nameof(ciphertext));

        var nonce = ciphertext.AsSpan(0, NonceBytes);
        var tag = ciphertext.AsSpan(NonceBytes, TagBytes);
        var cipher = ciphertext.AsSpan(NonceBytes + TagBytes);
        var plain = new byte[cipher.Length];

        using var aes = new AesGcm(_key, TagBytes);
        aes.Decrypt(nonce, cipher, tag, plain);
        return Encoding.UTF8.GetString(plain);
    }

    public string Hash(string plaintext)
    {
        var bytes = Encoding.UTF8.GetBytes(plaintext);
        var salted = new byte[_hashSalt.Length + bytes.Length];
        _hashSalt.CopyTo(salted, 0);
        bytes.CopyTo(salted, _hashSalt.Length);
        return System.Convert.ToHexString(SHA256.HashData(salted)).ToLowerInvariant();
    }

    public string Last4(string plaintext) =>
        plaintext.Length <= 4 ? plaintext.PadLeft(4, '0') : plaintext[^4..];
}

public class SystemClock : IClock
{

    public DateTimeOffset Now => DateTimeOffset.UtcNow;
}
