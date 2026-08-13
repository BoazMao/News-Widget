using System.Security.Cryptography;
using System.Text;

namespace sellthenews.Services;

public sealed class ApiKeyStore
{
    private readonly string directoryPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "NewsWidget");
    private string KeyPath => Path.Combine(directoryPath, "newsapi.key");
    private string LanguagePath => Path.Combine(directoryPath, "wsb-language.txt");

    public string? Load()
    {
        try
        {
            if (!File.Exists(KeyPath))
                return null;

            byte[] protectedBytes = File.ReadAllBytes(KeyPath);
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

        Directory.CreateDirectory(directoryPath);
        byte[] clearBytes = Encoding.UTF8.GetBytes(apiKey.Trim());
        byte[] protectedBytes = ProtectedData.Protect(clearBytes, null, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(KeyPath, protectedBytes);
    }

    public string LoadWsbLanguage()
    {
        try
        {
            string language = File.Exists(LanguagePath) ? File.ReadAllText(LanguagePath).Trim() : "en";
            return language == "zh" ? "zh" : "en";
        }
        catch (IOException)
        {
            return "en";
        }
    }

    public void SaveWsbLanguage(string language)
    {
        Directory.CreateDirectory(directoryPath);
        File.WriteAllText(LanguagePath, language == "zh" ? "zh" : "en");
    }

    public void Clear()
    {
        if (File.Exists(KeyPath))
            File.Delete(KeyPath);
    }
}
