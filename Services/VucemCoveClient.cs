using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VucemDownloader.Models;

namespace VucemDownloader.Services;

/// <summary>
/// Cliente para los servicios VUCEM relacionados con COVEs (edocuments).
///
/// Servicios implementados:
///   1. ConsultarEdocument  → busca un COVE por su folio eDocument (p.ej. "MNVA-2024-001")
///      Endpoint : https://www.ventanillaunica.gob.mx/ventanilla/ConsultarEdocumentService
///      SOAPAction: http://www.ventanillaunica.gob.mx/cove/ws/service/ConsultarEdocument
///
///   2. ConsultarRespuestaCove → obtiene el resultado de una operación por su numeroOperacion (int)
///      Endpoint : https://www.ventanillaunica.gob.mx:8110/ventanilla/ConsultarRespuestaCoveService
///      SOAPAction: http://www.ventanillaunica.gob.mx/ConsultarRespuestaCove
///
///   3. ConsultaAcuses (PDF) → descarga el acuse PDF de un COVE
///      Endpoint : https://www.ventanillaunica.gob.mx/ventanilla-acuses-HA/ConsultaAcusesServiceWS
///      SOAPAction: http://www.ventanillaunica.gob.mx/consulta/acuses/ConsultaAcuses
/// </summary>
public class VucemCoveClient : IDisposable
{
    // ───────────────────────────── Endpoints ─────────────────────────────
    private const string ConsultarEdocumentUrl =
        "https://www.ventanillaunica.gob.mx/ventanilla/ConsultarEdocumentService";

    private const string ConsultarRespuestaCoveUrl =
        "https://www.ventanillaunica.gob.mx:8110/ventanilla/ConsultarRespuestaCoveService";

    private const string ConsultaAcusesUrl =
        "https://www.ventanillaunica.gob.mx/ventanilla-acuses-HA/ConsultaAcusesServiceWS";

    // ─────────────────────────── SOAPActions ─────────────────────────────
    private const string SoapActionConsultarEdocument =
        "http://www.ventanillaunica.gob.mx/cove/ws/service/ConsultarEdocument";

    private const string SoapActionConsultarRespuestaCove =
        "http://www.ventanillaunica.gob.mx/ConsultarRespuestaCove";

    private const string SoapActionConsultaAcuses =
        "http://www.ventanillaunica.gob.mx/consulta/acuses/ConsultaAcuses";

    // ──────────────────────────── Namespaces ─────────────────────────────
    private const string NS_SOAP  = "http://schemas.xmlsoap.org/soap/envelope/";
    private const string NS_WSSE  = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";
    private const string NS_WSU   = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd";
    private const string NS_EDOC  = "http://www.ventanillaunica.gob.mx/ConsultarEdocument/";
    private const string NS_COVE_SVC = "http://www.ventanillaunica.gob.mx/cove/ws/service/";
    private const string NS_COVE_OXML = "http://www.ventanillaunica.gob.mx/cove/ws/oxml/";
    private const string NS_ACUSES = "http://www.ventanillaunica.gob.mx/consulta/acuses/oxml";
    private const string PASSWORD_TYPE =
        "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-username-token-profile-1.0#PasswordText";

    private readonly HttpClient _httpClient;
    private string _username;
    private string _password;

    public VucemCoveClient(string username, string password)
    {
        _username = username;
        _password = password;
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromMinutes(2);
        // Ignorar errores de certificado SSL en ambientes de prueba
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

    // ─────────────────────────────────────────────────────────────────────
    // 1. ConsultarEdocument: busca un COVE por folio eDocument
    // ─────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Consulta un COVE por su folio eDocument (p.ej. "2024ABC0001").
    /// Usa el servicio ConsultarEdocument (puerto 443).
    /// </summary>
    public async Task<CoveConsultaResult> ConsultarCovePorFolioAsync(string eDocument)
    {
        try
        {
            var soapEnvelope = GenerarSoapConsultarEdocument(eDocument.Trim());

            var content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");
            content.Headers.Add("SOAPAction", SoapActionConsultarEdocument);

            var response = await _httpClient.PostAsync(ConsultarEdocumentUrl, content);
            var responseXml = await response.Content.ReadAsStringAsync();

            return ParsearRespuestaEdocument(responseXml, eDocument);
        }
        catch (Exception ex)
        {
            return new CoveConsultaResult
            {
                Exitoso = false,
                Mensaje = $"Error de conexión (ConsultarEdocument): {ex.Message}"
            };
        }
    }

    // Sobrecarga de compatibilidad: si el código existente llama ConsultarCovesAsync con fechas,
    // redirigir indicando que se debe usar el folio.
    public Task<CoveConsultaResult> ConsultarCovesAsync(
        DateTime fechaInicio, DateTime fechaFin,
        string? rfcEmisor = null, string? estado = null)
    {
        return Task.FromResult(new CoveConsultaResult
        {
            Exitoso = false,
            Mensaje = "La búsqueda por rango de fechas no está disponible en VUCEM. " +
                      "Utilice ConsultarCovePorFolioAsync(folio) ingresando el eDocument directamente."
        });
    }

    // ─────────────────────────────────────────────────────────────────────
    // 2. ConsultarRespuestaCove: consulta resultado de una operación (int)
    // ─────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Consulta el resultado de una operación de COVE por su numeroOperacion (entero).
    /// Usa el servicio ConsultarRespuestaCove (puerto 8110).
    /// </summary>
    public async Task<CoveConsultaResult> ConsultarRespuestaCoveAsync(long numeroOperacion)
    {
        try
        {
            var soapEnvelope = GenerarSoapConsultarRespuestaCove(numeroOperacion);

            var content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");
            content.Headers.Add("SOAPAction", SoapActionConsultarRespuestaCove);

            var response = await _httpClient.PostAsync(ConsultarRespuestaCoveUrl, content);
            var responseXml = await response.Content.ReadAsStringAsync();

            return ParsearRespuestaRespuestaCove(responseXml);
        }
        catch (Exception ex)
        {
            return new CoveConsultaResult
            {
                Exitoso = false,
                Mensaje = $"Error de conexión (ConsultarRespuestaCove): {ex.Message}"
            };
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // 3. ConsultaAcuses: descarga el PDF acuse de un COVE
    // ─────────────────────────────────────────────────────────────────────
    public async Task<CoveDescargaResult> DescargarAcuseCoveAsync(string coveFolio)
    {
        try
        {
            var soapEnvelope = GenerarSoapConsultaAcusesCove(coveFolio.Trim());

            var content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");
            content.Headers.Add("SOAPAction", SoapActionConsultaAcuses);

            var response = await _httpClient.PostAsync(ConsultaAcusesUrl, content);
            var responseXml = await response.Content.ReadAsStringAsync();

            return ParsearRespuestaAcuse(responseXml, coveFolio);
        }
        catch (Exception ex)
        {
            return new CoveDescargaResult
            {
                Exitoso = false,
                Mensaje = $"Error de conexión (ConsultaAcuses): {ex.Message}"
            };
        }
    }

    // ─────────────────────────────────────────── Generadores SOAP ────────

    /// <summary>
    /// Genera el envelope SOAP para ConsultarEdocument.
    /// Body: ConsultarEdocumentRequest > request > {firmaElectronica, criterioBusqueda > eDocument}
    /// Nota: firmaElectronica se deja vacía; si VUCEM requiere FIEL real, integrar BouncyCastle aquí.
    /// </summary>
    private string GenerarSoapConsultarEdocument(string eDocument)
    {
        var rfc = _username.ToUpper().Trim();
        var (created, expires) = Timestamps();

        var sb = new StringBuilder();
        sb.AppendLine(@"<?xml version=""1.0"" encoding=""UTF-8""?>");
        sb.AppendLine($@"<soapenv:Envelope xmlns:soapenv=""{NS_SOAP}""");
        sb.AppendLine($@"                 xmlns:edoc=""{NS_EDOC}""");
        sb.AppendLine($@"                 xmlns:oxml=""{NS_COVE_OXML}"">");
        sb.AppendLine($@"  <soapenv:Header>");
        sb.AppendLine($@"    <wsse:Security soapenv:mustUnderstand=""1"" xmlns:wsse=""{NS_WSSE}"" xmlns:wsu=""{NS_WSU}"">");
        sb.AppendLine($@"      <wsse:UsernameToken wsu:Id=""UsernameToken-1"">");
        sb.AppendLine($@"        <wsse:Username>{Esc(rfc)}</wsse:Username>");
        sb.AppendLine($@"        <wsse:Password Type=""{PASSWORD_TYPE}"">{Esc(_password)}</wsse:Password>");
        sb.AppendLine($@"      </wsse:UsernameToken>");
        sb.AppendLine($@"      <wsu:Timestamp wsu:Id=""Timestamp-1"">");
        sb.AppendLine($@"        <wsu:Created>{created}</wsu:Created>");
        sb.AppendLine($@"        <wsu:Expires>{expires}</wsu:Expires>");
        sb.AppendLine($@"      </wsu:Timestamp>");
        sb.AppendLine($@"    </wsse:Security>");
        sb.AppendLine($@"  </soapenv:Header>");
        sb.AppendLine($@"  <soapenv:Body>");
        sb.AppendLine($@"    <edoc:ConsultarEdocumentRequest>");
        sb.AppendLine($@"      <edoc:request>");
        sb.AppendLine($@"        <edoc:firmaElectronica>");
        sb.AppendLine($@"          <oxml:certificado></oxml:certificado>");
        sb.AppendLine($@"          <oxml:cadenaOriginal></oxml:cadenaOriginal>");
        sb.AppendLine($@"          <oxml:firma></oxml:firma>");
        sb.AppendLine($@"        </edoc:firmaElectronica>");
        sb.AppendLine($@"        <edoc:criterioBusqueda>");
        sb.AppendLine($@"          <edoc:eDocument>{Esc(eDocument)}</edoc:eDocument>");
        sb.AppendLine($@"        </edoc:criterioBusqueda>");
        sb.AppendLine($@"      </edoc:request>");
        sb.AppendLine($@"    </edoc:ConsultarEdocumentRequest>");
        sb.AppendLine($@"  </soapenv:Body>");
        sb.AppendLine($@"</soapenv:Envelope>");
        return sb.ToString();
    }

    /// <summary>
    /// Genera el envelope SOAP para ConsultarRespuestaCove.
    /// Body: solicitarConsultarRespuestaCoveServicio > {numeroOperacion, firmaElectronica}
    /// </summary>
    private string GenerarSoapConsultarRespuestaCove(long numeroOperacion)
    {
        var rfc = _username.ToUpper().Trim();
        var (created, expires) = Timestamps();

        var sb = new StringBuilder();
        sb.AppendLine(@"<?xml version=""1.0"" encoding=""UTF-8""?>");
        sb.AppendLine($@"<soapenv:Envelope xmlns:soapenv=""{NS_SOAP}""");
        sb.AppendLine($@"                 xmlns:tns=""{NS_COVE_OXML}"">");
        sb.AppendLine($@"  <soapenv:Header>");
        sb.AppendLine($@"    <wsse:Security soapenv:mustUnderstand=""1"" xmlns:wsse=""{NS_WSSE}"" xmlns:wsu=""{NS_WSU}"">");
        sb.AppendLine($@"      <wsse:UsernameToken wsu:Id=""UsernameToken-1"">");
        sb.AppendLine($@"        <wsse:Username>{Esc(rfc)}</wsse:Username>");
        sb.AppendLine($@"        <wsse:Password Type=""{PASSWORD_TYPE}"">{Esc(_password)}</wsse:Password>");
        sb.AppendLine($@"      </wsse:UsernameToken>");
        sb.AppendLine($@"      <wsu:Timestamp wsu:Id=""Timestamp-1"">");
        sb.AppendLine($@"        <wsu:Created>{created}</wsu:Created>");
        sb.AppendLine($@"        <wsu:Expires>{expires}</wsu:Expires>");
        sb.AppendLine($@"      </wsu:Timestamp>");
        sb.AppendLine($@"    </wsse:Security>");
        sb.AppendLine($@"  </soapenv:Header>");
        sb.AppendLine($@"  <soapenv:Body>");
        sb.AppendLine($@"    <tns:solicitarConsultarRespuestaCoveServicio>");
        sb.AppendLine($@"      <tns:numeroOperacion>{numeroOperacion}</tns:numeroOperacion>");
        sb.AppendLine($@"      <tns:firmaElectronica>");
        sb.AppendLine($@"        <tns:certificado></tns:certificado>");
        sb.AppendLine($@"        <tns:cadenaOriginal></tns:cadenaOriginal>");
        sb.AppendLine($@"        <tns:firma></tns:firma>");
        sb.AppendLine($@"      </tns:firmaElectronica>");
        sb.AppendLine($@"    </tns:solicitarConsultarRespuestaCoveServicio>");
        sb.AppendLine($@"  </soapenv:Body>");
        sb.AppendLine($@"</soapenv:Envelope>");
        return sb.ToString();
    }

    /// <summary>
    /// Genera el envelope SOAP para ConsultaAcuses (descarga PDF).
    /// SOAPAction: http://www.ventanillaunica.gob.mx/consulta/acuses/ConsultaAcuses
    /// </summary>
    private string GenerarSoapConsultaAcusesCove(string coveFolio)
    {
        var rfc = _username.ToUpper().Trim();

        var sb = new StringBuilder();
        sb.AppendLine(@"<?xml version=""1.0"" encoding=""UTF-8""?>");
        sb.AppendLine($@"<soapenv:Envelope xmlns:soapenv=""{NS_SOAP}""");
        sb.AppendLine($@"                 xmlns:oxml=""{NS_ACUSES}"">");
        sb.AppendLine($@"  <soapenv:Header>");
        sb.AppendLine($@"    <wsse:Security soapenv:mustUnderstand=""1"" xmlns:wsse=""{NS_WSSE}"" xmlns:wsu=""{NS_WSU}"">");
        sb.AppendLine($@"      <wsse:UsernameToken>");
        sb.AppendLine($@"        <wsse:Username>{Esc(rfc)}</wsse:Username>");
        sb.AppendLine($@"        <wsse:Password Type=""{PASSWORD_TYPE}"">{Esc(_password)}</wsse:Password>");
        sb.AppendLine($@"      </wsse:UsernameToken>");
        sb.AppendLine($@"    </wsse:Security>");
        sb.AppendLine($@"  </soapenv:Header>");
        sb.AppendLine($@"  <soapenv:Body>");
        sb.AppendLine($@"    <oxml:consultaAcusesPeticion>");
        sb.AppendLine($@"      <idEdocument>{Esc(coveFolio)}</idEdocument>");
        sb.AppendLine($@"    </oxml:consultaAcusesPeticion>");
        sb.AppendLine($@"  </soapenv:Body>");
        sb.AppendLine($@"</soapenv:Envelope>");
        return sb.ToString();
    }

    // ─────────────────────────────────────────── Parsers ─────────────────

    private CoveConsultaResult ParsearRespuestaEdocument(string xml, string folioConsultado)
    {
        var result = new CoveConsultaResult();
        var coves = new List<CoveInfo>();

        try
        {
            if (EsFault(xml))
            {
                result.Exitoso = false;
                result.Mensaje = ExtraerFaultString(xml);
                result.RespuestaRaw = xml;
                return result;
            }

            // contieneError
            var contieneError = Regex.Match(xml,
                @"<[:\w]*contieneError>(.*?)</[:\w]*contieneError>", RegexOptions.Singleline);
            if (contieneError.Success &&
                contieneError.Groups[1].Value.Trim().Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                result.Exitoso = false;
                result.Mensaje = ExtraerErrores(xml);
                result.RespuestaRaw = xml;
                return result;
            }

            // Datos principales del COVE
            var eDoc = RegexVal(xml, @"<[:\w]*eDocument>(.*?)</[:\w]*eDocument>");
            var tipoOp = RegexVal(xml, @"<[:\w]*tipoOperacion>(.*?)</[:\w]*tipoOperacion>");
            var numeroFact = RegexVal(xml, @"<[:\w]*numeroFacturaRelacionFacturas>(.*?)</[:\w]*numeroFacturaRelacionFacturas>");
            var fechaExp = RegexVal(xml, @"<[:\w]*fechaExpedicion>(.*?)</[:\w]*fechaExpedicion>");

            // Emisor / Destinatario
            var rfcEmisor = RegexVal(xml, @"<[:\w]*emisor>.*?<[:\w]*identificacion>(.*?)</[:\w]*identificacion>.*?</[:\w]*emisor>");
            var rfcDestinatario = RegexVal(xml, @"<[:\w]*destinatario>.*?<[:\w]*identificacion>(.*?)</[:\w]*identificacion>.*?</[:\w]*destinatario>");

            result.Exitoso = true;
            result.Mensaje = string.IsNullOrEmpty(eDoc)
                ? $"No se encontró el eDocument '{folioConsultado}'"
                : $"COVE encontrado: {eDoc}";

            if (!string.IsNullOrEmpty(eDoc) || !string.IsNullOrEmpty(numeroFact))
            {
                coves.Add(new CoveInfo
                {
                    NumeroOperacion = eDoc,
                    FechaCreacion = DateTime.TryParse(fechaExp, out var fd) ? fd : DateTime.Now,
                    RfcEmisor = rfcEmisor,
                    RfcReceptor = rfcDestinatario,
                    Estado = "CONSULTADO",
                    TipoOperacion = tipoOp,
                    ValorTotal = 0,
                    Moneda = ""
                });
            }

            result.Covess = coves;
            result.TotalRegistros = coves.Count;
            result.RespuestaRaw = xml;
        }
        catch (Exception ex)
        {
            result.Exitoso = false;
            result.Mensaje = "Error al procesar respuesta: " + ex.Message;
        }

        return result;
    }

    private CoveConsultaResult ParsearRespuestaRespuestaCove(string xml)
    {
        var result = new CoveConsultaResult();
        var coves = new List<CoveInfo>();

        try
        {
            if (EsFault(xml))
            {
                result.Exitoso = false;
                result.Mensaje = ExtraerFaultString(xml);
                result.RespuestaRaw = xml;
                return result;
            }

            var numOp = RegexVal(xml, @"<[:\w]*numeroOperacion>(.*?)</[:\w]*numeroOperacion>");
            var horaRec = RegexVal(xml, @"<[:\w]*horaRecepcion>(.*?)</[:\w]*horaRecepcion>");
            var leyenda = RegexVal(xml, @"<[:\w]*leyenda>(.*?)</[:\w]*leyenda>");

            // respuestasOperaciones[]
            foreach (Match m in Regex.Matches(xml,
                @"<[:\w]*respuestasOperaciones>(.*?)</[:\w]*respuestasOperaciones>",
                RegexOptions.Singleline))
            {
                var inner = m.Groups[1].Value;
                var contieneError = RegexVal(inner, @"<[:\w]*contieneError>(.*?)</[:\w]*contieneError>");
                var eDoc = RegexVal(inner, @"<[:\w]*eDocument>(.*?)</[:\w]*eDocument>");
                var numFact = RegexVal(inner, @"<[:\w]*numeroFacturaORelacionFacturas>(.*?)</[:\w]*numeroFacturaORelacionFacturas>");

                coves.Add(new CoveInfo
                {
                    NumeroOperacion = string.IsNullOrEmpty(eDoc) ? numFact : eDoc,
                    FechaCreacion = DateTime.TryParse(horaRec, out var fd) ? fd : DateTime.Now,
                    RfcEmisor = _username,
                    RfcReceptor = "",
                    Estado = contieneError.Equals("true", StringComparison.OrdinalIgnoreCase) ? "ERROR" : "PROCESADO",
                    TipoOperacion = "COVE",
                    ValorTotal = 0,
                    Moneda = ""
                });
            }

            result.Exitoso = true;
            result.Covess = coves;
            result.TotalRegistros = coves.Count;
            result.Mensaje = string.IsNullOrEmpty(leyenda)
                ? $"Operación {numOp}: {coves.Count} resultado(s)"
                : leyenda;
            result.RespuestaRaw = xml;
        }
        catch (Exception ex)
        {
            result.Exitoso = false;
            result.Mensaje = "Error al procesar respuesta: " + ex.Message;
        }

        return result;
    }

    private CoveDescargaResult ParsearRespuestaAcuse(string xml, string coveFolio)
    {
        var result = new CoveDescargaResult();

        try
        {
            if (EsFault(xml))
            {
                result.Exitoso = false;
                result.Mensaje = ExtraerFaultString(xml);
                return result;
            }

            var hasError = RegexVal(xml, @"<[:\w]*error>(.*?)</[:\w]*error>").ToLower();
            var code = RegexVal(xml, @"<[:\w]*code>(.*?)</[:\w]*code>");
            var descripcion = RegexVal(xml, @"<[:\w]*descripcion>(.*?)</[:\w]*descripcion>");

            if (hasError == "true" || hasError == "1" || (code != "" && code != "0"))
            {
                result.Exitoso = false;
                result.Mensaje = string.IsNullOrEmpty(descripcion) ? "Error desconocido" : descripcion;
                return result;
            }

            var acuseDocumento = RegexVal(xml, @"<[:\w]*acuseDocumento>(.*?)</[:\w]*acuseDocumento>");
            acuseDocumento = Regex.Replace(acuseDocumento, @"[\r\n\s]+", "");

            if (!string.IsNullOrEmpty(acuseDocumento))
            {
                try
                {
                    result.Exitoso = true;
                    result.Contenido = Convert.FromBase64String(acuseDocumento);
                    result.NombreArchivo = $"ACUSE_{coveFolio}.pdf";
                    result.Mensaje = "Acuse descargado exitosamente";
                }
                catch
                {
                    result.Exitoso = false;
                    result.Mensaje = "Error al decodificar el PDF del acuse";
                }
            }
            else
            {
                result.Exitoso = false;
                result.Mensaje = $"No se encontró acuse para el COVE: {coveFolio}";
            }
        }
        catch (Exception ex)
        {
            result.Exitoso = false;
            result.Mensaje = "Error al procesar respuesta: " + ex.Message;
        }

        return result;
    }

    // ─────────────────────────────────────────── Helpers ─────────────────

    private static (string created, string expires) Timestamps()
    {
        var now = DateTime.UtcNow;
        return (now.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                now.AddMinutes(5).ToString("yyyy-MM-ddTHH:mm:ssZ"));
    }

    private static string Esc(string s) =>
        System.Security.SecurityElement.Escape(s) ?? "";

    private static string RegexVal(string xml, string pattern)
    {
        var m = Regex.Match(xml, pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);
        return m.Success ? m.Groups[1].Value.Trim() : "";
    }

    private static bool EsFault(string xml) =>
        xml.Contains("Fault") || xml.Contains("faultstring") || xml.Contains("SOAPFault");

    private static string ExtraerFaultString(string xml)
    {
        var fs = RegexVal(xml, @"<[:\w]*faultstring>(.*?)</[:\w]*faultstring>");
        return string.IsNullOrEmpty(fs) ? "Error del servicio VUCEM" : fs;
    }

    private static string ExtraerErrores(string xml)
    {
        var errores = new List<string>();
        foreach (Match m in Regex.Matches(xml,
            @"<[:\w]*error>(.*?)</[:\w]*error>", RegexOptions.Singleline))
        {
            errores.Add(m.Groups[1].Value.Trim());
        }
        // También puede venir como <mensaje>...</mensaje>
        if (errores.Count == 0)
        {
            var msg = RegexVal(xml, @"<[:\w]*mensaje>(.*?)</[:\w]*mensaje>");
            if (!string.IsNullOrEmpty(msg)) errores.Add(msg);
        }
        return errores.Count > 0
            ? string.Join("; ", errores)
            : "VUCEM reportó un error sin descripción";
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
