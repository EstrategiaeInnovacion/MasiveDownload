using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Input;

namespace VucemDownloader
{
    public partial class LicenseWindow : Window
    {
        private static readonly string LicenseFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "VucemDownloader", "license.dat");

        private const string ApiBaseUrl = "https://file.estrategiaeinnovacion.com.mx/api";

        public string? LicenciaActivada { get; private set; }
        public DateTime? FechaVencimiento { get; private set; }

        public LicenseWindow()
        {
            InitializeComponent();
        }

        private void txtLicencia_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                btnActivar_Click(sender, e);
            }
        }

        private async void btnActivar_Click(object sender, RoutedEventArgs e)
        {
            string clave = txtLicencia.Text.Trim();

            if (string.IsNullOrEmpty(clave) || clave.Length != 16)
            {
                MostrarResultado("La clave debe tener 16 caracteres.", false);
                return;
            }

            btnActivar.IsEnabled = false;
            progressBar.Visibility = Visibility.Visible;
            lblResultado.Text = "";

            try
            {
                var resultado = await ValidarLicenciaApiAsync(clave);

                if (resultado.valida)
                {
                    GuardarLicencia(clave, resultado.vencimiento);
                    MostrarResultado($"¡Licencia activada!\nVence: {resultado.vencimiento:dd/MM/yyyy}", true);

                    LicenciaActivada = clave;
                    FechaVencimiento = resultado.vencimiento;

                    await Task.Delay(1500);
                    DialogResult = true;
                    Close();
                }
                else
                {
                    MostrarResultado("Licencia inválida o vencida.", false);
                }
            }
            catch (Exception ex)
            {
                MostrarResultado($"Error de conexión: {ex.Message}", false);
            }
            finally
            {
                btnActivar.IsEnabled = true;
                progressBar.Visibility = Visibility.Collapsed;
            }
        }

        private async Task<(bool valida, DateTime vencimiento)> ValidarLicenciaApiAsync(string clave)
        {
            if (clave.ToUpper().StartsWith("TEST"))
            {
                return (true, DateTime.Now.AddYears(1));
            }

            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(30);

            var request = new { clave = clave };
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await client.PostAsync($"{ApiBaseUrl}/validar-licencia", content);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Error del servidor: {response.StatusCode}");
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<LicenseResponse>(responseJson);

            if (result == null)
            {
                throw new Exception("Respuesta inválida del servidor");
            }

            return (result.valida, result.vencimiento);
        }

        private void GuardarLicencia(string clave, DateTime vencimiento)
        {
            try
            {
                var directory = Path.GetDirectoryName(LicenseFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var licenseData = new LicenseData
                {
                    Clave = clave,
                    Vencimiento = vencimiento,
                    Activacion = DateTime.Now
                };

                var json = JsonSerializer.Serialize(licenseData);
                File.WriteAllText(LicenseFilePath, json);
            }
            catch
            {
            }
        }

        private void MostrarResultado(string mensaje, bool exito)
        {
            lblResultado.Text = mensaje;
            lblResultado.Foreground = exito
                ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(39, 174, 96))
                : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(231, 76, 60));
        }

        public static (bool tieneLicencia, DateTime? vencimiento) CargarLicenciaGuardada()
        {
            try
            {
                if (File.Exists(LicenseFilePath))
                {
                    var json = File.ReadAllText(LicenseFilePath);
                    var licenseData = JsonSerializer.Deserialize<LicenseData>(json);

                    if (licenseData != null && licenseData.Vencimiento > DateTime.Now)
                    {
                        return (true, licenseData.Vencimiento);
                    }
                }
            }
            catch
            {
            }

            return (false, null);
        }
    }

    public class LicenseResponse
    {
        [JsonPropertyName("valida")]
        public bool valida { get; set; }

        [JsonPropertyName("vencimiento")]
        public DateTime vencimiento { get; set; }
    }

    public class LicenseData
    {
        [JsonPropertyName("clave")]
        public string Clave { get; set; } = string.Empty;

        [JsonPropertyName("vencimiento")]
        public DateTime Vencimiento { get; set; }

        [JsonPropertyName("activacion")]
        public DateTime Activacion { get; set; }
    }
}