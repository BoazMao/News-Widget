using System.Security.Cryptography;
using System.Text;

namespace sellthenews.Services;

public sealed class ApiKeyStore
{
    private readonly string filePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "NewsWidget",
        "newsapi.key");

    public string? Load()
    {
        try
        {
            if (!File.Exists(filePath))
                return null;

            byte[] protectedBytes = File.ReadAllBytes(filePath);
            byte[] clearBytes = ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(clearBytes);
        }
        catch (CryptographicException)
        {
            return null;
        }
    }

    public void Save(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Clear();
            return;
        }

        string? directory = Path.GetDirectoryName(filePath);
        if (directory is not null)
            Directory.CreateDirectory(directory);

        byte[] clearBytes = Encoding.UTF8.GetBytes(apiKey.Trim());
        byte[] protectedBytes = ProtectedData.Protect(clearBytes, null, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(filePath, protectedBytes);
    }

    public void Clear()
    {
        if (File.Exists(filePath))
            File.Delete(filePath);
    }
}
