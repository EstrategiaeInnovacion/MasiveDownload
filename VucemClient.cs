using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;

namespace VucemDownloader
{
    public class VucemClient : IDisposable
    {
        private const string SoapUrl = "https://cfdiws-servicio.cloudapp.net/v2/cfdi40.svc";
        
        private X509Certificate2 _certificado;
        private AsymmetricKeyParameter _llavePrivada;
        private readonly HttpClient _httpClient;

        public VucemClient(X509Certificate2 certificado, AsymmetricKeyParameter llavePrivada)
        {
            _certificado = certificado;
            _llavePrivada = llavePrivada;
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromMinutes(5);
        }

        public async Task<DescargaResult> DescargarCfdisAsync(DateTime fechaInicio, DateTime fechaFin, string rfcEmisor = "", string estado = "Todos")
        {
            try
            {
                string xmlRequest = GenerarSoapRequest(fechaInicio, fechaFin, rfcEmisor, estado);
                string response = await EnviarSoapRequestAsync(xmlRequest);
                return ParsearRespuesta(response);
            }
            catch (Exception ex)
            {
                return new DescargaResult
                {
                    Exitoso = false,
                    Mensaje = ex.Message
                };
            }
        }

        public async Task<ConsultaResult> ConsultarCfdiAsync(string uuid)
        {
            try
            {
                string xmlRequest = GenerarSoapRequestConsulta(uuid);
                string response = await EnviarSoapRequestAsync(xmlRequest);
                return ParsearRespuestaConsulta(response);
            }
            catch (Exception ex)
            {
                return new ConsultaResult
                {
                    Exitoso = false,
                    Mensaje = ex.Message
                };
            }
        }

        public async Task<CancelacionResult> SolicitarCancelacionAsync(string uuid, string motivo)
        {
            try
            {
                string xmlRequest = GenerarSoapRequestCancelacion(uuid, motivo);
                string response = await EnviarSoapRequestAsync(xmlRequest);
                return ParsearRespuestaCancelacion(response);
            }
            catch (Exception ex)
            {
                return new CancelacionResult
                {
                    Exitoso = false,
                    Mensaje = ex.Message
                };
            }
        }

        public ValidacionResult ValidarCfdi(string rutaXml)
        {
            try
            {
                if (!File.Exists(rutaXml))
                {
                    return new ValidacionResult
                    {
                        EsValido = false,
                        Mensaje = "El archivo no existe."
                    };
                }

                XDocument doc = XDocument.Load(rutaXml);
                XNamespace cfdi = "http://www.sat.gob.mx/cfd/4";
                XNamespace tfd = "http://www.sat.gob.mx/TimbreFiscalDigital";
                
                var comprobante = doc.Root;
                if (comprobante == null)
                {
                    return new ValidacionResult
                    {
                        EsValido = false,
                        Mensaje = "XML no válido - Sin elemento raíz."
                    };
                }

                string uuid = comprobante.Descendants(tfd + "TimbreFiscalDigital")
                                     .FirstOrDefault()?.Attribute("UUID")?.Value ?? "";
                
                string rfcEmisor = comprobante.Attribute("RfcEmisor")?.Value ?? 
                                   comprobante.Element(cfdi + "Emisor")?.Attribute("Rfc")?.Value ?? "";
                
                string rfcReceptor = comprobante.Attribute("RfcReceptor")?.Value ?? 
                                     comprobante.Element(cfdi + "Receptor")?.Attribute("Rfc")?.Value ?? "";
                
                string total = comprobante.Attribute("Total")?.Value ?? "";
                string sello = comprobante.Attribute("Sello")?.Value ?? "";

                bool tieneSello = !string.IsNullOrEmpty(sello);
                
                string info = $"UUID: {uuid}\n" +
                             $"RFC Emisor: {rfcEmisor}\n" +
                             $"RFC Receptor: {rfcReceptor}\n" +
                             $"Total: ${total}\n" +
                             $"Tiene Sello Digital: {(tieneSello ? "Sí" : "No")}";

                return new ValidacionResult
                {
                    EsValido = tieneSello,
                    Mensaje = info,
                    Uuid = uuid,
                    RfcEmisor = rfcEmisor,
                    RfcReceptor = rfcReceptor
                };
            }
            catch (Exception ex)
            {
                return new ValidacionResult
                {
                    EsValido = false,
                    Mensaje = $"Error al validar: {ex.Message}"
                };
            }
        }

        private string GenerarSoapRequest(DateTime fechaInicio, DateTime fechaFin, string rfcEmisor, string estado)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(@"<?xml version=""1.0"" encoding=""UTF-8""?>");
            sb.AppendLine(@"<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" ");
            sb.AppendLine(@"xmlns:tem=""http://tempuri.org/"">");
            sb.AppendLine(@"<soap:Header/>");
            sb.AppendLine(@"<soap:Body>");
            sb.AppendLine(@"<tem:DescargaMasiva>");
            sb.AppendLine("<tem:fechaInicial>" + fechaInicio.ToString("yyyy-MM-dd") + "</tem:fechaInicial>");
            sb.AppendLine("<tem:fechaFinal>" + fechaFin.ToString("yyyy-MM-dd") + "</tem:fechaFinal>");
            
            if (!string.IsNullOrEmpty(rfcEmisor))
                sb.AppendLine("<tem:rfcEmisor>" + rfcEmisor + "</tem:rfcEmisor>");
            
            sb.AppendLine("<tem:estado>" + estado + "</tem:estado>");
            sb.AppendLine("</tem:DescargaMasiva>");
            sb.AppendLine("</soap:Body>");
            sb.AppendLine("</soap:Envelope>");
            return sb.ToString();
        }

        private string GenerarSoapRequestConsulta(string uuid)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(@"<?xml version=""1.0"" encoding=""UTF-8""?>");
            sb.AppendLine(@"<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" ");
            sb.AppendLine(@"xmlns:tem=""http://tempuri.org/"">");
            sb.AppendLine(@"<soap:Header/>");
            sb.AppendLine(@"<soap:Body>");
            sb.AppendLine(@"<tem:ConsultarCFDI>");
            sb.AppendLine("<tem:uuid>" + uuid + "</tem:uuid>");
            sb.AppendLine("</tem:ConsultarCFDI>");
            sb.AppendLine("</soap:Body>");
            sb.AppendLine("</soap:Envelope>");
            return sb.ToString();
        }

        private string GenerarSoapRequestCancelacion(string uuid, string motivo)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(@"<?xml version=""1.0"" encoding=""UTF-8""?>");
            sb.AppendLine(@"<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" ");
            sb.AppendLine(@"xmlns:tem=""http://tempuri.org/"">");
            sb.AppendLine(@"<soap:Header/>");
            sb.AppendLine(@"<soap:Body>");
            sb.AppendLine(@"<tem:SolicitarCancelacion>");
            sb.AppendLine("<tem:uuid>" + uuid + "</tem:uuid>");
            sb.AppendLine("<tem:motivo>" + motivo + "</tem:motivo>");
            sb.AppendLine("</tem:SolicitarCancelacion>");
            sb.AppendLine("</soap:Body>");
            sb.AppendLine("</soap:Envelope>");
            return sb.ToString();
        }

        private async Task<string> EnviarSoapRequestAsync(string xmlRequest)
        {
            var content = new StringContent(xmlRequest, Encoding.UTF8, "text/xml");
            content.Headers.Add("SOAPAction", "http://tempuri.org/IDatoservice/DescargaMasiva");
            
            var response = await _httpClient.PostAsync(SoapUrl, content);
            return await response.Content.ReadAsStringAsync();
        }

        private DescargaResult ParsearRespuesta(string response)
        {
            return new DescargaResult
            {
                Exitoso = true,
                Mensaje = "Respuesta recibida - Funcionalidad en desarrollo",
                Detalle = response
            };
        }

        private ConsultaResult ParsearRespuestaConsulta(string response)
        {
            return new ConsultaResult
            {
                Exitoso = true,
                Mensaje = "Respuesta recibida - Funcionalidad en desarrollo"
            };
        }

        private CancelacionResult ParsearRespuestaCancelacion(string response)
        {
            return new CancelacionResult
            {
                Exitoso = true,
                Mensaje = "Respuesta recibida - Funcionalidad en desarrollo"
            };
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }

    public class DescargaResult
    {
        public bool Exitoso { get; set; }
        public string Mensaje { get; set; } = "";
        public string Detalle { get; set; } = "";
    }

    public class ConsultaResult
    {
        public bool Exitoso { get; set; }
        public string Mensaje { get; set; } = "";
    }

    public class CancelacionResult
    {
        public bool Exitoso { get; set; }
        public string Mensaje { get; set; } = "";
    }

    public class ValidacionResult
    {
        public bool EsValido { get; set; }
        public string Mensaje { get; set; } = "";
        public string Uuid { get; set; } = "";
        public string RfcEmisor { get; set; } = "";
        public string RfcReceptor { get; set; } = "";
    }
}
