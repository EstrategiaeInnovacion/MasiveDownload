using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;

using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

namespace VucemDownloader
{
    public partial class LoginWindow : Window
    {
        private string rutaCer = string.Empty;
        private string rutaKey = string.Empty;
        private string rfcValidado = string.Empty;
        private X509Certificate2? certificadoValidado;
        private AsymmetricKeyParameter? llavePrivadaValidada;

        public string RfcValidado => rfcValidado;
        public X509Certificate2? CertificadoValidado => certificadoValidado;
        public AsymmetricKeyParameter? LlavePrivadaValidada => llavePrivadaValidada;

        public LoginWindow()
        {
            InitializeComponent();
        }

        public void CargarCredencialesGuardadas(string cer, string key, string password)
        {
            rutaCer = cer;
            rutaKey = key;
            lblCerPath.Text = Path.GetFileName(cer);
            lblCerPath.Foreground = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#27ae60"));
            lblKeyPath.Text = Path.GetFileName(key);
            lblKeyPath.Foreground = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#27ae60"));
            txtPassword.Password = password;
            chkRecordar.IsChecked = true;
        }

        private void btnCer_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Certificado Digital (*.cer)|*.cer";

            if (openFileDialog.ShowDialog() == true)
            {
                rutaCer = openFileDialog.FileName;
                lblCerPath.Text = Path.GetFileName(rutaCer);
                lblCerPath.Foreground = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#27ae60"));
            }
        }

        private void btnKey_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Llave Privada (*.key)|*.key";

            if (openFileDialog.ShowDialog() == true)
            {
                rutaKey = openFileDialog.FileName;
                lblKeyPath.Text = Path.GetFileName(rutaKey);
                lblKeyPath.Foreground = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#27ae60"));
            }
        }

        private void txtPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                btnValidar_Click(sender, e);
            }
        }

        private void btnValidar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(rutaCer) || string.IsNullOrEmpty(rutaKey) || string.IsNullOrEmpty(txtPassword.Password))
            {
                MessageBox.Show("Por favor, completa todos los campos.", "Atención", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            btnValidar.IsEnabled = false;
            progressBar.Visibility = Visibility.Visible;
            progressBar.IsIndeterminate = true;

            try
            {
                string password = txtPassword.Password;
                bool recordar = chkRecordar.IsChecked == true;

                var resultado = ValidarCredenciales(password, recordar);

                if (resultado.exito)
                {
                    MessageBox.Show(
                        $"¡Sesión iniciada correctamente!\n\nRFC: {resultado.rfc}\nVálido desde: {resultado.certificado.NotBefore:dd/MM/yyyy}\nVálido hasta: {resultado.certificado.NotAfter:dd/MM/yyyy}",
                        "VUCEM Suite", MessageBoxButton.OK, MessageBoxImage.Information);

                    DialogResult = true;
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                btnValidar.IsEnabled = true;
                progressBar.Visibility = Visibility.Collapsed;
            }
        }

        private (bool exito, string rfc, X509Certificate2 certificado) ValidarCredenciales(string password, bool recordar)
        {
            X509Certificate2 cert = X509CertificateLoader.LoadCertificateFromFile(rutaCer);
            var (modulusCert, exponentCert) = ExtractPublicKeyDetails(cert);

            byte[] keyBytes = File.ReadAllBytes(rutaKey);
            AsymmetricKeyParameter privateKey = PrivateKeyFactory.DecryptKey(password.ToCharArray(), keyBytes);

            if (privateKey == null || !privateKey.IsPrivate)
            {
                throw new Exception("No se pudo descifrar la llave privada.");
            }

            AsymmetricKeyParameter publicKeyFromPrivate;
            Org.BouncyCastle.Math.BigInteger modulusKey;
            Org.BouncyCastle.Math.BigInteger exponentKey;

            if (privateKey is RsaPrivateCrtKeyParameters rsaPriv)
            {
                modulusKey = rsaPriv.Modulus;
                exponentKey = rsaPriv.PublicExponent;
                publicKeyFromPrivate = new RsaKeyParameters(false, modulusKey, exponentKey);
            }
            else if (privateKey is RsaKeyParameters rsaKey)
            {
                modulusKey = rsaKey.Modulus;
                exponentKey = rsaKey.Exponent;
                publicKeyFromPrivate = new RsaKeyParameters(false, modulusKey, exponentKey);
            }
            else
            {
                throw new Exception("El tipo de llave no es RSA.");
            }

            bool modulesEqual = modulusCert.Equals(modulusKey);
            bool exponentEqual = exponentCert.Equals(exponentKey);

            if (!modulesEqual || !exponentEqual)
            {
                throw new Exception("La llave no corresponde al certificado.");
            }

            rfcValidado = GetRFCFromCert(cert);
            certificadoValidado = cert;
            llavePrivadaValidada = privateKey;

            if (recordar)
            {
                GuardarCredenciales(rutaCer, rutaKey, password);
            }

            return (true, rfcValidado, cert);
        }

        private void GuardarCredenciales(string cer, string key, string password)
        {
            CredentialManager.SaveCredentials(cer, key, password);
        }

        private (Org.BouncyCastle.Math.BigInteger modulus, Org.BouncyCastle.Math.BigInteger exponent) ExtractPublicKeyDetails(X509Certificate2 cert)
        {
            RSA? rsa = cert.GetRSAPublicKey();
            if (rsa != null)
            {
                RSAParameters rsaParams = rsa.ExportParameters(false);
                return (
                    new Org.BouncyCastle.Math.BigInteger(1, rsaParams.Modulus!),
                    new Org.BouncyCastle.Math.BigInteger(1, rsaParams.Exponent!)
                );
            }
            throw new NotSupportedException("Solo se soportan llaves RSA.");
        }

        private string GetRFCFromCert(X509Certificate2 cert)
        {
            string subject = cert.Subject;

            if (subject.Contains("SERIALNUMBER="))
            {
                int start = subject.IndexOf("SERIALNUMBER=") + 13;
                int end = subject.IndexOf(',', start);
                if (end == -1) end = subject.Length;
                string serial = subject.Substring(start, end - start).Trim();
                if (serial.Length >= 12 && serial.Length <= 13)
                    return serial;
            }

            var parts = subject.Split(',');
            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                if (trimmed.StartsWith("OID.2.5.4.5="))
                    return trimmed.Substring(11);
                if (trimmed.StartsWith("2.5.4.5="))
                    return trimmed.Substring(8);
                if (trimmed.StartsWith("SERIALNUMBER="))
                    return trimmed.Substring(12);
            }

            return cert.GetNameInfo(X509NameType.SimpleName, false).TrimStart('/');
        }
    }
}
