using System;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace VucemDownloader
{
    public static class CredentialManager
    {
        private static readonly string AppDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "VucemDownloader");

        private static readonly string SettingsFile = Path.Combine(AppDataPath, "settings.json");

        public static void SaveCredentials(string rutaCer, string rutaKey, string password)
        {
            try
            {
                if (!Directory.Exists(AppDataPath))
                    Directory.CreateDirectory(AppDataPath);

                var settings = new AppSettings
                {
                    RutaCertificado = rutaCer,
                    RutaLlave = rutaKey,
                    Contrasena = EncryptPassword(password),
                    Guardado = DateTime.Now
                };

                string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });

                File.WriteAllText(SettingsFile, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error guardando credenciales: {ex.Message}");
            }
        }

        public static (string rutaCer, string rutaKey, string password)? LoadCredentials()
        {
            try
            {
                if (!File.Exists(SettingsFile))
                    return null;

                string json = File.ReadAllText(SettingsFile);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);

                if (settings == null || string.IsNullOrEmpty(settings.RutaCertificado))
                    return null;

                if (!File.Exists(settings.RutaCertificado) || !File.Exists(settings.RutaLlave))
                    return null;

                string password = DecryptPassword(settings.Contrasena);
                return (settings.RutaCertificado, settings.RutaLlave, password);
            }
            catch
            {
                return null;
            }
        }

        public static void ClearCredentials()
        {
            try
            {
                if (File.Exists(SettingsFile))
                    File.Delete(SettingsFile);
            }
            catch { }
        }

        private static string EncryptPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                return string.Empty;

            byte[] plainBytes = System.Text.Encoding.UTF8.GetBytes(password);
            byte[] encryptedBytes = ProtectedData.Protect(plainBytes, null, System.Security.Cryptography.DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encryptedBytes);
        }

        private static string DecryptPassword(string encryptedPassword)
        {
            if (string.IsNullOrEmpty(encryptedPassword))
                return string.Empty;

            try
            {
                byte[] encryptedBytes = Convert.FromBase64String(encryptedPassword);
                byte[] decryptedBytes = ProtectedData.Unprotect(encryptedBytes, null, System.Security.Cryptography.DataProtectionScope.CurrentUser);
                return System.Text.Encoding.UTF8.GetString(decryptedBytes);
            }
            catch
            {
                return string.Empty;
            }
        }

        private class AppSettings
        {
            [JsonPropertyName("certificado")]
            public string RutaCertificado { get; set; } = string.Empty;

            [JsonPropertyName("llave")]
            public string RutaLlave { get; set; } = string.Empty;

            [JsonPropertyName("contrasena")]
            public string Contrasena { get; set; } = string.Empty;

            [JsonPropertyName("guardado")]
            public DateTime Guardado { get; set; }
        }
    }
}
