using System.Security.Cryptography;
using System.Text;

// AES-256-GCM: confidentiality + integrity. Wire format per line: Base64(nonce | tag | ciphertext)
static class Crypto
{
    // ponytail: fixed salt, fine for a shared-passphrase demo; exchange a per-session salt if keys must differ per connection
    static readonly byte[] Salt = Encoding.UTF8.GetBytes("NerveSocketAssignment");

    public static byte[] DeriveKey(string passphrase) =>
        Rfc2898DeriveBytes.Pbkdf2(passphrase, Salt, 100_000, HashAlgorithmName.SHA256, 32);

    public static string Encrypt(string plain, byte[] key)
    {
        byte[] nonce = RandomNumberGenerator.GetBytes(12);
        byte[] pt = Encoding.UTF8.GetBytes(plain);
        byte[] ct = new byte[pt.Length];
        byte[] tag = new byte[16];
        using AesGcm aes = new(key, 16);
        aes.Encrypt(nonce, pt, ct, tag);
        return Convert.ToBase64String([.. nonce, .. tag, .. ct]);
    }

    // Throws CryptographicException on wrong key or tampered data
    public static string Decrypt(string cipher, byte[] key)
    {
        byte[] all = Convert.FromBase64String(cipher);
        byte[] pt = new byte[all.Length - 28];
        using AesGcm aes = new(key, 16);
        aes.Decrypt(all.AsSpan(0, 12), all.AsSpan(28), all.AsSpan(12, 16), pt);
        return Encoding.UTF8.GetString(pt);
    }
}
