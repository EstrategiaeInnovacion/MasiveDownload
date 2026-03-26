using System;
using System.IO;
using System.Windows;

namespace VucemDownloader
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            try
            {
                var (tieneLicencia, vencimiento) = LicenseWindow.CargarLicenciaGuardada();

                if (!tieneLicencia)
                {
                    LicenseWindow licenseWindow = new LicenseWindow();
                    licenseWindow.ShowDialog();

                    if (licenseWindow.DialogResult != true)
                    {
                        Shutdown();
                        return;
                    }
                }

                MainWindow mainWindow = new MainWindow();
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}\n\n{ex.StackTrace}", "Error");
                Shutdown();
            }
        }
    }

    public class SessionInfo
    {
        public string RFC { get; set; } = string.Empty;
        public System.Security.Cryptography.X509Certificates.X509Certificate2? Certificado { get; set; }
        public Org.BouncyCastle.Crypto.AsymmetricKeyParameter? LlavePrivada { get; set; }
    }
}
