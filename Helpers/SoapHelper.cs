using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using VucemDownloader.Models;

namespace VucemDownloader.Services;

public class SoapHelper
{
    private static readonly HttpClient _httpClient = new();

    public SoapHelper()
    {
        _httpClient.Timeout = TimeSpan.FromMinutes(5);
    }

    public static string GenerarEnvelopeConsultarCove(string token, DateTime fechaInicio, DateTime fechaFin, string? rfcEmisor = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine(@"<?xml version=""1.0"" encoding=""UTF-8""?>");
        sb.AppendLine(@"<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" ");
        sb.AppendLine(@"xmlns:tem=""http://tempuri.org/"" ");
        sb.AppendLine(@"xmlns:vucem=""https://schemas.datacontract.org/2004/07/VucemService"">");
        sb.AppendLine("<soap:Header>");
        sb.AppendLine($"<vucem:Token>{token}</vucem:Token>");
        sb.AppendLine("</soap:Header>");
        sb.AppendLine("<soap:Body>");
        sb.AppendLine("<tem:ConsultarCove>");
        sb.AppendLine("<tem:fechaInicial>" + fechaInicio.ToString("yyyy-MM-dd") + "</tem:fechaInicial>");
        sb.AppendLine("<tem:fechaFinal>" + fechaFin.ToString("yyyy-MM-dd") + "</tem:fechaFinal>");
        
        if (!string.IsNullOrEmpty(rfcEmisor))
            sb.AppendLine("<tem:rfcEmisor>" + rfcEmisor + "</tem:rfcEmisor>");
        
        sb.AppendLine("</tem:ConsultarCove>");
        sb.AppendLine("</soap:Body>");
        sb.AppendLine("</soap:Envelope>");
        return sb.ToString();
    }

    public static string GenerarEnvelopeConsultarManifestacion(string token, DateTime fechaInicio, DateTime fechaFin, string? rfc = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine(@"<?xml version=""1.0"" encoding=""UTF-8""?>");
        sb.AppendLine(@"<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" ");
        sb.AppendLine(@"xmlns:tem=""http://tempuri.org/"" ");
        sb.AppendLine(@"xmlns:vucem=""https://schemas.datacontract.org/2004/07/VucemService"">");
        sb.AppendLine("<soap:Header>");
        sb.AppendLine($"<vucem:Token>{token}</vucem:Token>");
        sb.AppendLine("</soap:Header>");
        sb.AppendLine("<soap:Body>");
        sb.AppendLine("<tem:ConsultarManifestacion>");
        sb.AppendLine("<tem:fechaInicial>" + fechaInicio.ToString("yyyy-MM-dd") + "</tem:fechaInicial>");
        sb.AppendLine("<tem:fechaFinal>" + fechaFin.ToString("yyyy-MM-dd") + "</tem:fechaFinal>");
        
        if (!string.IsNullOrEmpty(rfc))
            sb.AppendLine("<tem:rfc>" + rfc + "</tem:rfc>");
        
        sb.AppendLine("</tem:ConsultarManifestacion>");
        sb.AppendLine("</soap:Body>");
        sb.AppendLine("</soap:Envelope>");
        return sb.ToString();
    }

    public static string GenerarEnvelopeConsultarPedimentos(string token, DateTime fechaInicio, DateTime fechaFin, string? rfc = null, string? aduana = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine(@"<?xml version=""1.0"" encoding=""UTF-8""?>");
        sb.AppendLine(@"<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" ");
        sb.AppendLine(@"xmlns:tem=""http://tempuri.org/"" ");
        sb.AppendLine(@"xmlns:vucem=""https://schemas.datacontract.org/2004/07/VucemService"">");
        sb.AppendLine("<soap:Header>");
        sb.AppendLine($"<vucem:Token>{token}</vucem:Token>");
        sb.AppendLine("</soap:Header>");
        sb.AppendLine("<soap:Body>");
        sb.AppendLine("<tem:ConsultarPedimento>");
        sb.AppendLine("<tem:fechaInicial>" + fechaInicio.ToString("yyyy-MM-dd") + "</tem:fechaInicial>");
        sb.AppendLine("<tem:fechaFinal>" + fechaFin.ToString("yyyy-MM-dd") + "</tem:fechaFinal>");
        
        if (!string.IsNullOrEmpty(rfc))
            sb.AppendLine("<tem:rfc>" + rfc + "</tem:rfc>");
        
        if (!string.IsNullOrEmpty(aduana))
            sb.AppendLine("<tem:aduana>" + aduana + "</tem:aduana>");
        
        sb.AppendLine("</tem:ConsultarPedimento>");
        sb.AppendLine("</soap:Body>");
        sb.AppendLine("</soap:Envelope>");
        return sb.ToString();
    }

    public static string GenerarEnvelopeObtenerDetalleCove(string token, string numeroOperacion)
    {
        var sb = new StringBuilder();
        sb.AppendLine(@"<?xml version=""1.0"" encoding=""UTF-8""?>");
        sb.AppendLine(@"<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" ");
        sb.AppendLine(@"xmlns:tem=""http://tempuri.org/"">");
        sb.AppendLine("<soap:Header>");
        sb.AppendLine("<vucem:Token xmlns:vucem=\"https://schemas.datacontract.org/2004/07/VucemService\">" + token + "</vucem:Token>");
        sb.AppendLine("</soap:Header>");
        sb.AppendLine("<soap:Body>");
        sb.AppendLine("<tem:ObtenerDetalleCove>");
        sb.AppendLine("<tem:numeroOperacion>" + numeroOperacion + "</tem:numeroOperacion>");
        sb.AppendLine("</tem:ObtenerDetalleCove>");
        sb.AppendLine("</soap:Body>");
        sb.AppendLine("</soap:Envelope>");
        return sb.ToString();
    }

    public static string GenerarEnvelopeDescargarDocumento(string token, string numeroOperacion, string tipoDocumento)
    {
        var sb = new StringBuilder();
        sb.AppendLine(@"<?xml version=""1.0"" encoding=""UTF-8""?>");
        sb.AppendLine(@"<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" ");
        sb.AppendLine(@"xmlns:tem=""http://tempuri.org/"">");
        sb.AppendLine("<soap:Header>");
        sb.AppendLine("<vucem:Token xmlns:vucem=\"https://schemas.datacontract.org/2004/07/VucemService\">" + token + "</vucem:Token>");
        sb.AppendLine("</soap:Header>");
        sb.AppendLine("<soap:Body>");
        sb.AppendLine("<tem:DescargarDocumento>");
        sb.AppendLine("<tem:numeroOperacion>" + numeroOperacion + "</tem:numeroOperacion>");
        sb.AppendLine("<tem:tipoDocumento>" + tipoDocumento + "</tem:tipoDocumento>");
        sb.AppendLine("</tem:DescargarDocumento>");
        sb.AppendLine("</soap:Body>");
        sb.AppendLine("</soap:Envelope>");
        return sb.ToString();
    }

    public async Task<string> EnviarRequestAsync(string url, string soapEnvelope, string soapAction)
    {
        var content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");
        content.Headers.Add("SOAPAction", soapAction);
        
        var response = await _httpClient.PostAsync(url, content);
        return await response.Content.ReadAsStringAsync();
    }

    public static XDocument ParsearXml(string xml)
    {
        return XDocument.Parse(xml);
    }

    public static string ExtraerContenidoNodo(XDocument doc, string xpath)
    {
        var element = doc.XPathSelectElement(xpath);
        return element?.Value ?? string.Empty;
    }

    public static List<XElement> ExtraerElementos(XDocument doc, string xpath)
    {
        return doc.XPathSelectElements(xpath).ToList();
    }
}

public static class XDocumentExtensions
{
    public static IEnumerable<XElement> XPathSelectElements(this XDocument doc, string xpath)
    {
        return doc.XPathSelectElements(xpath);
    }

    public static XElement? XPathSelectElement(this XDocument doc, string xpath)
    {
        return doc.XPathSelectElement(xpath);
    }
}
