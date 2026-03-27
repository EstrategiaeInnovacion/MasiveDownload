using System.Security.Cryptography.X509Certificates;
using Org.BouncyCastle.Crypto;

namespace VucemDownloader.Models;

public class SessionInfo
{
    public string RFC { get; set; } = string.Empty;
    public X509Certificate2? Certificado { get; set; }
    public AsymmetricKeyParameter? LlavePrivada { get; set; }
    public string WebservicePassword { get; set; } = string.Empty;
    public string? Token { get; set; }
    public DateTime? TokenExpiracion { get; set; }
}
