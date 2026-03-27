using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using VucemDownloader.Models;

namespace VucemDownloader.Services;

public class ExpedienteService
{
    private readonly string _carpetaBase;
    private readonly string _archivoDatos;
    private List<Expediente> _expedientes = new();
    private Dictionary<int, List<DocumentoExpediente>> _documentos = new();

    public ExpedienteService()
    {
        _carpetaBase = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "VucemDownloader", "Expedientes");
        
        _archivoDatos = Path.Combine(_carpetaBase, "datos.json");
        
        Directory.CreateDirectory(_carpetaBase);
        CargarDatos();
    }

    private void CargarDatos()
    {
        try
        {
            if (File.Exists(_archivoDatos))
            {
                var json = File.ReadAllText(_archivoDatos);
                var datos = JsonSerializer.Deserialize<ExpedienteDatos>(json);
                if (datos != null)
                {
                    _expedientes = datos.Expedientes ?? new List<Expediente>();
                    _documentos = datos.Documentos ?? new Dictionary<int, List<DocumentoExpediente>>();
                }
            }
        }
        catch
        {
            _expedientes = new List<Expediente>();
            _documentos = new Dictionary<int, List<DocumentoExpediente>>();
        }
    }

    private void GuardarDatos()
    {
        var datos = new ExpedienteDatos
        {
            Expedientes = _expedientes,
            Documentos = _documentos
        };
        var json = JsonSerializer.Serialize(datos, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_archivoDatos, json);
    }

    public ExpedienteCompletoResult CrearExpediente(string nombre, string rfc, string aduana)
    {
        try
        {
            var id = _expedientes.Any() ? _expedientes.Max(e => e.Id) + 1 : 1;
            var carpetaExpediente = Path.Combine(_carpetaBase, $"EXP_{id}_{nombre.Replace(" ", "_")}");
            Directory.CreateDirectory(carpetaExpediente);

            var expediente = new Expediente
            {
                Id = id,
                Nombre = nombre,
                Rfc = rfc,
                Aduana = aduana,
                FechaCreacion = DateTime.Now,
                Estado = "En proceso",
                RutaCarpeta = carpetaExpediente
            };

            _expedientes.Add(expediente);
            _documentos[id] = new List<DocumentoExpediente>();
            GuardarDatos();

            return ObtenerExpedienteCompleto(id);
        }
        catch (Exception ex)
        {
            return new ExpedienteCompletoResult
            {
                Exitoso = false,
                Mensaje = "Error al crear expediente: " + ex.Message
            };
        }
    }

    public ExpedienteConsultaResult ObtenerExpedientes(string? filtro = null)
    {
        var resultado = new ExpedienteConsultaResult();

        try
        {
            var lista = _expedientes.AsEnumerable();

            if (!string.IsNullOrEmpty(filtro))
            {
                lista = lista.Where(e => 
                    e.Nombre.Contains(filtro, StringComparison.OrdinalIgnoreCase) ||
                    e.Rfc.Contains(filtro, StringComparison.OrdinalIgnoreCase));
            }

            resultado.Expedientes = lista.OrderByDescending(e => e.FechaCreacion).ToList();
            resultado.TotalRegistros = resultado.Expedientes.Count;
            resultado.Exitoso = true;
            resultado.Mensaje = $"Se encontraron {resultado.TotalRegistros} expedientes";
        }
        catch (Exception ex)
        {
            resultado.Exitoso = false;
            resultado.Mensaje = "Error al consultar expedientes: " + ex.Message;
        }

        return resultado;
    }

    public ExpedienteCompletoResult ObtenerExpedienteCompleto(int id)
    {
        var resultado = new ExpedienteCompletoResult();

        try
        {
            var expediente = _expedientes.FirstOrDefault(e => e.Id == id);
            if (expediente == null)
            {
                resultado.Exitoso = false;
                resultado.Mensaje = "Expediente no encontrado";
                return resultado;
            }

            var docsSubidos = _documentos.GetValueOrDefault(id, new List<DocumentoExpediente>());
            var tiposConDocs = docsSubidos.Select(d => d.TipoDocumento).ToHashSet();

            var documentos = new List<DocumentoFaltante>();
            foreach (var tipo in TiposDocumento.Catalogo)
            {
                var doc = docsSubidos.FirstOrDefault(d => d.TipoDocumento == tipo.Key);
                documentos.Add(new DocumentoFaltante
                {
                    TipoDocumento = tipo.Key,
                    Descripcion = tipo.Value,
                    TieneDocumento = doc != null,
                    NombreArchivo = doc?.NombreArchivo
                });
            }

            resultado.Expediente = expediente;
            resultado.Documentos = documentos;
            resultado.Exitoso = true;
            resultado.Mensaje = $"Expediente: {expediente.Nombre}";
        }
        catch (Exception ex)
        {
            resultado.Exitoso = false;
            resultado.Mensaje = "Error al obtener expediente: " + ex.Message;
        }

        return resultado;
    }

    public DocumentoExpedienteResult SubirDocumento(int expedienteId, string rutaArchivo, string? tipoForzado = null)
    {
        var resultado = new DocumentoExpedienteResult();

        try
        {
            var expediente = _expedientes.FirstOrDefault(e => e.Id == expedienteId);
            if (expediente == null)
            {
                resultado.Exitoso = false;
                resultado.Mensaje = "Expediente no encontrado";
                return resultado;
            }

            if (!File.Exists(rutaArchivo))
            {
                resultado.Exitoso = false;
                resultado.Mensaje = "El archivo no existe";
                return resultado;
            }

            var extension = Path.GetExtension(rutaArchivo).ToLower();
            if (extension != ".pdf" && extension != ".xml" && extension != ".jpg" && extension != ".png")
            {
                resultado.Exitoso = false;
                resultado.Mensaje = $"Tipo de archivo no permitido: {extension}";
                return resultado;
            }

            var tipoDetectado = tipoForzado ?? DetectarTipoDocumento(rutaArchivo);
            
            var docsExistentes = _documentos.GetValueOrDefault(expedienteId, new List<DocumentoExpediente>());
            var docExistente = docsExistentes.FirstOrDefault(d => d.TipoDocumento == tipoDetectado);
            if (docExistente != null)
            {
                if (File.Exists(docExistente.RutaArchivo))
                {
                    File.Delete(docExistente.RutaArchivo);
                }
                docsExistentes.Remove(docExistente);
            }

            var nombreArchivo = $"{tipoDetectado}_{DateTime.Now:yyyyMMdd_HHmmss}{extension}";
            var rutaDestino = Path.Combine(expediente.RutaCarpeta, nombreArchivo);

            File.Copy(rutaArchivo, rutaDestino, true);

            var hash = CalcularHash(rutaDestino);
            var fileInfo = new FileInfo(rutaDestino);

            var doc = new DocumentoExpediente
            {
                Id = docsExistentes.Any() ? docsExistentes.Max(d => d.Id) + 1 : 1,
                ExpedienteId = expedienteId,
                TipoDocumento = tipoDetectado,
                NombreOriginal = Path.GetFileName(rutaArchivo),
                NombreArchivo = nombreArchivo,
                RutaArchivo = rutaDestino,
                FechaSubida = DateTime.Now,
                Hash = hash,
                Tamano = fileInfo.Length
            };

            docsExistentes.Add(doc);
            _documentos[expedienteId] = docsExistentes;

            VerificarEstadoExpediente(expedienteId);
            GuardarDatos();

            resultado.Documentos = docsExistentes;
            resultado.Exitoso = true;
            resultado.Mensaje = $"Documento {tipoDetectado} subido correctamente";
        }
        catch (Exception ex)
        {
            resultado.Exitoso = false;
            resultado.Mensaje = "Error al subir documento: " + ex.Message;
        }

        return resultado;
    }

    public DocumentoExpedienteResult SubirMultiplesDocumentos(int expedienteId, List<string> rutasArchivos)
    {
        var resultado = new DocumentoExpedienteResult();
        var docsAgregados = new List<DocumentoExpediente>();

        foreach (var ruta in rutasArchivos)
        {
            var result = SubirDocumento(expedienteId, ruta);
            if (result.Exitoso)
            {
                docsAgregados.AddRange(result.Documentos);
            }
        }

        resultado.Documentos = docsAgregados;
        resultado.Exitoso = docsAgregados.Count > 0;
        resultado.Mensaje = $"Se procesaron {docsAgregados.Count} de {rutasArchivos.Count} archivos";

        return resultado;
    }

    public DocumentoExpedienteResult EliminarDocumento(int expedienteId, string tipoDocumento)
    {
        var resultado = new DocumentoExpedienteResult();

        try
        {
            var docs = _documentos.GetValueOrDefault(expedienteId, new List<DocumentoExpediente>());
            var doc = docs.FirstOrDefault(d => d.TipoDocumento == tipoDocumento);

            if (doc == null)
            {
                resultado.Exitoso = false;
                resultado.Mensaje = "Documento no encontrado";
                return resultado;
            }

            if (File.Exists(doc.RutaArchivo))
            {
                File.Delete(doc.RutaArchivo);
            }

            docs.Remove(doc);
            _documentos[expedienteId] = docs;

            VerificarEstadoExpediente(expedienteId);
            GuardarDatos();

            resultado.Documentos = docs;
            resultado.Exitoso = true;
            resultado.Mensaje = $"Documento {tipoDocumento} eliminado";
        }
        catch (Exception ex)
        {
            resultado.Exitoso = false;
            resultado.Mensaje = "Error al eliminar documento: " + ex.Message;
        }

        return resultado;
    }

    public (bool Exitoso, string Mensaje, string? RutaZip) GenerarZip(int expedienteId, string? rutaPersonalizada = null)
    {
        try
        {
            var expediente = _expedientes.FirstOrDefault(e => e.Id == expedienteId);
            if (expediente == null)
            {
                return (false, "Expediente no encontrado", null);
            }

            var docs = _documentos.GetValueOrDefault(expedienteId, new List<DocumentoExpediente>());
            if (!docs.Any())
            {
                return (false, "No hay documentos para comprimir", null);
            }

            var nombreZip = $"Expediente_{expediente.Nombre.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmmss}.zip";
            var rutaZip = string.IsNullOrEmpty(rutaPersonalizada)
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), nombreZip)
                : Path.Combine(rutaPersonalizada, nombreZip);

            if (File.Exists(rutaZip))
            {
                File.Delete(rutaZip);
            }

            using var zip = ZipFile.Open(rutaZip, ZipArchiveMode.Create);

            var indice = new System.Text.StringBuilder();
            indice.AppendLine("INDICE DE EXPEDIENTE");
            indice.AppendLine("====================");
            indice.AppendLine($"Proyecto: {expediente.Nombre}");
            indice.AppendLine($"RFC: {expediente.Rfc}");
            indice.AppendLine($"Aduana: {expediente.Aduana}");
            indice.AppendLine($"Fecha: {expediente.FechaCreacion:dd/MM/yyyy}");
            indice.AppendLine($"Documentos: {docs.Count}");
            indice.AppendLine();
            indice.AppendLine("DOCUMENTOS:");
            indice.AppendLine("-----------");

            foreach (var doc in docs.OrderBy(d => d.TipoDocumento))
            {
                var descripcion = TiposDocumento.ObtenerDescripcion(doc.TipoDocumento);
                indice.AppendLine($"[{doc.TipoDocumento}] {descripcion}");
                indice.AppendLine($"       Archivo: {doc.NombreOriginal}");
                indice.AppendLine($"       Fecha: {doc.FechaSubida:dd/MM/yyyy HH:mm}");
                indice.AppendLine($"       Tamano: {doc.Tamano / 1024.0:F2} KB");
                indice.AppendLine();

                if (File.Exists(doc.RutaArchivo))
                {
                    zip.CreateEntryFromFile(doc.RutaArchivo, $"{doc.TipoDocumento}/{doc.NombreArchivo}");
                }
            }

            var indiceEntry = zip.CreateEntry("indice.txt");
            using var writer = new StreamWriter(indiceEntry.Open());
            writer.Write(indice.ToString());

            expediente.RutaZip = rutaZip;
            expediente.Estado = "Comprimido";
            expediente.FechaCierre = DateTime.Now;
            GuardarDatos();

            return (true, $"ZIP generado: {rutaZip}", rutaZip);
        }
        catch (Exception ex)
        {
            return (false, "Error al generar ZIP: " + ex.Message, null);
        }
    }

    public (bool Exitoso, string Mensaje) EliminarExpediente(int expedienteId)
    {
        try
        {
            var expediente = _expedientes.FirstOrDefault(e => e.Id == expedienteId);
            if (expediente == null)
            {
                return (false, "Expediente no encontrado");
            }

            if (!string.IsNullOrEmpty(expediente.RutaCarpeta) && Directory.Exists(expediente.RutaCarpeta))
            {
                Directory.Delete(expediente.RutaCarpeta, true);
            }

            _expedientes.Remove(expediente);
            _documentos.Remove(expedienteId);
            GuardarDatos();

            return (true, "Expediente eliminado");
        }
        catch (Exception ex)
        {
            return (false, "Error al eliminar expediente: " + ex.Message);
        }
    }

    private string DetectarTipoDocumento(string rutaArchivo)
    {
        var nombre = Path.GetFileNameWithoutExtension(rutaArchivo).ToUpper();
        
        var mapeo = new Dictionary<string, List<string>>
        {
            { "PED", new() { "PED", "PEDIMENTO" } },
            { "SIM", new() { "SIM", "SIMPLIFICADO" } },
            { "FAC", new() { "FAC", "FACTURA", "INVOICE" } },
            { "PKL", new() { "PKL", "PACKING", "LISTA", "EMPAQUE" } },
            { "GUIA", new() { "GUIA", "GUIDE", "HBL", "MBL", "AWB", "BL", "AWB" } },
            { "CO", new() { "CO", "CERTIFICADO", "ORIGEN", "CERTIFICATE" } },
            { "COV", new() { "COV", "COVE" } },
            { "DOD", new() { "DOD", "DODA" } },
            { "PITA", new() { "PITA" } },
            { "AC", new() { "ACUSE", "AC", "ACUSE_COVE" } },
            { "XCO", new() { "XCO", "XML_COVE" } },
            { "CFDI", new() { "CFDI", "FACTURA_ELECTRONICA", "TIMBRADA" } },
            { "XCFDI", new() { "XCFDI", "XML_CFDI" } },
            { "MV", new() { "MV", "MANIFESTACION", "VALOR", "E2" } },
            { "OC", new() { "OC", "ORDEN_COMPRA", "PO" } },
            { "REC", new() { "REC", "RECIBO", "RECEIPT" } }
        };

        foreach (var (tipo, keywords) in mapeo)
        {
            if (keywords.Any(k => nombre.Contains(k)))
            {
                return tipo;
            }
        }

        return "OTRO";
    }

    private string CalcularHash(string rutaArchivo)
    {
        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(rutaArchivo);
        var hash = sha256.ComputeHash(stream);
        return Convert.ToHexString(hash)[..16];
    }

    private void VerificarEstadoExpediente(int expedienteId)
    {
        var expediente = _expedientes.FirstOrDefault(e => e.Id == expedienteId);
        if (expediente == null) return;

        var docs = _documentos.GetValueOrDefault(expedienteId, new List<DocumentoExpediente>());
        var tiposUnicos = docs.Select(d => d.TipoDocumento).Distinct().Count();
        var totalTipos = TiposDocumento.Catalogo.Count;

        if (tiposUnicos == 0)
            expediente.Estado = "En proceso";
        else if (tiposUnicos >= totalTipos * 0.8)
            expediente.Estado = "Casi completo";
        else
            expediente.Estado = "En proceso";
    }

    private class ExpedienteDatos
    {
        public List<Expediente> Expedientes { get; set; } = new();
        public Dictionary<int, List<DocumentoExpediente>> Documentos { get; set; } = new();
    }
}
