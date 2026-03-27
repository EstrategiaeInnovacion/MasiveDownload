using System.Text.Json.Serialization;

namespace VucemDownloader.Models;

public class AppSettings
{
    [JsonPropertyName("carpetaDescarga")]
    public string CarpetaDescarga { get; set; } = string.Empty;

    [JsonPropertyName("estructuraCarpetas")]
    public string EstructuraCarpetas { get; set; } = "{TIPO}/{YYYY}/{MM}_{MMM}";

    [JsonPropertyName("formatoNombreArchivo")]
    public string FormatoNombreArchivo { get; set; } = "{TIPO}_{NUMERO}_{YYYYMMDD}";

    [JsonPropertyName("maxDescargasConcurrentes")]
    public int MaxDescargasConcurrentes { get; set; } = 3;

    [JsonPropertyName("timeoutSegundos")]
    public int TimeoutSegundos { get; set; } = 60;

    [JsonPropertyName("maxReintentos")]
    public int MaxReintentos { get; set; } = 3;

    [JsonPropertyName("descargarPdf")]
    public bool DescargarPdf { get; set; } = true;

    [JsonPropertyName("descargarXml")]
    public bool DescargarXml { get; set; } = true;

    [JsonPropertyName("comprimirEnZip")]
    public bool ComprimirEnZip { get; set; } = false;

    [JsonPropertyName("usarProxy")]
    public bool UsarProxy { get; set; } = false;

    [JsonPropertyName("proxyUrl")]
    public string ProxyUrl { get; set; } = string.Empty;

    [JsonPropertyName("proxyUser")]
    public string ProxyUser { get; set; } = string.Empty;

    [JsonPropertyName("proxyPassword")]
    public string ProxyPassword { get; set; } = string.Empty;

    [JsonPropertyName("abrirCarpetaDespues")]
    public bool AbrirCarpetaDespues { get; set; } = true;

    [JsonPropertyName("notificarCompletado")]
    public bool NotificarCompletado { get; set; } = true;
}
