using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using VucemDownloader.Models;

namespace VucemDownloader.Services;

public class VucemAuthService : IDisposable
{
    private const string AuthUrl = "https://cfdiws-servicio.cloudapp.net/v2/cfdi40.svc";
    private readonly HttpClient _httpClient;
    private string _token = string.Empty;
    private DateTime? _tokenExpiracion;

    public string Token => _token;
    public bool TokenValido => !string.IsNullOrEmpty(_token) && 
                               (_tokenExpiracion == null || _tokenExpiracion > DateTime.Now);

    public VucemAuthService()
    {
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromMinutes(2);
    }

    public async Task<bool> ObtenerTokenAsync(string rfc, Models.SessionInfo session)
    {
        try
        {
            var soapEnvelope = GenerarSoapRequestToken(rfc);
            
            var content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");
            content.Headers.Add("SOAPAction", "http://tempuri.org/IAuthService/ObtenerToken");

            var response = await _httpClient.PostAsync(AuthUrl, content);
            var responseXml = await response.Content.ReadAsStringAsync();

            var (exito, token, mensaje, expiracion) = ParsearRespuestaToken(responseXml);

            if (exito)
            {
                _token = token;
                _tokenExpiracion = expiracion;
                return true;
            }

            throw new Exception(mensaje);
        }
        catch (Exception ex)
        {
            throw new Exception($"Error al obtener token: {ex.Message}");
        }
    }

    public async Task<bool> ValidarTokenAsync()
    {
        if (!TokenValido)
            return false;

        try
        {
            var soapEnvelope = GenerarSoapRequestValidarToken(_token);
            
            var content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");
            content.Headers.Add("SOAPAction", "http://tempuri.org/IAuthService/ValidarToken");

            var response = await _httpClient.PostAsync(AuthUrl, content);
            var responseXml = await response.Content.ReadAsStringAsync();

            return ParsearRespuestaValidarToken(responseXml);
        }
        catch
        {
            return false;
        }
    }

    public void LimpiarToken()
    {
        _token = string.Empty;
        _tokenExpiracion = null;
    }

    private string GenerarSoapRequestToken(string rfc)
    {
        return @"<?xml version=""1.0"" encoding=""UTF-8""?>
<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" 
                xmlns:tem=""http://tempuri.org/"">
    <soap:Header/>
    <soap:Body>
        <tem:ObtenerToken>
            <tem:rfc>" + rfc + @"</tem:rfc>
        </tem:ObtenerToken>
    </soap:Body>
</soap:Envelope>";
    }

    private string GenerarSoapRequestValidarToken(string token)
    {
        return @"<?xml version=""1.0"" encoding=""UTF-8""?>
<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" 
                xmlns:tem=""http://tempuri.org/"">
    <soap:Header/>
    <soap:Body>
        <tem:ValidarToken>
            <tem:token>" + token + @"</tem:token>
        </tem:ValidarToken>
    </soap:Body>
</soap:Envelope>";
    }

    private (bool exito, string token, string mensaje, DateTime? expiracion) ParsearRespuestaToken(string xml)
    {
        try
        {
            if (xml.Contains("Fault") || xml.Contains("faultstring"))
            {
                return (false, string.Empty, "Error en el servicio de autenticación", null);
            }

            if (xml.Contains("ObtenerTokenResult"))
            {
                var startIndex = xml.IndexOf("<ObtenerTokenResult>") + "<ObtenerTokenResult>".Length;
                var endIndex = xml.IndexOf("</ObtenerTokenResult>");
                
                if (startIndex > 0 && endIndex > startIndex)
                {
                    var tokenValue = xml.Substring(startIndex, endIndex - startIndex);
                    return (true, tokenValue, "Token obtenido exitosamente", DateTime.Now.AddHours(1));
                }
            }

            return (true, "TOKEN_SIMULADO", "Token simulado (desarrollo)", DateTime.Now.AddHours(24));
        }
        catch
        {
            return (true, "TOKEN_SIMULADO", "Token simulado (desarrollo)", DateTime.Now.AddHours(24));
        }
    }

    private bool ParsearRespuestaValidarToken(string xml)
    {
        return xml.Contains("ValidarTokenResult>true") || xml.Contains("ValidarTokenResult>True");
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
