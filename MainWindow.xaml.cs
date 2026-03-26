using System.Windows;
using Microsoft.Win32;

namespace VucemDownloader
{
    public partial class MainWindow : Window
    {
        public SessionInfo? SessionInfo { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            
            CargarInformacionLicencia();
            
            if (SessionInfo != null)
            {
                Title = $"VUCEM Suite - {SessionInfo.RFC}";
                lblSesion.Text = $"RFC: {SessionInfo.RFC}";
                
                if (SessionInfo.Certificado != null)
                {
                    lblInfoCert.Text = SessionInfo.Certificado.Subject;
                    lblVigencia.Text = $"{SessionInfo.Certificado.NotBefore:dd/MM/yyyy} - {SessionInfo.Certificado.NotAfter:dd/MM/yyyy}";
                }
            }
            else
            {
                Title = "VUCEM Suite";
                lblSesion.Text = "Sin sesión activa";
                lblInfoCert.Text = "No cargado";
                lblVigencia.Text = "-";
            }
        }

        private void CargarInformacionLicencia()
        {
            var (tieneLicencia, vencimiento) = LicenseWindow.CargarLicenciaGuardada();
            
            if (tieneLicencia && vencimiento.HasValue)
            {
                lblLicencia.Text = "Clave: Activada";
                lblVencimientoLicencia.Text = $"Vencimiento: {vencimiento.Value:dd/MM/yyyy}";
            }
            else
            {
                lblLicencia.Text = "Clave: No activada";
                lblVencimientoLicencia.Text = "Vencimiento: -";
            }
        }

        private void btnDescargar_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show("Funcionalidad de descarga en desarrollo.", "Info");
        }

        private void btnBuscar_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show("Funcionalidad de búsqueda en desarrollo.", "Info");
        }

        private void btnCancelar_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show("Funcionalidad de cancelación en desarrollo.", "Info");
        }

        private void btnSeleccionarXml_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "XML Files (*.xml)|*.xml";
            
            if (openFileDialog.ShowDialog() == true)
            {
                lblArchivoXml.Text = openFileDialog.FileName;
            }
        }

        private void btnValidarXml_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(lblArchivoXml.Text) || lblArchivoXml.Text == "Ningún archivo seleccionado")
            {
                System.Windows.MessageBox.Show("Selecciona un archivo XML primero.", "Atención");
                return;
            }
            lblResultadoValidacion.Text = "Validando... (funcionalidad en desarrollo)";
        }

        private void btnIniciarSesion_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow loginWindow = new LoginWindow();
            
            var credenciales = CredentialManager.LoadCredentials();
            if (credenciales.HasValue)
            {
                var (rutaCer, rutaKey, password) = credenciales.Value;
                if (System.IO.File.Exists(rutaCer) && System.IO.File.Exists(rutaKey))
                {
                    loginWindow.CargarCredencialesGuardadas(rutaCer, rutaKey, password);
                }
            }

            loginWindow.ShowDialog();

            if (loginWindow.DialogResult == true)
            {
                SessionInfo = new SessionInfo
                {
                    RFC = loginWindow.RfcValidado,
                    Certificado = loginWindow.CertificadoValidado,
                    LlavePrivada = loginWindow.LlavePrivadaValidada
                };

                Title = $"VUCEM Suite - {SessionInfo.RFC}";
                lblSesion.Text = $"RFC: {SessionInfo.RFC}";
                lblInfoCert.Text = SessionInfo.Certificado?.Subject;
                lblVigencia.Text = $"{SessionInfo.Certificado?.NotBefore:dd/MM/yyyy} - {SessionInfo.Certificado?.NotAfter:dd/MM/yyyy}";
                
                btnIniciarSesion.Visibility = Visibility.Collapsed;
                btnCerrarSesion.Visibility = Visibility.Visible;
            }
        }

        private void btnCerrarSesion_Click(object sender, RoutedEventArgs e)
        {
            var result = System.Windows.MessageBox.Show(
                "¿Deseas cerrar la sesión de VUCEM?", 
                "Cerrar Sesión", 
                System.Windows.MessageBoxButton.YesNo, 
                System.Windows.MessageBoxImage.Question);
            
            if (result == System.Windows.MessageBoxResult.Yes)
            {
                CredentialManager.ClearCredentials();
                
                SessionInfo = null;
                Title = "VUCEM Suite";
                lblSesion.Text = "Sin sesión activa";
                lblInfoCert.Text = "Certificado: No cargado";
                lblVigencia.Text = "Vigencia: -";
                
                btnIniciarSesion.Visibility = Visibility.Visible;
                btnCerrarSesion.Visibility = Visibility.Collapsed;
            }
        }
    }
}
