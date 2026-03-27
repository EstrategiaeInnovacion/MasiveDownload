namespace VucemDownloader.Models;

public class ManifestacionInfo
{
    public string NumeroOperacion { get; set; } = string.Empty;
    public string NumeroMv { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
    public string RfcSolicitante { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string TipoOperacion { get; set; } = string.Empty;
    public string NumeroPedimento { get; set; } = string.Empty;
    public decimal ValorTotal { get; set; }
    public string Cove { get; set; } = string.Empty;
    public string AcusePdfBase64 { get; set; } = string.Empty;
    public string XmlRespuesta { get; set; } = string.Empty;
}

public class ManifestacionDetalle
{
    public string NumeroOperacion { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
    public string RfcSolicitante { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string TipoOperacion { get; set; } = string.Empty;
    public string NumeroPedimento { get; set; } = string.Empty;
    public decimal ValorTotal { get; set; }
    public string Moneda { get; set; } = string.Empty;
    public string PaisOrigen { get; set; } = string.Empty;
    public string PaisDestino { get; set; } = string.Empty;
    public List<ManifestacionPartida> Partidas { get; set; } = new();
}

public class ManifestacionPartida
{
    public int NumeroPartida { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public decimal Cantidad { get; set; }
    public string UnidadMedida { get; set; } = string.Empty;
    public decimal ValorUnitario { get; set; }
    public decimal ValorTotal { get; set; }
    public string FraccionArancelaria { get; set; } = string.Empty;
    public string PaisOrigen { get; set; } = string.Empty;
}

public class ManifestacionConsultaResult
{
    public bool Exitoso { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public List<ManifestacionInfo> Manifestaciones { get; set; } = new();
    public int TotalRegistros { get; set; }
    public string RespuestaRaw { get; set; } = string.Empty;
}

public class ManifestacionDetalleResult
{
    public bool Exitoso { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public ManifestacionDetalle? Detalle { get; set; }
}

public class ManifestacionDescargaResult
{
    public bool Exitoso { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public byte[]? Contenido { get; set; }
    public string NombreArchivo { get; set; } = string.Empty;
}
