using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VucemDownloader.Models;

namespace VucemDownloader.Services;

/// <summary>
/// Cliente para el servicio de Pedimentos de VUCEM.
/// 
/// Endpoint: https://www.ventanillaunica.gob.mx/ventanilla-ws-pedimentos/ConsultarPedimentoCompletoService
/// </summary>
public class VucemPedimentoClient : IDisposable
{
    private const string ConsultaUrl = "https://www.ventanillaunica.gob.mx/ventanilla-ws-pedimentos/ConsultarPedimentoCompletoService";
    
    private const string NS_SOAP = "http://schemas.xmlsoap.org/soap/envelope/";
    private const string NS_WSSE = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";
    private const string NS_WSU = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd";
    private const string NS_PED = "http://www.ventanillaunica.gob.mx/pedimento/ws/";
    private const string PASSWORD_TYPE = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-username-token-profile-1.0#PasswordText";

    private readonly HttpClient _httpClient;
    private string _username;
    private string _password;

    public VucemPedimentoClient(string username, string password)
    {
        _username = username;
        _password = password;
        
        _httpClient = new HttpClient(new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        });
        _httpClient.Timeout = TimeSpan.FromMinutes(5);
    }

    public void ActualizarCredenciales(string username, string password)
    {
        _username = username;
        _password = password;
    }

    public async Task<PedimentoConsultaResult> ConsultarPedimentosAsync(DateTime fechaInicio, DateTime fechaFin, string? rfc = null, string? aduana = null)
    {
        try
        {
             // La búsqueda masiva por fecha en VUCEM suele ser restringida o requerir firma FIEL.
             // Aquí implementamos la estructura SOAP estándar de VUCEM.
            var soapEnvelope = GenerarSoapConsultarPedimento(fechaInicio, fechaFin, rfc?.Trim());
            
            var content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");
            content.Headers.Add("SOAPAction", "");

            var response = await _httpClient.PostAsync(ConsultaUrl, content);
            var responseXml = await response.Content.ReadAsStringAsync();

            return ParsearRespuestaConsultarPedimento(responseXml);
        }
        catch (Exception ex)
        {
            return new PedimentoConsultaResult
            {
                Exitoso = false,
                Mensaje = "Error de conexión (Pedimentos): " + ex.Message
            };
        }
    }

    public async Task<PedimentoDescargaResult> DescargarPdfPedimentoAsync(string numeroPedimento)
    {
        try
        {
            var soapEnvelope = GenerarSoapDescargarPedimento(numeroPedimento.Trim());
            
            var content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");
            content.Headers.Add("SOAPAction", "");

            var response = await _httpClient.PostAsync(ConsultaUrl, content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseBytes = await response.Content.ReadAsByteArrayAsync();
                // OJO: VUCEM suele devolver bytes directamente o un XML con base64. 
                // Si es un WS de descarga, a veces devuelve el PDF directamente en el body.
                
                return new PedimentoDescargaResult
                {
                    Exitoso = true,
                    Contenido = responseBytes,
                    NombreArchivo = "PED_" + numeroPedimento + ".pdf",
                    Mensaje = "Documento obtenido correctamente"
                };
            }
            
            return new PedimentoDescargaResult { Exitoso = false, Mensaje = "Error del servidor VUCEM: " + response.StatusCode };
        }
        catch (Exception ex)
        {
            return new PedimentoDescargaResult
            {
                Exitoso = false,
                Mensaje = "Error en descarga Pedimento: " + ex.Message
            };
        }
    }

    private string GenerarSoapConsultarPedimento(DateTime fechaInicio, DateTime fechaFin, string? rfc)
    {
        var rfcParam = string.IsNullOrEmpty(rfc) ? _username.ToUpper() : rfc.ToUpper();
        var now = DateTime.UtcNow;
        string created = now.ToString("yyyy-MM-ddTHH:mm:ssZ");
        string expires = now.AddMinutes(5).ToString("yyyy-MM-ddTHH:mm:ssZ");
        
        var sb = new StringBuilder();
        sb.AppendLine(@"<?xml version=""1.0"" encoding=""UTF-8""?>");
        sb.AppendLine($@"<soapenv:Envelope xmlns:soapenv=""{NS_SOAP}"" xmlns:ped=""{NS_PED}"">");
        sb.AppendLine($@"  <soapenv:Header>");
        sb.AppendLine($@"    <wsse:Security soapenv:mustUnderstand=""1"" xmlns:wsse=""{NS_WSSE}"" xmlns:wsu=""{NS_WSU}"">");
        sb.AppendLine($@"      <wsse:UsernameToken>");
        sb.AppendLine($@"        <wsse:Username>{Esc(rfcParam)}</wsse:Username>");
        sb.AppendLine($@"        <wsse:Password Type=""{PASSWORD_TYPE}"">{Esc(_password)}</wsse:Password>");
        sb.AppendLine($@"      </wsse:UsernameToken>");
        sb.AppendLine($@"      <wsu:Timestamp>");
        sb.AppendLine($@"        <wsu:Created>{created}</wsu:Created>");
        sb.AppendLine($@"        <wsu:Expires>{expires}</wsu:Expires>");
        sb.AppendLine($@"      </wsu:Timestamp>");
        sb.AppendLine($@"    </wsse:Security>");
        sb.AppendLine($@"  </soapenv:Header>");
        sb.AppendLine($@"  <soapenv:Body>");
        sb.AppendLine($@"    <ped:consultarPedimento>");
        sb.AppendLine($@"      <ped:fechaInicial>{fechaInicio:yyyy-MM-dd}</ped:fechaInicial>");
        sb.AppendLine($@"      <ped:fechaFinal>{fechaFin:yyyy-MM-dd}</ped:fechaFinal>");
        sb.AppendLine($@"      <ped:rfc>{Esc(rfcParam)}</ped:rfc>");
        sb.AppendLine($@"    </ped:consultarPedimento>");
        sb.AppendLine($@"  </soapenv:Body>");
        sb.AppendLine($@"</soapenv:Envelope>");
        return sb.ToString();
    }

    private string GenerarSoapDescargarPedimento(string numeroPedimento)
    {
        var rfc = _username.ToUpper().Trim();
        var now = DateTime.UtcNow;
        
        var sb = new StringBuilder();
        sb.AppendLine(@"<?xml version=""1.0"" encoding=""UTF-8""?>");
        sb.AppendLine($@"<soapenv:Envelope xmlns:soapenv=""{NS_SOAP}"" xmlns:ped=""{NS_PED}"">");
        sb.AppendLine($@"  <soapenv:Header>");
        sb.AppendLine($@"    <wsse:Security xmlns:wsse=""{NS_WSSE}"">");
        sb.AppendLine($@"      <wsse:UsernameToken>");
        sb.AppendLine($@"        <wsse:Username>{Esc(rfc)}</wsse:Username>");
        sb.AppendLine($@"        <wsse:Password Type=""{PASSWORD_TYPE}"">{Esc(_password)}</wsse:Password>");
        sb.AppendLine($@"      </wsse:UsernameToken>");
        sb.AppendLine($@"    </wsse:Security>");
        sb.AppendLine($@"  </soapenv:Header>");
        sb.AppendLine($@"  <soapenv:Body>");
        sb.AppendLine($@"    <ped:descargarPdfPedimento>");
        sb.AppendLine($@"      <ped:numeroPedimento>{Esc(numeroPedimento)}</ped:numeroPedimento>");
        sb.AppendLine($@"    </ped:descargarPdfPedimento>");
        sb.AppendLine($@"  </soapenv:Body>");
        sb.AppendLine($@"</soapenv:Envelope>");
        return sb.ToString();
    }

    private PedimentoConsultaResult ParsearRespuestaConsultarPedimento(string xml)
    {
        var result = new PedimentoConsultaResult { Exitoso = true };
        var pedimentos = new List<PedimentoInfo>();

        try
        {
            if (xml.Contains("Fault") || xml.Contains("faultstring"))
            {
                result.Exitoso = false;
                result.Mensaje = RegexVal(xml, @"<[:\w]*faultstring>(.*?)</[:\w]*faultstring>") ?? "Error del servicio VUCEM";
                return result;
            }

            // Parseo básico por Regex (más rápido si no hay namespaces complejos claros)
            var matches = Regex.Matches(xml, @"<[:\w]*pedimento>(.*?)</[:\w]*pedimento>", RegexOptions.Singleline);
            
            foreach (Match m in matches)
            {
                var inner = m.Groups[1].Value;
                var ped = new PedimentoInfo
                {
                    NumeroPedimento = RegexVal(inner, @"<[:\w]*numeroPedimento>(.*?)</[:\w]*numeroPedimento>"),
                    FechaPago = DateTime.TryParse(RegexVal(inner, @"<[:\w]*fechaPago>(.*?)</[:\w]*fechaPago>"), out var f) ? f : DateTime.Now,
                    RfcImportador = RegexVal(inner, @"<[:\w]*rfcImportador>(.*?)</[:\w]*rfcImportador>"),
                    Aduana = RegexVal(inner, @"<[:\w]*aduana>(.*?)</[:\w]*aduana>"),
                    Estado = RegexVal(inner, @"<[:\w]*estado>(.*?)</[:\w]*estado>"),
                    ValorAduana = decimal.TryParse(RegexVal(inner, @"<[:\w]*valorAduana>(.*?)</[:\w]*valorAduana>"), out var v) ? v : 0,
                    TipoOperacion = RegexVal(inner, @"<[:\w]*tipoOperacion>(.*?)</[:\w]*tipoOperacion>")
                };
                if (!string.IsNullOrEmpty(ped.NumeroPedimento)) pedimentos.Add(ped);
            }

            result.Pedimentos = pedimentos;
            result.TotalRegistros = pedimentos.Count;
            result.Mensaje = pedimentos.Count > 0 ? $"Se encontraron {pedimentos.Count} pedimentos" : "No se encontraron resultados";
        }
        catch (Exception ex)
        {
            result.Exitoso = false;
            result.Mensaje = "Error al procesar respuesta Pedimento: " + ex.Message;
        }

        return result;
    }

    private static string Esc(string s) => System.Security.SecurityElement.Escape(s) ?? "";

    private static string RegexVal(string xml, string pattern)
    {
        var m = Regex.Match(xml, pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);
        return m.Success ? m.Groups[1].Value.Trim() : "";
    }

    public void Dispose() => _httpClient?.Dispose();
}
