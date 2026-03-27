# PLAN DE DESARROLLO - VUCEM Suite
## Sistema de Descarga Masiva de Expedientes VUCEM

---

## RESUMEN EJECUTIVO

**Objetivo:** Crear una aplicación de escritorio para descargar masivamente COVEs, Manifestaciones de Valor (E2) y Pedimentos desde la VUCEM.

**Stack Tecnológico:**
- .NET 10 / WPF
- BouncyCastle para firmas digitales
- SOAP Web Services
- Certificado FIEL para autenticación

**Estado Actual:** 15% completado (base de autenticación y licencia)

---

## FASE 1: Análisis y Estructura Base
**Duración estimada:** 1-2 días

### 1.1 Documentación de Web Services VUCEM
- [ ] Estudiar WSDLs oficiales de VUCEM
- [ ] Documentar endpoints y operaciones disponibles
- [ ] Mapear estructura de requests/responses

### 1.2 Reorganizar Estructura del Proyecto
```
MasiveDownload/
├── App.xaml(.cs)
├── MainWindow.xaml(.cs)
├── LoginWindow.xaml(.cs)
├── LicenseWindow.xaml(.cs)
├── CredentialManager.cs
├── Models/
│   ├── SessionInfo.cs
│   ├── CoveResult.cs
│   ├── ManifestacionResult.cs
│   └── PedimentoResult.cs
├── Services/
│   ├── VucemAuthService.cs      (autenticación/token)
│   ├── VucemCoveClient.cs       (COVE operations)
│   ├── VucemManifestacionClient.cs  (E2 operations)
│   ├── VucemPedimentoClient.cs  (pedimentos)
│   └── FileDownloadService.cs    (guardado archivos)
├── ViewModels/
│   ├── MainViewModel.cs
│   ├── CoveViewModel.cs
│   ├── ManifestacionViewModel.cs
│   └── SettingsViewModel.cs
├── Views/
│   ├── CovesView.xaml(.cs)
│   ├── ManifestacionesView.xaml(.cs)
│   ├── PedimentosView.xaml(.cs)
│   └── SettingsView.xaml(.cs)
├── Helpers/
│   ├── SoapHelper.cs
│   ├── XmlHelper.cs
│   └── FileHelper.cs
└── Resources/
    └── Styles/
```

### 1.3 Refactorizar VucemClient.cs Existente
- Mover a `Services/VucemClient.cs`
- Separar en servicios específicos por módulo

---

## FASE 2: Implementar Cliente VUCEM - COVE
**Duración estimada:** 3-4 días

### 2.1 Servicio de Autenticación
```csharp
public class VucemAuthService
{
    Task<string> ObtenerTokenAsync(string rfc, X509Certificate2 cert, AsymmetricKeyParameter privateKey);
    Task<bool> ValidarTokenAsync(string token);
}
```

### 2.2 Cliente COVE
```csharp
public class VucemCoveClient
{
    Task<CoveConsultaResult> ConsultarCovesAsync(DateTime fechaInicio, DateTime fechaFin, string rfcEmisor = "");
    Task<CoveDetalleResult> ObtenerDetalleCoveAsync(string numeroOperacion);
    Task<byte[]> DescargarPdfCoveAsync(string numeroOperacion);
    Task<byte[]> DescargarXmlCoveAsync(string numeroOperacion);
}
```

### 2.3 Operaciones SOAP Requeridas
- [ ] `consultarCove` - Búsqueda por rango de fechas
- [ ] `obtenerDetalleCove` - Datos completos del COVE
- [ ] `descargarPdfCove` - PDF del documento
- [ ] `descargarXmlCove` - XML original

### 2.4 Modelos de Datos COVE
```csharp
public class CoveInfo
{
    public string NumeroOperacion { get; set; }
    public DateTime FechaCreacion { get; set; }
    public string RfcEmisor { get; set; }
    public string RfcReceptor { get; set; }
    public string Estado { get; set; }
    public string TipoOperacion { get; set; }
    public decimal ValorTotal { get; set; }
}

public class CoveDetalle
{
    // Información completa del COVE
    // Productos, cantidades, valores, etc.
}
```

---

## FASE 3: Implementar Cliente VUCEM - Manifestaciones de Valor
**Duración estimada:** 3-4 días

### 3.1 Cliente Manifestaciones
```csharp
public class VucemManifestacionClient
{
    Task<ManifestacionConsultaResult> ConsultarManifestacionesAsync(DateTime fechaInicio, DateTime fechaFin, string rfc = "");
    Task<ManifestacionDetalleResult> ObtenerDetalleManifestacionAsync(string numeroOperacion);
    Task<byte[]> DescargarPdfManifestacionAsync(string numeroOperacion);
    Task<byte[]> DescargarXmlManifestacionAsync(string numeroOperacion);
}
```

### 3.2 Operaciones SOAP Requeridas
- [ ] `consultarManifestacion` - Búsqueda por rango de fechas
- [ ] `obtenerDetalleManifestacion` - Datos completos del E2
- [ ] `descargarPdfManifestacion` - PDF del documento
- [ ] `descargarXmlManifestacion` - XML original

### 3.3 Modelos de Datos Manifestación
```csharp
public class ManifestacionInfo
{
    public string NumeroOperacion { get; set; }
    public DateTime FechaCreacion { get; set; }
    public string RfcSolicitante { get; set; }
    public string Estado { get; set; }
    public string TipoOperacion { get; set; }
    public string NumeroPedimento { get; set; }
}

public class ManifestacionDetalle
{
    // Datos completos de la manifestación E2
    // Partidas, valores, identificaciones, etc.
}
```

---

## FASE 4: Implementar Cliente VUCEM - Pedimentos
**Duración estimada:** 2-3 días

### 4.1 Cliente Pedimentos
```csharp
public class VucemPedimentoClient
{
    Task<PedimentoConsultaResult> ConsultarPedimentosAsync(DateTime fechaInicio, DateTime fechaFin, string rfc = "");
    Task<PedimentoDetalleResult> ObtenerDetallePedimentoAsync(string numeroPedimento);
    Task<byte[]> DescargarPdfPedimentoAsync(string numeroPedimento);
}
```

### 4.2 Modelos de Datos Pedimento
```csharp
public class PedimentoInfo
{
    public string NumeroPedimento { get; set; }
    public DateTime FechaPago { get; set; }
    public string RfcImportador { get; set; }
    public string Aduana { get; set; }
    public string Estado { get; set; }
    public decimal ValorAduana { get; set; }
}
```

---

## FASE 5: Rediseñar MainWindow.xaml UI Completa
**Duración estimada:** 3-4 días

### 5.1 Layout Principal
```
┌─────────────────────────────────────────────────────────┐
│  VUCEM Suite - [RFC]     [Estado Sesión]    [Config]   │
├─────────────────────────────────────────────────────────┤
│  [COVEs] [Manifestaciones] [Pedimentos] [Reportes]     │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  CONTENIDO SEGÚN TAB SELECCIONADO                       │
│                                                         │
│  ┌─────────────────────────────────────────────────┐   │
│  │ Filtros: [Fecha Inicio] [Fecha Fin] [RFC]       │   │
│  │          [Tipo] [Estado] [Buscar]                │   │
│  └─────────────────────────────────────────────────┘   │
│                                                         │
│  ┌─────────────────────────────────────────────────┐   │
│  │ DataGrid con resultados                          │   │
│  │ □ 001 | 25/01/2026 | COVE | RFC ABC | $10,000   │   │
│  │ □ 002 | 25/01/2026 | COVE | RFC XYZ | $25,000   │   │
│  └─────────────────────────────────────────────────┘   │
│                                                         │
│  [Descargar Seleccionados] [Descargar Todo]            │
│                                                         │
├─────────────────────────────────────────────────────────┤
│  Progreso: ████████░░░░░░░░░ 50% - Descargando...      │
└─────────────────────────────────────────────────────────┘
```

### 5.2 Vista COVEs
- Filtros: Rango de fechas, RFC emisor/receptor, Estado, Tipo operación
- DataGrid: Número, Fecha, RFC Emisor, RFC Receptor, Estado, Valor
- Acciones: Descargar PDF, Descargar XML, Ver detalle
- Selección múltiple para descarga masiva

### 5.3 Vista Manifestaciones
- Filtros: Rango de fechas, RFC solicitante, Estado, Número pedimento
- DataGrid: Número, Fecha, RFC, Estado, Pedimento asociado
- Acciones: Descargar PDF, Descargar XML, Ver detalle

### 5.4 Vista Pedimentos
- Filtros: Rango de fechas, RFC, Aduana, Estado
- DataGrid: Pedimento, Fecha, RFC, Aduana, Valor
- Acciones: Descargar PDF, Ver detalle

### 5.5 Vista Reportes
- Total descargado por tipo
- Descargas por período
- Estados más frecuentes
- Exportar a Excel/PDF

### 5.6 Configuración
- Carpeta de descarga por defecto
- Formato de guardado (XML/PDF/ZIP)
- Nomenclatura de archivos
- Notificaciones
- Proxy settings

---

## FASE 6: Implementar Lógica de Descarga y Guardado
**Duración estimada:** 3-4 días

### 6.1 Servicio de Descarga de Archivos
```csharp
public class FileDownloadService
{
    Task DescargarArchivosAsync(IEnumerable<DownloadTask> tasks, IProgress<int> progress);
    string GenerarNombreArchivo(ArchivoType tipo, string numero);
    Task<bool> GuardarArchivoAsync(byte[] contenido, string ruta, string nombre);
}
```

### 6.2 Opciones de Nomenclatura
```
Por defecto: {TIPO}_{NUMERO}_{FECHA}.{EXT}
Ejemplo: COVE_12345_20250125.pdf
```

### 6.3 Estructura de Carpetas
```
├── Descargas/
│   ├── COVEs/
│   │   ├── 2025/
│   │   │   ├── 01_Enero/
│   │   │   ├── 02_Febrero/
│   │   │   └── ...
│   ├── Manifestaciones/
│   │   └── ...
│   └── Pedimentos/
│       └── ...
```

### 6.4 Manejo de Errores
- Reintentos automáticos (3 intentos)
- Cola de descargas fallidas
- Logging detallado
- Notificaciones de errores

### 6.5 Exportación Masiva
- Comprimir en ZIP por fecha/tipo
- Exportar a carpeta específica
- Incluir índice/resumen en Excel

---

## FASE 7: Sistema de Configuración y Preferencias
**Duración estimada:** 1-2 días

### 7.1 Opciones de Configuración
```csharp
public class AppSettings
{
    // Rutas
    public string CarpetaDescargaDefault { get; set; }
    public string EstructuraCarpetas { get; set; }
    
    // Nomenclatura
    public string FormatoNombreArchivo { get; set; }
    public bool IncluirFechaEnCarpeta { get; set; }
    
    // Descarga
    public int MaxDescargasConcurrentes { get; set; }
    public int TimeoutSegundos { get; set; }
    public int MaxReintentos { get; set; }
    
    // Formatos
    public bool DescargarPDF { get; set; }
    public bool DescargarXML { get; set; }
    public bool ComprimirEnZip { get; set; }
    
    // Proxy
    public bool UsarProxy { get; set; }
    public string ProxyUrl { get; set; }
    public string ProxyUser { get; set; }
    public string ProxyPassword { get; set; }
}
```

### 7.2 Persistencia
- Guardar en `%LOCALAPPDATA%\VucemDownloader\settings.json`
- Encriptar información sensible

---

## FASE 8: Módulo de Reportes y Estadísticas
**Duración estimada:** 2-3 días

### 8.1 Reportes Disponibles
1. **Resumen General**
   - Total de descargas por tipo
   - Período más activo
   - Valor total procesado

2. **Detalle por Fecha**
   - Gráfica de descargas por día/semana/mes
   - Tendencias

3. **Por Estado**
   - Distribución de estados (completados, fallidos, pendientes)

4. **Exportación**
   - Exportar a Excel
   - Exportar a PDF
   - Exportar a CSV

### 8.2 Historial de Descargas
- Registro de todas las descargas realizadas
- Búsqueda y filtros
- Re-descargar documentos

---

## FASE 9: Pruebas y Documentación
**Duración estimada:** 2-3 días

### 9.1 Pruebas
- [ ] Pruebas unitarias de servicios
- [ ] Pruebas de integración con VUCEM (UAT)
- [ ] Pruebas de UI
- [ ] Pruebas de rendimiento

### 9.2 Documentación
- [ ] Manual de usuario
- [ ] Documentación técnica
- [ ] README del proyecto
- [ ] Changelog

### 9.3 Entregables
- Ejecutable (.exe)
- Instalador (opcional)
- Documentación

---

## CRONOGRAMA RESUMIDO

```
Semana 1: FASE 1-2 (Análisis + Cliente COVE)
Semana 2: FASE 3 (Cliente Manifestaciones)
Semana 3: FASE 4-5 (Cliente Pedimentos + UI)
Semana 4: FASE 6-7 (Descarga + Configuración)
Semana 5: FASE 8-9 (Reportes + Pruebas)
```

**Total estimado:** 5 semanas (25 días laborables)

---

## PRIORIDADES DE IMPLEMENTACIÓN

### Alta Prioridad (MVP)
1. Login con FIEL ✓ (ya existe)
2. Sistema de licencias ✓ (ya existe)
3. Cliente COVE con descarga masiva
4. Cliente Manifestaciones con descarga masiva
5. Guardado de archivos

### Media Prioridad
6. Cliente Pedimentos
7. Configuración completa
8. Manejo de errores robusto

### Baja Prioridad (v2.0)
9. Reportes completos
10. Historial de descargas
11. Exportación avanzada

---

## NOTAS IMPORTANTES

1. **Ambiente de Pruebas:** Usar URL UAT de VUCEM primero
2. **Rate Limiting:** Respetar límites de la API
3. **Seguridad:** Nunca guardar passwords en texto plano
4. **Logs:** Registrar todas las operaciones para debugging
5. **Versionado:** Usar semver para el proyecto

---

## PRÓXIMOS PASOS INMEDIATOS

1. ✅ Crear estructura de carpetas del proyecto
2. ✅ Refactorizar VucemClient.cs base
3. ✅ Implementar VucemAuthService para tokens
4. ✅ Implementar consultas básicas de COVE
5. ✅ Probar conexión con ambiente UAT
