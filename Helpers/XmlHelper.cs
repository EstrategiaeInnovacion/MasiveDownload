using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;

namespace VucemDownloader.Helpers;

public static class XmlHelper
{
    public static string GenerarSoapEnvelope(string operation, Dictionary<string, string> parameters)
    {
        var sb = new StringBuilder();
        sb.AppendLine(@"<?xml version=""1.0"" encoding=""UTF-8""?>");
        sb.AppendLine(@"<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" ");
        sb.AppendLine(@"xmlns:tem=""http://tempuri.org/"">");
        sb.AppendLine("<soap:Header/>");
        sb.AppendLine("<soap:Body>");
        sb.AppendLine($"<tem:{operation}>");

        foreach (var param in parameters)
        {
            sb.AppendLine($"<tem:{param.Key}>{param.Value}</tem:{param.Key}>");
        }

        sb.AppendLine($"</tem:{operation}>");
        sb.AppendLine("</soap:Body>");
        sb.AppendLine("</soap:Envelope>");
        return sb.ToString();
    }

    public static XDocument CargarXml(string ruta)
    {
        return XDocument.Load(ruta);
    }

    public static bool GuardarXml(XDocument doc, string ruta)
    {
        try
        {
            doc.Save(ruta);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static string? ObtenerValor(XElement element, string nombre)
    {
        return element.Attribute(nombre)?.Value ?? element.Element(nombre)?.Value;
    }

    public static string? ObtenerValorNamespace(XElement element, XNamespace ns, string nombre)
    {
        return element.Element(ns + nombre)?.Value;
    }

    public static DateTime? ParsearFecha(string fecha)
    {
        if (DateTime.TryParse(fecha, out var result))
            return result;
        return null;
    }

    public static decimal? ParsearDecimal(string valor)
    {
        if (decimal.TryParse(valor, out var result))
            return result;
        return null;
    }

    public static string FormatearFecha(DateTime fecha, string formato = "yyyyMMdd")
    {
        return fecha.ToString(formato);
    }

    public static string FormatearFechaMexico(DateTime fecha)
    {
        return fecha.ToString("dd/MM/yyyy HH:mm:ss");
    }

    public static bool EsXmlValido(string xml)
    {
        try
        {
            XDocument.Parse(xml);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
