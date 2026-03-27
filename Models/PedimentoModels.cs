namespace VucemDownloader.Models;

public class PedimentoInfo
{
    public string NumeroPedimento { get; set; } = string.Empty;
    public DateTime FechaPago { get; set; }
    public string RfcImportador { get; set; } = string.Empty;
    public string Aduana { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public decimal ValorAduana { get; set; }
    public string TipoOperacion { get; set; } = string.Empty;
}

public class PedimentoDetalle
{
    public string NumeroPedimento { get; set; } = string.Empty;
    public DateTime FechaPago { get; set; }
    public string RfcImportador { get; set; } = string.Empty;
    public string Aduana { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public decimal ValorAduana { get; set; }
    public decimal ValorDolares { get; set; }
    public string TipoCambio { get; set; } = string.Empty;
    public string ClavePedimento { get; set; } = string.Empty;
    public string Regimen { get; set; } = string.Empty;
    public List<PedimentoFraccion> Fracciones { get; set; } = new();
}

public class PedimentoFraccion
{
    public int NumeroPartida { get; set; }
    public string Fraccion { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public decimal ValorAduana { get; set; }
    public decimal CantidadUM { get; set; }
    public string UnidadMedida { get; set; } = string.Empty;
    public decimal Preferencia { get; set; }
    public string TatePais { get; set; } = string.Empty;
}

public class PedimentoConsultaResult
{
    public bool Exitoso { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public List<PedimentoInfo> Pedimentos { get; set; } = new();
    public int TotalRegistros { get; set; }
    public string RespuestaRaw { get; set; } = string.Empty;
}

public class PedimentoDetalleResult
{
    public bool Exitoso { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public PedimentoDetalle? Detalle { get; set; }
}

public class PedimentoDescargaResult
{
    public bool Exitoso { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public byte[]? Contenido { get; set; }
    public string NombreArchivo { get; set; } = string.Empty;
}
