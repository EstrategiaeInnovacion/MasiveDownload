using System;
using System.IO;
using System.Text.Json;

namespace VucemDownloader.Helpers;

public static class FileHelper
{
    private static readonly string AppDataPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "VucemDownloader");

    public static string ObtenerCarpetaDescargas(string? rutaPersonalizada = null)
    {
        if (!string.IsNullOrEmpty(rutaPersonalizada) && Directory.Exists(rutaPersonalizada))
            return rutaPersonalizada;

        string carpetaDefault = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "VucemDownloader", "Descargas");

        if (!Directory.Exists(carpetaDefault))
            Directory.CreateDirectory(carpetaDefault);

        return carpetaDefault;
    }

    public static string GenerarRutaArchivo(string carpetaBase, string tipo, string numero, string extension, DateTime fecha)
    {
        string estructura = "{TIPO}/{YYYY}/{MM}_{MMM}";
        string ruta = estructura
            .Replace("{TIPO}", tipo)
            .Replace("{YYYY}", fecha.Year.ToString())
            .Replace("{MM}", fecha.Month.ToString("D2"))
            .Replace("{MMM}", fecha.ToString("MMMM"));

        ruta = Path.Combine(carpetaBase, ruta);
        
        if (!Directory.Exists(ruta))
            Directory.CreateDirectory(ruta);

        string nombreArchivo = $"{tipo}_{numero}_{fecha:yyyyMMdd}.{extension}";
        return Path.Combine(ruta, nombreArchivo);
    }

    public static bool GuardarArchivo(byte[] contenido, string ruta)
    {
        try
        {
            string directorio = Path.GetDirectoryName(ruta) ?? "";
            if (!Directory.Exists(directorio))
                Directory.CreateDirectory(directorio);

            File.WriteAllBytes(ruta, contenido);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool GuardarTexto(string contenido, string ruta)
    {
        try
        {
            string directorio = Path.GetDirectoryName(ruta) ?? "";
            if (!Directory.Exists(directorio))
                Directory.CreateDirectory(directorio);

            File.WriteAllText(ruta, contenido);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static void AbrirCarpetaEnExplorador(string ruta)
    {
        if (Directory.Exists(ruta))
        {
            System.Diagnostics.Process.Start("explorer.exe", ruta);
        }
    }

    public static void AbrirArchivo(string ruta)
    {
        if (File.Exists(ruta))
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = ruta,
                UseShellExecute = true
            });
        }
    }

    public static long GetTamanoCarpeta(string ruta)
    {
        if (!Directory.Exists(ruta))
            return 0;

        return new DirectoryInfo(ruta).GetFiles("*", SearchOption.AllDirectories)
            .Sum(f => f.Length);
    }

    public static string FormatearTamano(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    public static string[] ObtenerMesesEspanol()
    {
        return new[] { "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio",
                        "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre" };
    }
}
