namespace VucemDownloader.Models;

public class CoveInfo
{
    public string NumeroOperacion { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
    public string RfcEmisor { get; set; } = string.Empty;
    public string RfcReceptor { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string TipoOperacion { get; set; } = string.Empty;
    public decimal ValorTotal { get; set; }
    public string Moneda { get; set; } = string.Empty;
}

public class CoveDetalle
{
    public string NumeroOperacion { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
    public string RfcEmisor { get; set; } = string.Empty;
    public string RfcReceptor { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string TipoOperacion { get; set; } = string.Empty;
    public decimal SubTotal { get; set; }
    public decimal Total { get; set; }
    public string Moneda { get; set; } = string.Empty;
    public string NumeroGuia { get; set; } = string.Empty;
    public string Pedimento { get; set; } = string.Empty;
    public List<CovePartida> Partidas { get; set; } = new();
}

public class CovePartida
{
    public int NumeroPartida { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public decimal Cantidad { get; set; }
    public string UnidadMedida { get; set; } = string.Empty;
    public decimal ValorUnitario { get; set; }
    public decimal ValorTotal { get; set; }
    public string FraccionArancelaria { get; set; } = string.Empty;
}

public class CoveConsultaResult
{
    public bool Exitoso { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public List<CoveInfo> Covess { get; set; } = new();
    public int TotalRegistros { get; set; }
    public string RespuestaRaw { get; set; } = string.Empty;
}

public class CoveDetalleResult
{
    public bool Exitoso { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public CoveDetalle? Detalle { get; set; }
}

public class CoveDescargaResult
{
    public bool Exitoso { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public byte[]? Contenido { get; set; }
    public string NombreArchivo { get; set; } = string.Empty;
}
