using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;

namespace VucemDownloader.Helpers;

public static class CertHelper
{
    public static (bool valido, string rfc, string mensaje) ValidarCertificadoYLlave(
        string rutaCer, string rutaKey, string password)
    {
        try
        {
            var cert = X509CertificateLoader.LoadCertificateFromFile(rutaCer);
            var (modulusCert, exponentCert) = ExtractPublicKeyDetails(cert);

            byte[] keyBytes = File.ReadAllBytes(rutaKey);
            AsymmetricKeyParameter privateKey = PrivateKeyFactory.DecryptKey(password.ToCharArray(), keyBytes);

            if (privateKey == null || !privateKey.IsPrivate)
            {
                return (false, string.Empty, "No se pudo descifrar la llave privada.");
            }

            AsymmetricKeyParameter publicKeyFromPrivate;
            Org.BouncyCastle.Math.BigInteger modulusKey;
            Org.BouncyCastle.Math.BigInteger exponentKey;

            if (privateKey is RsaPrivateCrtKeyParameters rsaPriv)
            {
                modulusKey = rsaPriv.Modulus;
                exponentKey = rsaPriv.PublicExponent;
                publicKeyFromPrivate = new RsaKeyParameters(false, modulusKey, exponentKey);
            }
            else if (privateKey is RsaKeyParameters rsaKey)
            {
                modulusKey = rsaKey.Modulus;
                exponentKey = rsaKey.Exponent;
                publicKeyFromPrivate = new RsaKeyParameters(false, modulusKey, exponentKey);
            }
            else
            {
                return (false, string.Empty, "El tipo de llave no es RSA.");
            }

            bool modulesEqual = modulusCert.Equals(modulusKey);
            bool exponentEqual = exponentCert.Equals(exponentKey);

            if (!modulesEqual || !exponentEqual)
            {
                return (false, string.Empty, "La llave no corresponde al certificado.");
            }

            string rfc = GetRFCFromCert(cert);
            return (true, rfc, "Certificado y llave válidos.");
        }
        catch (Exception ex)
        {
            return (false, string.Empty, $"Error: {ex.Message}");
        }
    }

    public static X509Certificate2 CargarCertificado(string rutaCer)
    {
        return X509CertificateLoader.LoadCertificateFromFile(rutaCer);
    }

    public static AsymmetricKeyParameter CargarLlavePrivada(string rutaKey, string password)
    {
        byte[] keyBytes = File.ReadAllBytes(rutaKey);
        return PrivateKeyFactory.DecryptKey(password.ToCharArray(), keyBytes);
    }

    public static (Org.BouncyCastle.Math.BigInteger modulus, Org.BouncyCastle.Math.BigInteger exponent) ExtractPublicKeyDetails(X509Certificate2 cert)
    {
        using var rsa = cert.GetRSAPublicKey();
        if (rsa != null)
        {
            RSAParameters rsaParams = rsa.ExportParameters(false);
            return (
                new Org.BouncyCastle.Math.BigInteger(1, rsaParams.Modulus!),
                new Org.BouncyCastle.Math.BigInteger(1, rsaParams.Exponent!)
            );
        }
        throw new NotSupportedException("Solo se soportan llaves RSA.");
    }

    public static string GetRFCFromCert(X509Certificate2 cert)
    {
        string subject = cert.Subject;

        if (subject.Contains("SERIALNUMBER="))
        {
            int start = subject.IndexOf("SERIALNUMBER=") + 13;
            int end = subject.IndexOf(',', start);
            if (end == -1) end = subject.Length;
            string serial = subject.Substring(start, end - start).Trim();
            if (serial.Length >= 12 && serial.Length <= 13)
                return serial;
        }

        var parts = subject.Split(',');
        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            if (trimmed.StartsWith("OID.2.5.4.5="))
                return trimmed.Substring(11);
            if (trimmed.StartsWith("2.5.4.5="))
                return trimmed.Substring(8);
            if (trimmed.StartsWith("SERIALNUMBER="))
                return trimmed.Substring(12);
        }

        return cert.GetNameInfo(X509NameType.SimpleName, false).TrimStart('/');
    }

    public static bool CertificadoVigente(X509Certificate2 cert)
    {
        var ahora = DateTime.Now;
        return ahora >= cert.NotBefore && ahora <= cert.NotAfter;
    }

    public static string GetInformacionCertificado(X509Certificate2 cert)
    {
        return $"Válido desde: {cert.NotBefore:dd/MM/yyyy}\n" +
               $"Válido hasta: {cert.NotAfter:dd/MM/yyyy}\n" +
               $"Sujeto: {cert.Subject}\n" +
               $"Emisor: {cert.Issuer}";
    }
}
