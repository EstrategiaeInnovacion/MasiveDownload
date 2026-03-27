using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VucemDownloader.Models;

namespace VucemDownloader.Services;

/// <summary>
/// Cliente para el servicio de Manifestación de Valor (MVE/E2) de VUCEM.
/// 
/// Endpoint de Consulta: https://privados.ventanillaunica.gob.mx/ConsultaManifestacionImpl/ConsultaManifestacionService
/// SOAPAction: ""
/// </summary>
public class VucemManifestacionClient : IDisposable
{
    private const string ConsultaEndpoint = "https://privados.ventanillaunica.gob.mx/ConsultaManifestacionImpl/ConsultaManifestacionService";
    
    private const string NS_CONSULTA = "http://ws.consultamanifestacion.manifestacion.www.ventanillaunica.gob.mx";
    private const string NS_SOAP = "http://schemas.xmlsoap.org/soap/envelope/";
    private const string NS_WSSE = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";
    private const string NS_WSU = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd";
    private const string PASSWORD_TYPE = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-username-token-profile-1.0#PasswordText";
    
    private readonly HttpClient _httpClient;
    private string _username;
    private string _password;

    public VucemManifestacionClient(string username, string password)
    {
        _username = username;
        _password = password;
        
        // Ignorar errores de certificado SSL
        _httpClient = new HttpClient(new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        });
        _httpClient.Timeout = TimeSpan.FromMinutes(2);
    }

    public void ActualizarCredenciales(string username, string password)
    {
        _username = username;
        _password = password;
    }

    /// <summary>
    /// Consulta una Manifestación de Valor por su folio alfanumérico (eDocument) o número de operación.
    /// </summary>
    /// <param name="folio">eDocument (ej. "MNVA-2024-...") o numeroOperacion (ej. "123456")</param>
    public async Task<ManifestacionConsultaResult> ConsultarManifestacionPorFolioAsync(string folio)
    {
        try
        {
            var soapEnvelope = GenerarSoapConsultarManifestacion(folio.Trim());
            
            var content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");
            // El SOAPAction para este servicio debe estar vacío según el WSDL
            content.Headers.Add("SOAPAction", "");

            var response = await _httpClient.PostAsync(ConsultaEndpoint, content);
            var responseXml = await response.Content.ReadAsStringAsync();

            return ParsearRespuestaManifestacion(responseXml, folio);
        }
        catch (Exception ex)
        {
            return new ManifestacionConsultaResult
            {
                Exitoso = false,
                Mensaje = "Error de conexión (Manifestación): " + ex.Message
            };
        }
    }

    // Sobrecarga de compatibilidad: VUCEM no soporta búsqueda masiva por fechas vía este WS.
    public Task<ManifestacionConsultaResult> ConsultarManifestacionesAsync(DateTime fechaInicio, DateTime fechaFin, string? rfc = null, string? estado = null)
    {
        return Task.FromResult(new ManifestacionConsultaResult
        {
            Exitoso = false,
            Mensaje = "La búsqueda masiva por rango de fechas no está soportada por el WS de Manifestación. " +
                      "Ingrese el folio eDocument o Número de Operación directamente."
        });
    }

    public async Task<ManifestacionDescargaResult> DescargarPdfManifestacionAsync(string numeroOperacion)
    {
        var resultado = await ConsultarManifestacionPorFolioAsync(numeroOperacion);
        
        if (!resultado.Exitoso || resultado.Manifestaciones.Count == 0)
        {
            return new ManifestacionDescargaResult
            {
                Exitoso = false,
                Mensaje = resultado.Mensaje
            };
        }

        var manif = resultado.Manifestaciones[0];
        
        if (!string.IsNullOrEmpty(manif.AcusePdfBase64))
        {
            try
            {
                var pdfBytes = Convert.FromBase64String(manif.AcusePdfBase64);
                return new ManifestacionDescargaResult
                {
                    Exitoso = true,
                    Contenido = pdfBytes,
                    NombreArchivo = "MANIF_" + numeroOperacion + ".pdf",
                    Mensaje = "PDF extraído exitosamente de la respuesta"
                };
            }
            catch
            {
                return new ManifestacionDescargaResult { Exitoso = false, Mensaje = "Error al decodificar PDF" };
            }
        }

        return new ManifestacionDescargaResult { Exitoso = false, Mensaje = "No se encontró el nodo acusePDF en la respuesta" };
    }

    public async Task<ManifestacionDescargaResult> DescargarXmlManifestacionAsync(string numeroOperacion)
    {
        var resultado = await ConsultarManifestacionPorFolioAsync(numeroOperacion);
        
        if (!resultado.Exitoso || resultado.Manifestaciones.Count == 0)
        {
            return new ManifestacionDescargaResult { Exitoso = false, Mensaje = resultado.Mensaje };
        }

        var manif = resultado.Manifestaciones[0];
        
        if (!string.IsNullOrEmpty(manif.XmlRespuesta))
        {
            return new ManifestacionDescargaResult
            {
                Exitoso = true,
                Contenido = Encoding.UTF8.GetBytes(manif.XmlRespuesta),
                NombreArchivo = "MANIF_" + numeroOperacion + ".xml",
                Mensaje = "XML generado exitosamente"
            };
        }

        return new ManifestacionDescargaResult { Exitoso = false, Mensaje = "No hay XML disponible" };
    }

    private string GenerarSoapConsultarManifestacion(string folio)
    {
        var rfc = _username.ToUpper().Trim();
        var now = DateTime.UtcNow;
        string created = now.ToString("yyyy-MM-ddTHH:mm:ssZ");
        string expires = now.AddMinutes(5).ToString("yyyy-MM-ddTHH:mm:ssZ");

        // Determinar si es numérico (numeroOperacion) o alfanumérico (eDocument)
        bool esNumerico = long.TryParse(folio, out _);
        string datosManifestacionXml = esNumerico
            ? $"<ws:numeroOperacion>{folio}</ws:numeroOperacion>"
            : $"<ws:eDocument>{Esc(folio)}</ws:eDocument>";

        var sb = new StringBuilder();
        sb.AppendLine(@"<?xml version=""1.0"" encoding=""UTF-8""?>");
        sb.AppendLine($@"<soapenv:Envelope xmlns:soapenv=""{NS_SOAP}"" xmlns:ws=""{NS_CONSULTA}"">");
        sb.AppendLine($@"   <soapenv:Header>");
        sb.AppendLine($@"      <wsse:Security soapenv:mustUnderstand=""1"" xmlns:wsse=""{NS_WSSE}"" xmlns:wsu=""{NS_WSU}"">");
        sb.AppendLine($@"         <wsse:UsernameToken wsu:Id=""UsernameToken-1"">");
        sb.AppendLine($@"            <wsse:Username>{Esc(rfc)}</wsse:Username>");
        sb.AppendLine($@"            <wsse:Password Type=""{PASSWORD_TYPE}"">{Esc(_password)}</wsse:Password>");
        sb.AppendLine($@"         </wsse:UsernameToken>");
        sb.AppendLine($@"         <wsu:Timestamp wsu:Id=""Timestamp-1"">");
        sb.AppendLine($@"            <wsu:Created>{created}</wsu:Created>");
        sb.AppendLine($@"            <wsu:Expires>{expires}</wsu:Expires>");
        sb.AppendLine($@"         </wsu:Timestamp>");
        sb.AppendLine($@"      </wsse:Security>");
        sb.AppendLine($@"   </soapenv:Header>");
        sb.AppendLine($@"   <soapenv:Body>");
        sb.AppendLine($@"      <ws:consultaManifestacion>");
        sb.AppendLine($@"         <ws:datosManifestacion>");
        sb.AppendLine($"            {datosManifestacionXml}");
        sb.AppendLine($@"         </ws:datosManifestacion>");
        sb.AppendLine($@"      </ws:consultaManifestacion>");
        sb.AppendLine($@"   </soapenv:Body>");
        sb.AppendLine($@"</soapenv:Envelope>");
        
        return sb.ToString();
    }

    private ManifestacionConsultaResult ParsearRespuestaManifestacion(string xml, string folioConsultado)
    {
        var result = new ManifestacionConsultaResult { Exitoso = false };
        var manifestaciones = new List<ManifestacionInfo>();

        try
        {
            if (xml.Contains("Fault") || xml.Contains("faultstring"))
            {
                result.Mensaje = ExtraerFault(xml);
                return result;
            }

            // El elemento principal de respuesta es <return> según el XSD
            var eDoc = RegexVal(xml, @"<[:\w]*eDocument>(.*?)</[:\w]*eDocument>");
            var status = RegexVal(xml, @"<[:\w]*estatus>(.*?)</[:\w]*estatus>");
            var rfcSol = RegexVal(xml, @"<[:\w]*rfcSolicitante>(.*?)</[:\w]*rfcSolicitante>");
            var fechaRec = RegexVal(xml, @"<[:\w]*fechaYHoraRecepcion>(.*?)</[:\w]*fechaYHoraRecepcion>");
            var acusePdf = RegexVal(xml, @"<[:\w]*acusePDF>(.*?)</[:\w]*acusePDF>");

            // Buscar errores en el nodo <mensaje>
            var errorMsg = RegexVal(xml, @"<[:\w]*descripcionError>(.*?)</[:\w]*descripcionError>");

            if (!string.IsNullOrEmpty(errorMsg))
            {
                result.Mensaje = errorMsg;
                return result;
            }

            if (!string.IsNullOrEmpty(eDoc) || !string.IsNullOrEmpty(status))
            {
                result.Exitoso = true;
                result.Mensaje = "Manifestación consultada correctamente";
                
                var manif = new ManifestacionInfo
                {
                    NumeroOperacion = folioConsultado,
                    NumeroMv = eDoc,
                    FechaCreacion = DateTime.TryParse(fechaRec, out var fd) ? fd : DateTime.Now,
                    RfcSolicitante = rfcSol,
                    Estado = status,
                    TipoOperacion = "E2",
                    AcusePdfBase64 = acusePdf,
                    XmlRespuesta = xml
                };

                // Extraer primer COVE asociado si existe
                var coveAsociado = RegexVal(xml, @"<[:\w]*informacionCove>.*?<[:\w]*cove>(.*?)</[:\w]*cove>");
                if (!string.IsNullOrEmpty(coveAsociado)) manif.Cove = coveAsociado;

                manifestaciones.Add(manif);
            }
            else
            {
                result.Mensaje = $"No se encontró información para el folio '{folioConsultado}'";
            }

            result.Manifestaciones = manifestaciones;
            result.TotalRegistros = manifestaciones.Count;
        }
        catch (Exception ex)
        {
            result.Mensaje = "Error al procesar respuesta Manifestación: " + ex.Message;
        }

        return result;
    }

    private static string Esc(string s) => System.Security.SecurityElement.Escape(s) ?? "";
    
    private static string RegexVal(string xml, string pattern)
    {
        var m = Regex.Match(xml, pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);
        return m.Success ? m.Groups[1].Value.Trim() : "";
    }

    private static string ExtraerFault(string xml)
    {
        var fs = RegexVal(xml, @"<[:\w]*faultstring>(.*?)</[:\w]*faultstring>");
        return string.IsNullOrEmpty(fs) ? "Error de servicio VUCEM" : fs;
    }

    public void Dispose() => _httpClient?.Dispose();
}
