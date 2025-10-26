using System.Security.Cryptography;
using System.Text;

namespace JISMemo.Services;

public class EncryptionService
{
    public static string Encrypt(string plainText, string password)
    {
        byte[] salt = Encoding.UTF8.GetBytes("JISMemoSalt2025");
        using var aes = Aes.Create();
        var key = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256);
        aes.Key = key.GetBytes(32);
        aes.IV = key.GetBytes(16);

        using var encryptor = aes.CreateEncryptor();
        byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
        byte[] encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        return Convert.ToBase64String(encryptedBytes);
    }

    public static string Decrypt(string encryptedText, string password)
    {
        byte[] salt = Encoding.UTF8.GetBytes("JISMemoSalt2025");
        using var aes = Aes.Create();
        var key = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256);
        aes.Key = key.GetBytes(32);
        aes.IV = key.GetBytes(16);

        using var decryptor = aes.CreateDecryptor();
        byte[] encryptedBytes = Convert.FromBase64String(encryptedText);
        byte[] decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
        return Encoding.UTF8.GetString(decryptedBytes);
    }
}
