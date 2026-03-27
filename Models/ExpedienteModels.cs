using System;
using System.Collections.Generic;

namespace VucemDownloader.Models;

public class Expediente
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Rfc { get; set; } = string.Empty;
    public string Aduana { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaCierre { get; set; }
    public string Estado { get; set; } = "En proceso";
    public string RutaCarpeta { get; set; } = string.Empty;
    public string RutaZip { get; set; } = string.Empty;
}

public class DocumentoExpediente
{
    public int Id { get; set; }
    public int ExpedienteId { get; set; }
    public string TipoDocumento { get; set; } = string.Empty;
    public string NombreOriginal { get; set; } = string.Empty;
    public string NombreArchivo { get; set; } = string.Empty;
    public string RutaArchivo { get; set; } = string.Empty;
    public DateTime FechaSubida { get; set; }
    public string Hash { get; set; } = string.Empty;
    public long Tamano { get; set; }
}

public class DocumentoFaltante
{
    public string TipoDocumento { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public bool TieneDocumento { get; set; }
    public string? NombreArchivo { get; set; }
}

public class TiposDocumento
{
    public static readonly Dictionary<string, string> Catalogo = new()
    {
        { "PED", "Pedimento pagado detallado" },
        { "SIM", "Pedimento Simplificado" },
        { "FAC", "Factura comercial - Proveeduria" },
        { "PKL", "Lista de empaque" },
        { "GUIA", "HBL, MBL, AWB (Guia conocimiento de embarque)" },
        { "CO", "Certificado de origen" },
        { "COV", "COVE" },
        { "DOD", "DODA" },
        { "PITA", "PITA" },
        { "AVC", "Avis de cruce" },
        { "ED", "E-documents" },
        { "AC", "Acuse de Cove" },
        { "AE", "Acuse de e-documents" },
        { "XCO", "XML COVE" },
        { "XDO", "XML DODA" },
        { "XPITA", "XML PITA" },
        { "OTRO", "Otro documento" },
        { "MV", "Manifestacion de valor" },
        { "HC", "Hoja de calculo" },
        { "IMM", "Autorizacion IMMEX" },
        { "PROS", "Autorizacion PROSEC" },
        { "CIVA", "Certificacion IVA" },
        { "USM", "Uso de marca" },
        { "C318", "Carta 3.1.8. Importacion" },
        { "CID", "Carta Datos de Identificacion Individual" },
        { "CA65", "Carta Art. 65 LA" },
        { "CNM", "Carta de excepcion de NOM" },
        { "RRNA", "RRNA's (cartas de uso dual)" },
        { "HON", "Cuenta de gastos (Honorarios de Agente aduanal)" },
        { "CAM", "Cuenta americana" },
        { "MA", "Maniobras (flete)" },
        { "GT", "Gastos terceros comprobados" },
        { "MCA", "Manejo, Custodia, Almacenajes" },
        { "DES", "Desconsolidacion de guias" },
        { "AP", "Archivo de envio de pago" },
        { "ERR", "Archivo de respuesta ERR" },
        { "ARP", "Archivo de respuesta de pago" },
        { "AM", "Archivo de validacion M" },
        { "PSE", "Poliza de seguro" },
        { "OC", "Orden de Compra" },
        { "REC", "Recibo de mercancias" },
        { "CFDI", "Factura fiscal (timbrada)" },
        { "XCFDI", "XML de factura fiscal" }
    };

    public static List<string> ObtenerClaves() => new(Catalogo.Keys);

    public static string ObtenerDescripcion(string clave) =>
        Catalogo.TryGetValue(clave, out var desc) ? desc : clave;
}

public class ExpedienteConsultaResult
{
    public bool Exitoso { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public List<Expediente> Expedientes { get; set; } = new();
    public int TotalRegistros { get; set; }
}

public class DocumentoExpedienteResult
{
    public bool Exitoso { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public List<DocumentoExpediente> Documentos { get; set; } = new();
}

public class ExpedienteCompletoResult
{
    public bool Exitoso { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public Expediente? Expediente { get; set; }
    public List<DocumentoFaltante> Documentos { get; set; } = new();
    public int TotalDocumentos => TiposDocumento.Catalogo.Count;
    public int DocumentosSubidos => Documentos.Count(d => d.TieneDocumento);
    public double PorcentajeCompletado => TotalDocumentos > 0 ? (DocumentosSubidos * 100.0 / TotalDocumentos) : 0;
}
