using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using VucemDownloader.Models;
using VucemDownloader.Services;
using VucemDownloader.Helpers;

namespace VucemDownloader
{
    public partial class MainWindow : Window
    {
        public SessionInfo? SessionInfo { get; set; }
        private VucemCoveClient? _coveClient;
        private VucemManifestacionClient? _manifestacionClient;
        private VucemPedimentoClient? _pedimentoClient;

        public ObservableCollection<CoveInfoViewModel> Coves { get; set; } = new();
        public ObservableCollection<ManifestacionInfoViewModel> Manifestaciones { get; set; } = new();
        public ObservableCollection<PedimentoInfoViewModel> Pedimentos { get; set; } = new();

        public MainWindow()
        {
            InitializeComponent();
            
            InicializarFechas();
            CargarInformacionLicencia();
            
            dgCoves.ItemsSource = Coves;
            dgManifestaciones.ItemsSource = Manifestaciones;
            dgPedimentos.ItemsSource = Pedimentos;
        }

        private void InicializarFechas()
        {
            DateTime hoy = DateTime.Today;
            dtpCoveFechaInicio.SelectedDate = hoy.AddMonths(-1);
            dtpCoveFechaFin.SelectedDate = hoy;
            dtpManifFechaInicio.SelectedDate = hoy.AddMonths(-1);
            dtpManifFechaFin.SelectedDate = hoy;
            dtpPedFechaInicio.SelectedDate = hoy.AddMonths(-1);
            dtpPedFechaFin.SelectedDate = hoy;
        }

        private void CargarInformacionLicencia()
        {
            var (tieneLicencia, vencimiento) = LicenseWindow.CargarLicenciaGuardada();
            
            if (tieneLicencia && vencimiento.HasValue)
            {
                lblReporteLicencia.Text = "Activada - Vence: " + vencimiento.Value.ToString("dd/MM/yyyy");
            }
            else
            {
                lblReporteLicencia.Text = "No activada";
            }
        }

        private void ActualizarEstadoSesion()
        {
            if (SessionInfo != null && !string.IsNullOrEmpty(SessionInfo.RFC))
            {
                lblSesion.Text = "RFC: " + SessionInfo.RFC;
                lblReporteRfc.Text = SessionInfo.RFC;
                lblReporteCert.Text = SessionInfo.Certificado?.Subject ?? "No disponible";
                ellipseStatus.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#059669"));

                _coveClient = new VucemCoveClient(SessionInfo.RFC, SessionInfo.WebservicePassword);
                _manifestacionClient = new VucemManifestacionClient(SessionInfo.RFC, SessionInfo.WebservicePassword);
                _pedimentoClient = new VucemPedimentoClient(SessionInfo.RFC, SessionInfo.WebservicePassword);
            }
            else
            {
                lblSesion.Text = "Sin sesión activa";
                lblReporteRfc.Text = "No disponible";
                lblReporteCert.Text = "No disponible";
                ellipseStatus.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#dc2626"));
            }
        }

        private void MostrarProgreso(string mensaje)
        {
            lblProgreso.Text = mensaje;
            progressOverlay.Visibility = Visibility.Visible;
        }

        private void OcultarProgreso()
        {
            progressOverlay.Visibility = Visibility.Collapsed;
        }

        private async void btnBuscarCoves_Click(object sender, RoutedEventArgs e)
        {
            if (!_ValidarSesion()) return;

            string folio = txtCoveFolio.Text.Trim();
            
            MostrarProgreso("Buscando COVEs...");

            try
            {
                CoveConsultaResult resultado;
                
                if (!string.IsNullOrEmpty(folio))
                {
                    // Búsqueda directa por folio (eDocument)
                    resultado = await _coveClient!.ConsultarCovePorFolioAsync(folio);
                }
                else
                {
                    // Búsqueda por fechas
                    DateTime? fechaInicio = dtpCoveFechaInicio.SelectedDate;
                    DateTime? fechaFin = dtpCoveFechaFin.SelectedDate;

                    if (!fechaInicio.HasValue || !fechaFin.HasValue)
                    {
                        MessageBox.Show("Seleccione el rango de fechas o ingrese un folio.", "Atención", MessageBoxButton.OK, MessageBoxImage.Warning);
                        OcultarProgreso();
                        return;
                    }

                    resultado = await _coveClient!.ConsultarCovesAsync(
                        fechaInicio.Value, fechaFin.Value,
                        txtCoveRfcEmisor.Text,
                        (cmbCoveEstado.SelectedItem as ComboBoxItem)?.Content?.ToString());
                }

                if (resultado.Exitoso)
                {
                    Coves.Clear();
                    foreach (var cove in resultado.Covess)
                    {
                        Coves.Add(new CoveInfoViewModel(cove));
                    }
                    lblTotalCoves.Text = resultado.TotalRegistros.ToString();
                }
                else
                {
                    MessageBox.Show(resultado.Mensaje, "Información", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                OcultarProgreso();
            }
        }

        private async void btnDescargarCovePdf_Click(object sender, RoutedEventArgs e)
        {
            var seleccionados = ObtenerCovesSeleccionados();
            if (seleccionados.Count == 0)
            {
                MessageBox.Show("Seleccione al menos un COVE.", "Atención", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            await DescargarDocumentos(seleccionados, "COVE", true, false);
        }

        private async void btnDescargarCoveXml_Click(object sender, RoutedEventArgs e)
        {
            var seleccionados = ObtenerCovesSeleccionados();
            if (seleccionados.Count == 0)
            {
                MessageBox.Show("Seleccione al menos un COVE.", "Atención", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            await DescargarDocumentos(seleccionados, "COVE", false, true);
        }

        private void btnVerDetalleCove_Click(object sender, RoutedEventArgs e)
        {
            var seleccionados = ObtenerCovesSeleccionados();
            if (seleccionados.Count == 0)
            {
                MessageBox.Show("Seleccione un COVE.", "Atención", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var cove = seleccionados[0];
            MessageBox.Show(
                "No. Operación: " + cove.NumeroOperacion + "\n" +
                "Fecha: " + cove.FechaCreacion.ToString("dd/MM/yyyy") + "\n" +
                "RFC Emisor: " + cove.RfcEmisor + "\n" +
                "RFC Receptor: " + cove.RfcReceptor + "\n" +
                "Tipo: " + cove.TipoOperacion + "\n" +
                "Estado: " + cove.Estado + "\n" +
                "Valor: " + cove.ValorTotal.ToString("C2") + " " + cove.Moneda,
                "Detalle COVE", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void btnBuscarManifestaciones_Click(object sender, RoutedEventArgs e)
        {
            if (!_ValidarSesion()) return;

            string folio = txtManifFolio.Text.Trim();
            MostrarProgreso("Buscando Manifestaciones...");

            try
            {
                ManifestacionConsultaResult resultado;

                if (!string.IsNullOrEmpty(folio))
                {
                    // Búsqueda directa por folio (eDocument o Operación)
                    resultado = await _manifestacionClient!.ConsultarManifestacionPorFolioAsync(folio);
                }
                else
                {
                    // Búsqueda por fechas
                    DateTime? fechaInicio = dtpManifFechaInicio.SelectedDate;
                    DateTime? fechaFin = dtpManifFechaFin.SelectedDate;

                    if (!fechaInicio.HasValue || !fechaFin.HasValue)
                    {
                        MessageBox.Show("Seleccione el rango de fechas o ingrese un folio.", "Atención", MessageBoxButton.OK, MessageBoxImage.Warning);
                        OcultarProgreso();
                        return;
                    }

                    resultado = await _manifestacionClient!.ConsultarManifestacionesAsync(
                        fechaInicio.Value, fechaFin.Value,
                        txtManifRfc.Text,
                        (cmbManifEstado.SelectedItem as ComboBoxItem)?.Content?.ToString());
                }

                if (resultado.Exitoso)
                {
                    Manifestaciones.Clear();
                    foreach (var manif in resultado.Manifestaciones)
                    {
                        Manifestaciones.Add(new ManifestacionInfoViewModel(manif));
                    }
                    lblTotalManifestaciones.Text = resultado.TotalRegistros.ToString();
                }
                else
                {
                    MessageBox.Show(resultado.Mensaje, "Información", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                OcultarProgreso();
            }
        }

        private async void btnDescargarManifPdf_Click(object sender, RoutedEventArgs e)
        {
            var seleccionados = ObtenerManifestacionesSeleccionadas();
            if (seleccionados.Count == 0)
            {
                MessageBox.Show("Seleccione al menos una manifestación.", "Atención", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            await DescargarDocumentosManif(seleccionados, true, false);
        }

        private async void btnDescargarManifXml_Click(object sender, RoutedEventArgs e)
        {
            var seleccionados = ObtenerManifestacionesSeleccionadas();
            if (seleccionados.Count == 0)
            {
                MessageBox.Show("Seleccione al menos una manifestación.", "Atención", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            await DescargarDocumentosManif(seleccionados, false, true);
        }

        private void btnVerDetalleManif_Click(object sender, RoutedEventArgs e)
        {
            var seleccionados = ObtenerManifestacionesSeleccionadas();
            if (seleccionados.Count == 0)
            {
                MessageBox.Show("Seleccione una manifestación.", "Atención", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var manif = seleccionados[0];
            MessageBox.Show(
                "No. Operación: " + manif.NumeroOperacion + "\n" +
                "Fecha: " + manif.FechaCreacion.ToString("dd/MM/yyyy") + "\n" +
                "RFC Solicitante: " + manif.RfcSolicitante + "\n" +
                "Tipo: " + manif.TipoOperacion + "\n" +
                "No. Pedimento: " + manif.NumeroPedimento + "\n" +
                "Estado: " + manif.Estado + "\n" +
                "Valor Total: " + manif.ValorTotal.ToString("C2"),
                "Detalle Manifestación", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void btnBuscarPedimentos_Click(object sender, RoutedEventArgs e)
        {
            if (!_ValidarSesion()) return;

            DateTime? fechaInicio = dtpPedFechaInicio.SelectedDate;
            DateTime? fechaFin = dtpPedFechaFin.SelectedDate;

            if (!fechaInicio.HasValue || !fechaFin.HasValue)
            {
                MessageBox.Show("Seleccione el rango de fechas.", "Atención", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MostrarProgreso("Buscando Pedimentos...");

            try
            {
                var resultado = await _pedimentoClient!.ConsultarPedimentosAsync(
                    fechaInicio.Value, fechaFin.Value,
                    txtPedRfc.Text,
                    (cmbPedAduana.SelectedItem as ComboBoxItem)?.Content?.ToString());

                if (resultado.Exitoso)
                {
                    Pedimentos.Clear();
                    foreach (var ped in resultado.Pedimentos)
                    {
                        Pedimentos.Add(new PedimentoInfoViewModel(ped));
                    }
                    lblTotalPedimentos.Text = resultado.TotalRegistros.ToString();
                }
                else
                {
                    MessageBox.Show(resultado.Mensaje, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                OcultarProgreso();
            }
        }

        private async void btnDescargarPedimentoPdf_Click(object sender, RoutedEventArgs e)
        {
            var seleccionados = ObtenerPedimentosSeleccionados();
            if (seleccionados.Count == 0)
            {
                MessageBox.Show("Seleccione al menos un pedimento.", "Atención", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            await DescargarDocumentosPed(seleccionados);
        }

        private void btnVerDetallePedimento_Click(object sender, RoutedEventArgs e)
        {
            var seleccionados = ObtenerPedimentosSeleccionados();
            if (seleccionados.Count == 0)
            {
                MessageBox.Show("Seleccione un pedimento.", "Atención", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var ped = seleccionados[0];
            MessageBox.Show(
                "No. Pedimento: " + ped.NumeroPedimento + "\n" +
                "Fecha Pago: " + ped.FechaPago.ToString("dd/MM/yyyy") + "\n" +
                "RFC Importador: " + ped.RfcImportador + "\n" +
                "Aduana: " + ped.Aduana + "\n" +
                "Tipo: " + ped.TipoOperacion + "\n" +
                "Estado: " + ped.Estado + "\n" +
                "Valor Aduana: " + ped.ValorAduana.ToString("C2"),
                "Detalle Pedimento", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void btnConfiguracion_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow loginWindow = new LoginWindow();
            
            var credenciales = CredentialManager.LoadCredentials();
            if (credenciales.HasValue)
            {
                var (rutaCer, rutaKey, password, wsPassword) = credenciales.Value;
                if (System.IO.File.Exists(rutaCer) && System.IO.File.Exists(rutaKey))
                {
                    loginWindow.CargarCredencialesGuardadas(rutaCer, rutaKey, password, wsPassword);
                }
            }

            loginWindow.ShowDialog();

            if (loginWindow.DialogResult == true)
            {
                SessionInfo = new SessionInfo
                {
                    RFC = loginWindow.RfcValidado,
                    Certificado = loginWindow.CertificadoValidado,
                    LlavePrivada = loginWindow.LlavePrivadaValidada,
                    WebservicePassword = loginWindow.WebservicePassword
                };

                ActualizarEstadoSesion();
            }
        }

        private bool _ValidarSesion()
        {
            if (SessionInfo == null || string.IsNullOrEmpty(SessionInfo.RFC))
            {
                MessageBox.Show("Inicie sesión primero.", "Atención", MessageBoxButton.OK, MessageBoxImage.Warning);
                btnConfiguracion_Click(null!, null!);
                return false;
            }
            return true;
        }

        private System.Collections.Generic.List<CoveInfo> ObtenerCovesSeleccionados()
        {
            var seleccionados = new System.Collections.Generic.List<CoveInfo>();
            foreach (var item in Coves)
            {
                if (item.Seleccionado)
                {
                    seleccionados.Add(item.GetModel());
                }
            }
            return seleccionados;
        }

        private System.Collections.Generic.List<ManifestacionInfo> ObtenerManifestacionesSeleccionadas()
        {
            var seleccionados = new System.Collections.Generic.List<ManifestacionInfo>();
            foreach (var item in Manifestaciones)
            {
                if (item.Seleccionado)
                {
                    seleccionados.Add(item.GetModel());
                }
            }
            return seleccionados;
        }

        private System.Collections.Generic.List<PedimentoInfo> ObtenerPedimentosSeleccionados()
        {
            var seleccionados = new System.Collections.Generic.List<PedimentoInfo>();
            foreach (var item in Pedimentos)
            {
                if (item.Seleccionado)
                {
                    seleccionados.Add(item.GetModel());
                }
            }
            return seleccionados;
        }

        private async Task DescargarDocumentos(System.Collections.Generic.List<CoveInfo> items, string tipo, bool pdf, bool xml)
        {
            string carpeta = FileHelper.ObtenerCarpetaDescargas();
            carpeta = System.IO.Path.Combine(carpeta, tipo);
            int descargados = 0;

            MostrarProgreso("Descargando documentos...");

            foreach (var item in items)
            {
                try
                {
                    if (pdf || xml)
                    {
                        var resultado = await _coveClient!.DescargarAcuseCoveAsync(item.NumeroOperacion);
                        if (resultado.Exitoso && resultado.Contenido != null)
                        {
                            string ext = "pdf";
                            string ruta = FileHelper.GenerarRutaArchivo(carpeta, tipo, item.NumeroOperacion, ext, item.FechaCreacion);
                            FileHelper.GuardarArchivo(resultado.Contenido, ruta);
                            descargados++;
                        }
                    }
                }
                catch { }
            }

            OcultarProgreso();
            MessageBox.Show("Se descargaron " + descargados + " archivos.", "Descarga Completada", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async Task DescargarDocumentosManif(System.Collections.Generic.List<ManifestacionInfo> items, bool pdf, bool xml)
        {
            string carpeta = FileHelper.ObtenerCarpetaDescargas();
            carpeta = System.IO.Path.Combine(carpeta, "MANIF");
            int descargados = 0;

            MostrarProgreso("Descargando documentos...");

            foreach (var item in items)
            {
                try
                {
                    if (pdf)
                    {
                        var resultado = await _manifestacionClient!.DescargarPdfManifestacionAsync(item.NumeroOperacion);
                        if (resultado.Exitoso && resultado.Contenido != null)
                        {
                            string ruta = FileHelper.GenerarRutaArchivo(carpeta, "MANIF", item.NumeroOperacion, "pdf", item.FechaCreacion);
                            FileHelper.GuardarArchivo(resultado.Contenido, ruta);
                            descargados++;
                        }
                    }

                    if (xml)
                    {
                        var resultado = await _manifestacionClient!.DescargarXmlManifestacionAsync(item.NumeroOperacion);
                        if (resultado.Exitoso && resultado.Contenido != null)
                        {
                            string ruta = FileHelper.GenerarRutaArchivo(carpeta, "MANIF", item.NumeroOperacion, "xml", item.FechaCreacion);
                            FileHelper.GuardarArchivo(resultado.Contenido, ruta);
                            descargados++;
                        }
                    }
                }
                catch { }
            }

            OcultarProgreso();
            MessageBox.Show($"Se descargaron {descargados} archivos.", "Descarga Completada", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async Task DescargarDocumentosPed(System.Collections.Generic.List<PedimentoInfo> items)
        {
            string carpeta = FileHelper.ObtenerCarpetaDescargas();
            carpeta = System.IO.Path.Combine(carpeta, "PED");
            int descargados = 0;

            MostrarProgreso("Descargando pedimentos...");

            foreach (var item in items)
            {
                try
                {
                    var resultado = await _pedimentoClient!.DescargarPdfPedimentoAsync(item.NumeroPedimento);
                    if (resultado.Exitoso && resultado.Contenido != null)
                    {
                        string ruta = FileHelper.GenerarRutaArchivo(carpeta, "PED", item.NumeroPedimento, "pdf", item.FechaPago);
                        FileHelper.GuardarArchivo(resultado.Contenido, ruta);
                        descargados++;
                    }
                }
                catch { }
            }

            OcultarProgreso();
            MessageBox.Show($"Se descargaron {descargados} archivos.", "Descarga Completada", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    public class CoveInfoViewModel
    {
        public bool Seleccionado { get; set; }
        public string NumeroOperacion { get; set; }
        public DateTime FechaCreacion { get; set; }
        public string RfcEmisor { get; set; }
        public string RfcReceptor { get; set; }
        public string Estado { get; set; }
        public string TipoOperacion { get; set; }
        public decimal ValorTotal { get; set; }
        public string Moneda { get; set; }

        public CoveInfoViewModel(CoveInfo model)
        {
            Seleccionado = false;
            NumeroOperacion = model.NumeroOperacion;
            FechaCreacion = model.FechaCreacion;
            RfcEmisor = model.RfcEmisor;
            RfcReceptor = model.RfcReceptor;
            Estado = model.Estado;
            TipoOperacion = model.TipoOperacion;
            ValorTotal = model.ValorTotal;
            Moneda = model.Moneda;
        }

        public CoveInfo GetModel() => new CoveInfo
        {
            NumeroOperacion = NumeroOperacion,
            FechaCreacion = FechaCreacion,
            RfcEmisor = RfcEmisor,
            RfcReceptor = RfcReceptor,
            Estado = Estado,
            TipoOperacion = TipoOperacion,
            ValorTotal = ValorTotal,
            Moneda = Moneda
        };
    }

    public class ManifestacionInfoViewModel
    {
        public bool Seleccionado { get; set; }
        public string NumeroOperacion { get; set; }
        public DateTime FechaCreacion { get; set; }
        public string RfcSolicitante { get; set; }
        public string Estado { get; set; }
        public string TipoOperacion { get; set; }
        public string NumeroPedimento { get; set; }
        public decimal ValorTotal { get; set; }

        public ManifestacionInfoViewModel(ManifestacionInfo model)
        {
            Seleccionado = false;
            NumeroOperacion = model.NumeroOperacion;
            FechaCreacion = model.FechaCreacion;
            RfcSolicitante = model.RfcSolicitante;
            Estado = model.Estado;
            TipoOperacion = model.TipoOperacion;
            NumeroPedimento = model.NumeroPedimento;
            ValorTotal = model.ValorTotal;
        }

        public ManifestacionInfo GetModel() => new ManifestacionInfo
        {
            NumeroOperacion = NumeroOperacion,
            FechaCreacion = FechaCreacion,
            RfcSolicitante = RfcSolicitante,
            Estado = Estado,
            TipoOperacion = TipoOperacion,
            NumeroPedimento = NumeroPedimento,
            ValorTotal = ValorTotal
        };
    }

    public class PedimentoInfoViewModel
    {
        public bool Seleccionado { get; set; }
        public string NumeroPedimento { get; set; }
        public DateTime FechaPago { get; set; }
        public string RfcImportador { get; set; }
        public string Aduana { get; set; }
        public string Estado { get; set; }
        public decimal ValorAduana { get; set; }
        public string TipoOperacion { get; set; }

        public PedimentoInfoViewModel(PedimentoInfo model)
        {
            Seleccionado = false;
            NumeroPedimento = model.NumeroPedimento;
            FechaPago = model.FechaPago;
            RfcImportador = model.RfcImportador;
            Aduana = model.Aduana;
            Estado = model.Estado;
            ValorAduana = model.ValorAduana;
            TipoOperacion = model.TipoOperacion;
        }

        public PedimentoInfo GetModel() => new PedimentoInfo
        {
            NumeroPedimento = NumeroPedimento,
            FechaPago = FechaPago,
            RfcImportador = RfcImportador,
            Aduana = Aduana,
            Estado = Estado,
            ValorAduana = ValorAduana,
            TipoOperacion = TipoOperacion
        };
    }
}
