using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using VucemDownloader.Models;

namespace VucemDownloader
{
    public class ArchivoSubir
    {
        public string RutaArchivo { get; set; } = string.Empty;
        public string NombreArchivo { get; set; } = string.Empty;
        public string Tamano { get; set; } = string.Empty;
        public KeyValuePair<string, string>? TipoSeleccionado { get; set; }
        public ObservableCollection<KeyValuePair<string, string>> TiposDisponibles { get; set; } = new();
    }

    public partial class SubirArchivosWindow : Window
    {
        public ObservableCollection<ArchivoSubir> Archivos { get; set; } = new();
        
        public SubirArchivosWindow(string[] rutasArchivos)
        {
            InitializeComponent();
            Owner = Application.Current.MainWindow;

            var tiposUsados = new HashSet<string>();

            foreach (var ruta in rutasArchivos)
            {
                var archivo = new ArchivoSubir
                {
                    RutaArchivo = ruta,
                    NombreArchivo = Path.GetFileName(ruta),
                    Tamano = FormatFileSize(new FileInfo(ruta).Length)
                };

                var tipoDetectado = DetectarTipoDocumento(ruta);
                
                foreach (var tipo in TiposDocumento.Catalogo)
                {
                    if (!tiposUsados.Contains(tipo.Key) || tipo.Key == tipoDetectado)
                    {
                        archivo.TiposDisponibles.Add(tipo);
                    }
                }

                var tipoInicial = TiposDocumento.Catalogo.FirstOrDefault(t => t.Key == tipoDetectado);
                if (tipoInicial.Key != null && !tiposUsados.Contains(tipoInicial.Key))
                {
                    archivo.TipoSeleccionado = tipoInicial;
                    tiposUsados.Add(tipoInicial.Key);
                }
                else
                {
                    archivo.TipoSeleccionado = archivo.TiposDisponibles.FirstOrDefault();
                }

                Archivos.Add(archivo);
            }

            lstArchivos.ItemsSource = Archivos;
        }

        private string DetectarTipoDocumento(string rutaArchivo)
        {
            var nombre = Path.GetFileNameWithoutExtension(rutaArchivo).ToLower();
            var nombreCompleto = nombre;
            
            if (nombreCompleto.Contains("pedimento") || nombreCompleto.Contains("pagado") || nombreCompleto.Contains("detalle") || nombre.Contains("ped"))
                return "PED";
            if (nombreCompleto.Contains("simplificado") || nombreCompleto.Contains("simplif"))
                return "SIM";
            if (nombreCompleto.Contains("factura") || nombreCompleto.Contains("proveed") || nombre.Contains("fac"))
                return "FAC";
            if (nombreCompleto.Contains("packing") || nombreCompleto.Contains("empaque") || nombre.Contains("pkl"))
                return "PKL";
            if (nombreCompleto.Contains("guia") || nombreCompleto.Contains("hbl") || nombreCompleto.Contains("mbl") || nombreCompleto.Contains("awb"))
                return "GUIA";
            if ((nombreCompleto.Contains("certificado") || nombreCompleto.Contains("origen")) && !nombreCompleto.Contains("cove"))
                return "CO";
            if (nombreCompleto.Contains("cove"))
                return "COV";
            if (nombreCompleto.Contains("doda") || nombreCompleto.Contains("dod"))
                return "DOD";
            if (nombreCompleto.Contains("pita"))
                return "PITA";
            if (nombreCompleto.Contains("aviso") || nombreCompleto.Contains("cruce"))
                return "AVC";
            if (nombreCompleto.Contains("edoc") || nombreCompleto.Contains("edocument") || nombreCompleto.Contains("e-doc"))
                return "ED";
            
            return "PED";
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            int order = 0;
            double size = bytes;
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }
            return $"{size:0.##} {sizes[order]}";
        }

        private void btnSubir_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void btnCancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}