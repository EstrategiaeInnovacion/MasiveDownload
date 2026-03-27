using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using VucemDownloader.Models;
using VucemDownloader.Services;

namespace VucemDownloader
{
    public partial class MainWindow : Window
    {
        public SessionInfo? SessionInfo { get; set; }
        private ExpedienteService? _expedienteService;

        public ObservableCollection<Expediente> Expedientes { get; set; } = new();
        public ObservableCollection<DocumentoFaltante> DocumentosFaltantes { get; set; } = new();

        private Expediente? _expedienteSeleccionado;

        public MainWindow()
        {
            InitializeComponent();
            
            lstExpedientes.ItemsSource = Expedientes;
            dgDocumentos.ItemsSource = DocumentosFaltantes;

            Closing += (s, e) => Application.Current.Shutdown();

            _expedienteService = new ExpedienteService();
            CargarExpedientes();
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

                if (SessionInfo != null && !string.IsNullOrEmpty(SessionInfo.RFC))
                {
                    lblSesion.Text = "RFC: " + SessionInfo.RFC;
                    ellipseStatus.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#059669"));
                }
            }
        }

        private void CargarExpedientes(string? filtro = null)
        {
            if (_expedienteService == null) return;
            
            var resultado = _expedienteService.ObtenerExpedientes(filtro);
            Expedientes.Clear();
            foreach (var exp in resultado.Expedientes)
            {
                Expedientes.Add(exp);
            }
        }

        private void btnNuevoExpediente_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new NewExpedienteWindow { Owner = this };
            
            if (dialog.ShowDialog() == true)
            {
                var result = _expedienteService!.CrearExpediente(dialog.Nombre, dialog.Rfc, dialog.Aduana);
                if (result.Exitoso)
                {
                    CargarExpedientes();
                    MessageBox.Show("Expediente creado correctamente", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(result.Mensaje, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void btnBuscarExpediente_Click(object sender, RoutedEventArgs e)
        {
            CargarExpedientes(txtBusquedaExpediente.Text);
        }

        private void txtBusquedaExpediente_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                CargarExpedientes(txtBusquedaExpediente.Text);
            }
        }

        private void lstExpedientes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstExpedientes.SelectedItem is Expediente expediente)
            {
                _expedienteSeleccionado = expediente;
                CargarDocumentosExpediente(expediente.Id);
                panelInfoExpediente.Visibility = Visibility.Visible;
                panelProgreso.Visibility = Visibility.Visible;
                panelDropZone.Visibility = Visibility.Visible;
            }
            else
            {
                _expedienteSeleccionado = null;
                panelInfoExpediente.Visibility = Visibility.Collapsed;
                panelProgreso.Visibility = Visibility.Collapsed;
                panelDropZone.Visibility = Visibility.Collapsed;
                DocumentosFaltantes.Clear();
            }
        }

        private void CargarDocumentosExpediente(int expedienteId)
        {
            if (_expedienteService == null) return;

            try
            {
                var result = _expedienteService.ObtenerExpedienteCompleto(expedienteId);
                if (result.Exitoso && result.Expediente != null)
                {
                    lblNombreExpediente.Text = result.Expediente.Nombre;
                    lblRfcExpediente.Text = "RFC: " + result.Expediente.Rfc;
                    lblAduanaExpediente.Text = "Aduana: " + (string.IsNullOrEmpty(result.Expediente.Aduana) ? "N/A" : result.Expediente.Aduana);

                    progressExpediente.Value = result.PorcentajeCompletado;
                    lblProgresoExpediente.Text = $"{result.DocumentosSubidos}/{result.TotalDocumentos}";

                    DocumentosFaltantes.Clear();
                    foreach (var doc in result.Documentos)
                    {
                        DocumentosFaltantes.Add(doc);
                    }
                    
                    lblCountDocumentos.Text = result.Documentos.Count(d => d.TieneDocumento).ToString();
                }
                else
                {
                    MessageBox.Show("Error al cargar documentos: " + result.Mensaje, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar documentos: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnSubirArchivos_Click(object sender, RoutedEventArgs e)
        {
            if (_expedienteSeleccionado == null)
            {
                MessageBox.Show("Seleccione un expediente primero", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new OpenFileDialog
            {
                Multiselect = true,
                Title = "Seleccionar Archivos",
                Filter = "Archivos permitidos (*.pdf;*.xml;*.jpg;*.png)|*.pdf;*.xml;*.jpg;*.png|PDF (*.pdf)|*.pdf|XML (*.xml)|*.xml|Imagenes (*.jpg;*.png)|*.jpg;*.png"
            };

            if (dialog.ShowDialog() == true && dialog.FileNames.Length > 0)
            {
                var dialogo = new SubirArchivosWindow(dialog.FileNames) { Owner = this };
                if (dialogo.ShowDialog() == true)
                {
                    SubirArchivos(dialogo.Archivos);
                }
            }
        }

        private void SubirArchivos(ObservableCollection<ArchivoSubir> archivos)
        {
            if (_expedienteSeleccionado == null || _expedienteService == null) return;

            MostrarProgreso("Subiendo archivos...");
            
            int exitosos = 0;
            foreach (var archivo in archivos)
            {
                if (archivo.TipoSeleccionado == null || string.IsNullOrEmpty(archivo.TipoSeleccionado.Value.Key))
                {
                    MessageBox.Show($"Seleccione un tipo de documento para: {archivo.NombreArchivo}", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                    continue;
                }

                var result = _expedienteService.SubirDocumento(_expedienteSeleccionado.Id, archivo.RutaArchivo, archivo.TipoSeleccionado.Value.Key);
                if (result.Exitoso)
                {
                    exitosos++;
                }
                else
                {
                    MessageBox.Show($"Error al subir {archivo.NombreArchivo}: {result.Mensaje}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }

            OcultarProgreso();
            
            if (exitosos > 0)
            {
                MessageBox.Show($"Se guardaron {exitosos} archivo(s) correctamente", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            
            CargarDocumentosExpediente(_expedienteSeleccionado.Id);
            CargarExpedientes();
        }

        private void panelDropZone_Drop(object sender, DragEventArgs e)
        {
            panelDropZone.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f0fdf4"));
            panelDropZone.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#86efac"));

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var archivos = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (archivos != null && archivos.Length > 0)
                {
                    var dialogo = new SubirArchivosWindow(archivos) { Owner = this };
                    if (dialogo.ShowDialog() == true)
                    {
                        SubirArchivos(dialogo.Archivos);
                    }
                }
            }
        }

        private void panelDropZone_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
            panelDropZone.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#dcfce7"));
            panelDropZone.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#22c55e"));
            e.Handled = true;
        }

        private void panelDropZone_DragLeave(object sender, DragEventArgs e)
        {
            panelDropZone.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f0fdf4"));
            panelDropZone.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#86efac"));
        }

        private void btnEliminarDocumento_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is DocumentoFaltante doc)
            {
                if (_expedienteSeleccionado == null || _expedienteService == null) return;

                var resultado = MessageBox.Show($"Eliminar documento {doc.TipoDocumento}?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (resultado == MessageBoxResult.Yes)
                {
                    var result = _expedienteService.EliminarDocumento(_expedienteSeleccionado.Id, doc.TipoDocumento);
                    if (result.Exitoso)
                    {
                        CargarDocumentosExpediente(_expedienteSeleccionado.Id);
                        CargarExpedientes();
                    }
                    else
                    {
                        MessageBox.Show(result.Mensaje, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void btnGenerarZip_Click(object sender, RoutedEventArgs e)
        {
            if (_expedienteSeleccionado == null)
            {
                MessageBox.Show("Seleccione un expediente primero", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new SaveFileDialog
            {
                Title = "Guardar Expediente ZIP",
                Filter = "ZIP (*.zip)|*.zip",
                FileName = $"Expediente_{_expedienteSeleccionado.Nombre}_{DateTime.Now:yyyyMMdd}.zip"
            };

            if (dialog.ShowDialog() == true)
            {
                MostrarProgreso("Generando ZIP...");
                
                var carpeta = System.IO.Path.GetDirectoryName(dialog.FileName);
                var (exitoso, mensaje, rutaZip) = _expedienteService!.GenerarZip(_expedienteSeleccionado.Id, carpeta);
                
                OcultarProgreso();

                if (exitoso)
                {
                    MessageBox.Show($"Expediente compresso correctamente.\n\nUbicacion: {rutaZip}", "Exito", MessageBoxButton.OK, MessageBoxImage.Information);
                    CargarDocumentosExpediente(_expedienteSeleccionado.Id);
                    CargarExpedientes();
                }
                else
                {
                    MessageBox.Show(mensaje, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void btnEliminarExpediente_Click(object sender, RoutedEventArgs e)
        {
            if (_expedienteSeleccionado == null) return;

            var resultado = MessageBox.Show($"Eliminar el expediente '{_expedienteSeleccionado.Nombre}'?\n\nEsta accion eliminara todos los documentos asociados.", 
                "Confirmar Eliminacion", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            
            if (resultado == MessageBoxResult.Yes)
            {
                var (exitoso, mensaje) = _expedienteService!.EliminarExpediente(_expedienteSeleccionado.Id);
                if (exitoso)
                {
                    _expedienteSeleccionado = null;
                    DocumentosFaltantes.Clear();
                    panelInfoExpediente.Visibility = Visibility.Collapsed;
                    panelProgreso.Visibility = Visibility.Collapsed;
                    panelDropZone.Visibility = Visibility.Collapsed;
                    CargarExpedientes();
                    MessageBox.Show("Expediente eliminado", "Exito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(mensaje, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
