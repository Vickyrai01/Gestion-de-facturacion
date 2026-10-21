param([switch]$OmitirCompilacion)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
Set-Location -LiteralPath $projectRoot
$localDir = Join-Path $PSScriptRoot '.local'
New-Item -ItemType Directory -Path $localDir -Force | Out-Null
$vswhere = 'C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe'
$msbuild = & $vswhere -latest -products '*' -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
if (-not $OmitirCompilacion) {
    & $msbuild SistemaFacturacion.Legacy.slnx /t:Build /p:Configuration=Debug /p:MvcBuildViews=true /nologo /verbosity:minimal
    if ($LASTEXITCODE -ne 0) { throw 'Falló la compilación.' }
}
$databaseName = 'FacturacionLegacyModulo2Pruebas_' + [Guid]::NewGuid().ToString('N')
$connection = "Data Source=(LocalDB)\MSSQLLocalDB;Initial Catalog=$databaseName;Integrated Security=True;MultipleActiveResultSets=True"
$oldVariables = @{}
foreach ($name in @('FACTURACION_DB_CONNECTION', 'FACTURACION_ADMIN_PASSWORD', 'FACTURACION_OPERADOR_PASSWORD')) { $oldVariables[$name] = [Environment]::GetEnvironmentVariable($name) }
$env:FACTURACION_DB_CONNECTION = $connection
$rng = [Security.Cryptography.RandomNumberGenerator]::Create()
$bytes = New-Object byte[] 24
$rng.GetBytes($bytes); $env:FACTURACION_ADMIN_PASSWORD = [Convert]::ToBase64String($bytes)
$rng.GetBytes($bytes); $env:FACTURACION_OPERADOR_PASSWORD = [Convert]::ToBase64String($bytes)
$rng.Dispose()
$adminCookies = New-Object Net.CookieContainer
$operatorCookies = New-Object Net.CookieContainer
$anonymousCookies = New-Object Net.CookieContainer
$script:checks = 0
$mvcProcess = $null; $wcfProcess = $null
$certificate = Get-ChildItem Cert:\LocalMachine\My | Where-Object { $_.FriendlyName -eq 'IIS Express Development Certificate' -and $_.NotAfter -gt (Get-Date) } | Select-Object -First 1
if ($null -eq $certificate) { throw 'Falta el certificado de desarrollo de IIS Express.' }
Add-Type -TypeDefinition @'
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using System.Text;
using System.Threading.Tasks;
public static class CertificadoLocalModulo2 {
    public static string Thumbprint;
    public static RemoteCertificateValidationCallback Callback = Validar;
    public static bool Validar(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors) {
        var request = sender as HttpWebRequest;
        return request != null && request.RequestUri.Host == "localhost" && certificate != null && certificate.GetCertHashString() == Thumbprint;
    }
    public static int[] EnviarParalelo(string url, CookieContainer cookies, string body, int cantidad) {
        var estados = new int[cantidad];
        Parallel.For(0, cantidad, i => {
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST"; request.AllowAutoRedirect = false; request.CookieContainer = cookies;
            request.ServerCertificateValidationCallback = Callback; request.ContentType = "application/x-www-form-urlencoded";
            var bytes = Encoding.UTF8.GetBytes(body); request.ContentLength = bytes.Length;
            using (var stream = request.GetRequestStream()) stream.Write(bytes, 0, bytes.Length);
            try { using (var response = (HttpWebResponse)request.GetResponse()) estados[i] = (int)response.StatusCode; }
            catch (WebException error) { using (var response = (HttpWebResponse)error.Response) { if (response == null) throw; estados[i] = (int)response.StatusCode; } }
        });
        return estados;
    }
}
'@
[CertificadoLocalModulo2]::Thumbprint = $certificate.Thumbprint
function Check($condition, $description) {
    if (-not $condition) { throw "FALLÓ: $description" }
    $script:checks++
    Write-Output "OK: $description"
}
function SqlScalar([string]$query) {
    $sql = New-Object Data.SqlClient.SqlConnection $connection
    try { $sql.Open(); $cmd = $sql.CreateCommand(); $cmd.CommandText = $query; return $cmd.ExecuteScalar() }
    finally { $sql.Dispose() }
}
function Request([string]$url, [string]$method = 'GET', $data = @{}, $jar = $adminCookies, [string]$soapAction = '') {
    if ($url.StartsWith('/')) { $url = 'https://localhost:44374' + $url }
    $req = [Net.HttpWebRequest]::Create($url)
    $req.Method = $method; $req.AllowAutoRedirect = $false; $req.CookieContainer = $jar; $req.Timeout = 30000
    if ($url.StartsWith('https://localhost:44374/')) { $req.ServerCertificateValidationCallback = [CertificadoLocalModulo2]::Callback }
    if ($method -ne 'GET') {
        if ($soapAction) {
            $req.ContentType = 'text/xml; charset=utf-8'; $req.Headers['SOAPAction'] = '"' + $soapAction + '"'; $body = [string]$data
        } else {
            $req.ContentType = 'application/x-www-form-urlencoded'
            $pairs = foreach ($key in $data.Keys) { [Uri]::EscapeDataString($key) + '=' + [Uri]::EscapeDataString([string]$data[$key]) }
            $body = $pairs -join '&'
        }
        $payload = [Text.Encoding]::UTF8.GetBytes($body); $req.ContentLength = $payload.Length
        $stream = $req.GetRequestStream(); $stream.Write($payload, 0, $payload.Length); $stream.Close()
    }
    try { $res = $req.GetResponse() } catch [Net.WebException] { if ($null -eq $_.Exception.Response) { throw }; $res = $_.Exception.Response }
    $reader = New-Object IO.StreamReader($res.GetResponseStream())
    $raw = $reader.ReadToEnd()
    $result = [pscustomobject]@{ Status = [int]$res.StatusCode; Raw = $raw; Body = [Net.WebUtility]::HtmlDecode($raw); Location = $res.Headers['Location']; Cookies = $res.Headers['Set-Cookie'] }
    $reader.Dispose(); $res.Dispose()
    return $result
}
function Token($page) {
    $match = [regex]::Match($page.Raw, 'name="__RequestVerificationToken"[^>]*value="([^"]+)"')
    if (-not $match.Success) { throw 'No se encontró el token antiforgery.' }
    return [Net.WebUtility]::HtmlDecode($match.Groups[1].Value)
}
function Login($user, $password, $jar) {
    $page = Request '/Account/Login' 'GET' @{} $jar
    return Request '/Account/Login' 'POST' @{ NombreUsuario = $user; Password = $password; __RequestVerificationToken = (Token $page) } $jar
}
function WaitSite($url) {
    for ($i = 0; $i -lt 20; $i++) {
        try { $page = Request $url; if ($page.Status -lt 500) { return } } catch [Net.WebException] { }
        Start-Sleep -Milliseconds 500
    }
    throw "El sitio no inició: $url"
}
function StartMvc {
    $config = Join-Path $localDir 'applicationhost.config'
    return Start-Process -FilePath "$env:ProgramFiles\IIS Express\iisexpress.exe" -ArgumentList @("/config:`"$config`"", '/site:Modulo2Mvc', '/systray:false') -WorkingDirectory $projectRoot -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $localDir 'mvc-out.log') -RedirectStandardError (Join-Path $localDir 'mvc-error.log')
}
try {
    foreach ($port in @(44374, 60122)) {
        $probe = New-Object Net.Sockets.TcpClient
        try { $probe.Connect('localhost', $port); throw "El puerto $port ya está en uso. Detené la instancia local antes de validar." }
        catch [Net.Sockets.SocketException] { } finally { $probe.Dispose() }
    }
    $toolsExe = Join-Path $projectRoot 'FacturacionLegacy.Tools/bin/Debug/FacturacionLegacy.Tools.exe'
    & $toolsExe migrar; if ($LASTEXITCODE -ne 0) { throw 'No se pudo preparar la base de pruebas.' }
    Check ((SqlScalar 'SELECT COUNT(*) FROM dbo.Clientes') -eq 5 -and (SqlScalar 'SELECT COUNT(*) FROM dbo.Productos') -eq 8 -and (SqlScalar 'SELECT COUNT(*) FROM dbo.Facturas') -eq 3) 'Migraciones y datos iniciales 5/8/3'
    & $toolsExe migrar; if ($LASTEXITCODE -ne 0) { throw 'Falló la segunda inicialización.' }
    Check ((SqlScalar 'SELECT COUNT(*) FROM dbo.Clientes') -eq 5 -and (SqlScalar 'SELECT COUNT(*) FROM dbo.Productos') -eq 8 -and (SqlScalar 'SELECT COUNT(*) FROM dbo.Facturas') -eq 3) 'Seed idempotente'
    & $toolsExe usuarios-demo; if ($LASTEXITCODE -ne 0) { throw 'No se pudieron preparar las cuentas de pruebas.' }
    Check ((SqlScalar 'SELECT COUNT(*) FROM dbo.Usuarios WHERE PasswordHash LIKE ''$2%$12$%'' AND Activo=1') -eq 2) 'Contraseñas con bcrypt y factor 12'
    # Configuración temporal de IIS: reutiliza sus módulos y application pool CLR v4.
    $template = Join-Path ${env:ProgramFiles(x86)} 'IIS Express/AppServer/applicationhost.config'
    if (-not (Test-Path $template)) { $template = Join-Path $env:USERPROFILE 'Documents/IISExpress/config/applicationhost.config' }
    [xml]$hostConfig = Get-Content $template -Raw
    $sites = $hostConfig.configuration.'system.applicationHost'.sites
    foreach ($site in @($sites.site)) { [void]$sites.RemoveChild($site) }
    $site = $hostConfig.CreateElement('site'); $site.SetAttribute('name', 'Modulo2Mvc'); $site.SetAttribute('id', '10')
    $application = $hostConfig.CreateElement('application'); $application.SetAttribute('path', '/'); $application.SetAttribute('applicationPool', 'Clr4IntegratedAppPool')
    $virtual = $hostConfig.CreateElement('virtualDirectory'); $virtual.SetAttribute('path', '/'); $virtual.SetAttribute('physicalPath', (Join-Path $projectRoot 'legacyGestionDeFacturacion.web')); [void]$application.AppendChild($virtual); [void]$site.AppendChild($application)
    $bindings = $hostConfig.CreateElement('bindings'); $binding = $hostConfig.CreateElement('binding'); $binding.SetAttribute('protocol', 'https'); $binding.SetAttribute('bindingInformation', '*:44374:localhost'); [void]$bindings.AppendChild($binding); [void]$site.AppendChild($bindings); [void]$sites.AppendChild($site)
    $hostConfig.Save((Join-Path $localDir 'applicationhost.config'))
    $mvcProcess = StartMvc
    $wcfProcess = Start-Process -FilePath "$env:ProgramFiles\IIS Express\iisexpress.exe" -ArgumentList @("/path:`"$(Join-Path $projectRoot 'FacturacionLegacy.Wcf')`"", '/port:60122', '/systray:false') -WorkingDirectory $projectRoot -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $localDir 'wcf-out.log') -RedirectStandardError (Join-Path $localDir 'wcf-error.log')
    WaitSite 'https://localhost:44374/Account/Login'
    $anonymous = Request '/Clientes' 'GET' @{} $anonymousCookies
    Check ($anonymous.Status -eq 302 -and $anonymous.Location.Contains('/Account/Login')) 'Endpoints privados requieren login'
    $invalidLogin = Login 'admin' 'incorrecta' $anonymousCookies
    Check ($invalidLogin.Status -eq 200 -and $invalidLogin.Body.Contains('Usuario o contraseña inválidos.')) 'Login inválido rechazado'
    $logged = Login 'admin' $env:FACTURACION_ADMIN_PASSWORD $adminCookies
    Check ($logged.Status -eq 302) 'Login ADMIN'
    Check ($logged.Cookies -match 'secure' -and $logged.Cookies -match 'HttpOnly' -and $logged.Cookies -match 'SameSite=Lax') 'Cookie de autenticación segura'
    Check ((Request '/').Body.Contains('Sistema de Gestión de Facturación')) 'Inicio autenticado'
    Check ((Request '/Clientes').Status -eq 200 -and (Request '/Productos').Status -eq 200 -and (Request '/Facturas').Status -eq 200) 'Listados de clientes, productos y facturas'
    $token = Token (Request '/Clientes/Create')
    $invalidClient = Request '/Clientes/Create' 'POST' @{ __RequestVerificationToken = $token; Nombre = ''; Apellido = ''; Documento = ''; Email = 'mal' }
    Check ($invalidClient.Status -eq 200 -and $invalidClient.Body.Contains('El nombre es obligatorio.') -and $invalidClient.Body.Contains('Ingresá un email válido.')) 'Validación de clientes'
    $client = Request '/Clientes/Create' 'POST' @{ __RequestVerificationToken = $token; Nombre = 'Prueba'; Apellido = 'Persistencia'; Documento = '99900001'; Email = 'prueba@example.com'; Activo = 'false' }
    Check ($client.Status -eq 302) 'Crear cliente EF6'
    $clientId = [int]([regex]::Match($client.Location, '/(\d+)$').Groups[1].Value)
    Check ((SqlScalar "SELECT Activo FROM dbo.Clientes WHERE Id=$clientId") -eq $true) 'Alta ignora estado enviado'
    $editClient = Request "/Clientes/Edit/$clientId" 'POST' @{ __RequestVerificationToken = $token; Nombre = 'Editado'; Apellido = 'Persistencia'; Documento = '99900001'; Email = 'editado@example.com'; Activo = 'false' }
    Check ($editClient.Status -eq 302 -and (Request "/Clientes/Details/$clientId").Body.Contains('editado@example.com')) 'Editar y consultar cliente'
    $prodToken = Token (Request '/Productos/Create')
    $productData = @{ __RequestVerificationToken = $prodToken; Codigo = 'prueba-001'; Nombre = 'Producto prueba'; Descripcion = 'Prueba'; PrecioUnitario = '125.50'; Stock = '7' }
    $product = Request '/Productos/Create' 'POST' $productData
    Check ($product.Status -eq 302) 'Crear producto'
    $productId = [int]([regex]::Match($product.Location, '/(\d+)$').Groups[1].Value)
    Check ((SqlScalar "SELECT Codigo FROM dbo.Productos WHERE Id=$productId") -eq 'PRUEBA-001') 'Normalización de código'
    $duplicate = Request '/Productos/Create' 'POST' $productData
    Check ($duplicate.Status -eq 200 -and $duplicate.Body.Contains('Ya existe un producto con ese código.')) 'Código duplicado rechazado'
    $productData.Codigo = 'PRUEBA-NEG'; $productData.PrecioUnitario = '-1'; $productData.Stock = '-2'
    $negative = Request '/Productos/Create' 'POST' $productData
    Check ($negative.Status -eq 200 -and (SqlScalar "SELECT COUNT(*) FROM dbo.Productos WHERE Codigo='PRUEBA-NEG'") -eq 0) 'Precio y stock negativos rechazados'
    $invoiceToken = Token (Request '/Facturas/Create')
    $seedProductId = SqlScalar "SELECT Id FROM dbo.Productos WHERE Codigo='PROD-001'"
    $inactiveClientId = SqlScalar "SELECT Id FROM dbo.Clientes WHERE Documento='32555666'"
    $invoiceData = @{ __RequestVerificationToken = $invoiceToken; ClienteId = "$clientId"; 'Detalles[0].ProductoId' = "$productId"; 'Detalles[0].Cantidad' = '2'; 'Detalles[1].ProductoId' = "$seedProductId"; 'Detalles[1].Cantidad' = '3'; Subtotal = '1'; Impuestos = '1'; Total = '1'; 'Detalles[0].PrecioUnitario' = '1' }
    $invoice = Request '/Facturas/Create' 'POST' $invoiceData
    Check ($invoice.Status -eq 302) 'Crear factura con múltiples detalles'
    $invoiceId = [int]([regex]::Match($invoice.Location, '/(\d+)$').Groups[1].Value)
    Check ((SqlScalar "SELECT Subtotal FROM dbo.Facturas WHERE Id=$invoiceId") -eq 551 -and (SqlScalar "SELECT Impuestos FROM dbo.Facturas WHERE Id=$invoiceId") -eq 115.71 -and (SqlScalar "SELECT Total FROM dbo.Facturas WHERE Id=$invoiceId") -eq 666.71) 'Importes exactos calculados en servidor, sin confiar en totales enviados'
    Check ((SqlScalar "SELECT COUNT(*) FROM dbo.DetallesFactura WHERE FacturaId=$invoiceId") -eq 2 -and (SqlScalar "SELECT Stock FROM dbo.Productos WHERE Id=$productId") -eq 7) 'Detalles persistidos y stock informativo'
    $invalidInvoice = $invoiceData.Clone(); $invalidInvoice.'Detalles[1].ProductoId' = '999999'
    $before = SqlScalar 'SELECT COUNT(*) FROM dbo.Facturas'
    Check ((Request '/Facturas/Create' 'POST' $invalidInvoice).Status -eq 200 -and (SqlScalar 'SELECT COUNT(*) FROM dbo.Facturas') -eq $before) 'Producto inexistente no guarda factura parcial'
    $invalidInvoice = $invoiceData.Clone(); $invalidInvoice.'Detalles[0].Cantidad' = '0'
    Check ((Request '/Facturas/Create' 'POST' $invalidInvoice).Status -eq 200 -and (SqlScalar 'SELECT COUNT(*) FROM dbo.Facturas') -eq $before) 'Cantidad no positiva rechazada'
    $invalidInvoice = $invoiceData.Clone(); $invalidInvoice.ClienteId = "$inactiveClientId"
    Check ((Request '/Facturas/Create' 'POST' $invalidInvoice).Status -eq 200 -and (SqlScalar 'SELECT COUNT(*) FROM dbo.Facturas') -eq $before) 'Cliente inactivo rechazado'
    $invalidInvoice = @{ __RequestVerificationToken = $invoiceToken; ClienteId = "$clientId" }
    Check ((Request '/Facturas/Create' 'POST' $invalidInvoice).Status -eq 200 -and (SqlScalar 'SELECT COUNT(*) FROM dbo.Facturas') -eq $before) 'Factura sin detalles rechazada'
    $editedProduct = Request "/Productos/Edit/$productId" 'POST' @{ __RequestVerificationToken = $prodToken; Codigo = 'PRUEBA-001'; Nombre = 'Nuevo nombre'; Descripcion = ''; PrecioUnitario = '200.00'; Stock = '7' }
    Check ($editedProduct.Status -eq 302 -and (SqlScalar "SELECT PrecioUnitario FROM dbo.DetallesFactura WHERE FacturaId=$invoiceId AND ProductoId=$productId") -eq 125.50) 'Precio histórico no cambia al editar producto'
    Check ((Request "/Facturas?clienteId=$clientId&estado=PENDIENTE").Body.Contains((SqlScalar "SELECT Numero FROM dbo.Facturas WHERE Id=$invoiceId"))) 'Filtros por cliente y estado'
    Check ((Request '/Facturas?estado=INVALIDO').Status -eq 400) 'Estado de filtro inválido rechazado'
    Check ((Request "/Facturas/Emitir/$invoiceId").Status -eq 404 -and (Request "/Facturas/Emitir/$invoiceId" 'POST').Status -ge 400) 'Emisión exige POST y antiforgery'
    Check ((Login 'operador' $env:FACTURACION_OPERADOR_PASSWORD $operatorCookies).Status -eq 302) 'Login OPERADOR'
    Check ((Request '/Clientes' 'GET' @{} $operatorCookies).Status -eq 200 -and (Request '/Productos' 'GET' @{} $operatorCookies).Status -eq 200) 'OPERADOR consulta clientes y productos'
    foreach ($url in @('/Clientes/Create', '/Clientes/Edit/1', '/Productos/Create', '/Productos/Edit/1', '/Facturas/Reportes')) { Check ((Request $url 'GET' @{} $operatorCookies).Status -eq 403) "OPERADOR sin permiso: $url" }
    $operatorToken = Token (Request '/Facturas/Create' 'GET' @{} $operatorCookies)
    foreach ($url in @('/Clientes/Create', "/Clientes/Desactivar/$clientId", '/Productos/Create', "/Productos/Desactivar/$productId", "/Facturas/Emitir/$invoiceId", "/Facturas/Anular/$invoiceId")) { Check ((Request $url 'POST' @{ __RequestVerificationToken = $operatorToken } $operatorCookies).Status -eq 403) "Permiso de escritura en servidor: $url" }
    $operatorInvoice = $invoiceData.Clone(); $operatorInvoice.__RequestVerificationToken = $operatorToken
    Check ((Request '/Facturas/Create' 'POST' $operatorInvoice $operatorCookies).Status -eq 302) 'OPERADOR crea facturas'
    Check ((Request "/Facturas/Emitir/$invoiceId" 'POST' @{ __RequestVerificationToken = $invoiceToken }).Status -eq 302) 'ADMIN emite factura'
    Check ((Request "/Facturas/Emitir/$invoiceId" 'POST' @{ __RequestVerificationToken = $invoiceToken }).Status -eq 409 -and (Request "/Facturas/Edit/$invoiceId").Status -eq 404) 'Factura emitida no puede editarse ni emitirse de nuevo'
    Check ((Request "/Facturas/Anular/$invoiceId" 'POST' @{ __RequestVerificationToken = $invoiceToken }).Status -eq 302 -and (SqlScalar "SELECT Estado FROM dbo.Facturas WHERE Id=$invoiceId") -eq 'ANULADA' -and (SqlScalar "SELECT COUNT(*) FROM dbo.DetallesFactura WHERE FacturaId=$invoiceId") -eq 2) 'Anulación conserva historial'
    Check ((Request '/Facturas/Reportes').Status -eq 200) 'ADMIN consulta reportes'
    Check ((Request "/Productos/Desactivar/$productId" 'POST' @{ __RequestVerificationToken = $prodToken }).Status -eq 302 -and (SqlScalar "SELECT Activo FROM dbo.Productos WHERE Id=$productId") -eq $false) 'Desactivar producto'
    Check ((Request '/Facturas/Create' 'POST' $invoiceData).Status -eq 200 -and (SqlScalar 'SELECT COUNT(*) FROM dbo.Facturas') -eq ($before + 1)) 'Producto inactivo rechazado'
    Check ((Request "/Clientes/Desactivar/$clientId" 'PATCH' @{ __RequestVerificationToken = $token }).Status -eq 302 -and (SqlScalar "SELECT Activo FROM dbo.Clientes WHERE Id=$clientId") -eq $false) 'Clientes conservan desactivación PATCH del Módulo 1'
    $parallelInvoice = $invoiceData.Clone()
    $parallelInvoice.ClienteId = [string](SqlScalar "SELECT Id FROM dbo.Clientes WHERE Documento='30123456'")
    $parallelInvoice.'Detalles[0].ProductoId' = "$seedProductId"
    $parallelBody = ($parallelInvoice.Keys | ForEach-Object { [Uri]::EscapeDataString($_) + '=' + [Uri]::EscapeDataString([string]$parallelInvoice[$_]) }) -join '&'
    $parallelBefore = SqlScalar 'SELECT COUNT(*) FROM dbo.Facturas'
    $statuses = [CertificadoLocalModulo2]::EnviarParalelo('https://localhost:44374/Facturas/Create', $adminCookies, $parallelBody, 4)
    Check ((@($statuses | Where-Object { $_ -eq 302 }).Count -eq 4) -and (SqlScalar 'SELECT COUNT(*) FROM dbo.Facturas') -eq ($parallelBefore + 4)) 'Cuatro altas concurrentes de facturas'
    Check ((SqlScalar 'SELECT COUNT(*) - COUNT(DISTINCT Numero) FROM dbo.Facturas') -eq 0) 'Numeración única con altas concurrentes'
    $raceData = @{ __RequestVerificationToken = $prodToken; Codigo = 'CARRERA-001'; Nombre = 'Concurrente'; PrecioUnitario = '1.00'; Stock = '0' }
    $raceBody = ($raceData.Keys | ForEach-Object { [Uri]::EscapeDataString($_) + '=' + [Uri]::EscapeDataString([string]$raceData[$_]) }) -join '&'
    $raceStatuses = [CertificadoLocalModulo2]::EnviarParalelo('https://localhost:44374/Productos/Create', $adminCookies, $raceBody, 2)
    Check ((@($raceStatuses | Where-Object { $_ -eq 302 }).Count -eq 1) -and (SqlScalar "SELECT COUNT(*) FROM dbo.Productos WHERE Codigo='CARRERA-001'") -eq 1) 'Código único con dos altas concurrentes'
    WaitSite 'http://localhost:60122/FacturaConsultaService.svc?wsdl'
    $wsdl = Request 'http://localhost:60122/FacturaConsultaService.svc?wsdl'
    Check ($wsdl.Status -eq 200 -and $wsdl.Raw.Contains('ObtenerFacturaPorId') -and $wsdl.Raw.Contains('ObtenerFacturasPorCliente') -and $wsdl.Raw.Contains('ObtenerFacturasPorEstado')) 'WSDL y operaciones SOAP'
    foreach ($operation in @('ObtenerFacturaPorId', 'ObtenerFacturasPorCliente', 'ObtenerFacturasPorEstado')) {
        $parameter = switch ($operation) { 'ObtenerFacturaPorId' { "<id>$invoiceId</id>" }; 'ObtenerFacturasPorCliente' { "<clienteId>$clientId</clienteId>" }; 'ObtenerFacturasPorEstado' { '<estado>PENDIENTE</estado>' } }
        $soap = '<s:Envelope xmlns:s="http://schemas.xmlsoap.org/soap/envelope/"><s:Body><' + $operation + ' xmlns="urn:facturacion-legacy">' + $parameter + '</' + $operation + '></s:Body></s:Envelope>'
        $response = Request 'http://localhost:60122/FacturaConsultaService.svc' 'POST' $soap $anonymousCookies ('urn:facturacion-legacy/IFacturaConsultaService/' + $operation)
        Check ($response.Status -eq 200 -and $response.Raw.Contains($operation + 'Response')) "SOAP: $operation"
    }
    $badSoap = '<s:Envelope xmlns:s="http://schemas.xmlsoap.org/soap/envelope/"><s:Body><ObtenerFacturasPorEstado xmlns="urn:facturacion-legacy"><estado>INVALIDO</estado></ObtenerFacturasPorEstado></s:Body></s:Envelope>'
    $fault = Request 'http://localhost:60122/FacturaConsultaService.svc' 'POST' $badSoap $anonymousCookies 'urn:facturacion-legacy/IFacturaConsultaService/ObtenerFacturasPorEstado'
    Check ($fault.Status -eq 500 -and $fault.Body.Contains('Estado inválido.') -and -not $fault.Body.Contains('SqlException')) 'Fault WCF controlado'
    $beforeStates = SqlScalar "SELECT COUNT(*) FROM dbo.Facturas WHERE Estado='PENDIENTE'"
    & ./FacturacionLegacy.WindowsService/bin/Debug/FacturacionLegacy.WindowsService.exe --una-vez
    Check ($LASTEXITCODE -eq 0 -and (SqlScalar "SELECT COUNT(*) FROM dbo.Facturas WHERE Estado='PENDIENTE'") -eq $beforeStates) 'Windows Service consulta y registra sin cambiar estados'
    $logDir = 'FacturacionLegacy.WindowsService/bin/Debug/logs'
    Check ((Get-ChildItem $logDir -Filter '*.log' | Get-Content | Select-Object -Last 1) -match 'Facturas pendientes:') 'Registro de fecha y cantidad'
    $serviceConfig = (Resolve-Path 'FacturacionLegacy.WindowsService/bin/Debug/FacturacionLegacy.WindowsService.exe.config').Path
    $originalServiceConfig = [IO.File]::ReadAllText($serviceConfig)
    $logCount = @(Get-ChildItem $logDir -Filter '*.log' | Get-Content).Count
    try {
        [xml]$configXml = $originalServiceConfig
        ($configXml.configuration.appSettings.add | Where-Object { $_.key -eq 'IntervaloSegundos' }).value = '5'
        $configXml.Save($serviceConfig)
        & ./FacturacionLegacy.WindowsService/bin/Debug/FacturacionLegacy.WindowsService.exe --consola --segundos 6
        Check ($LASTEXITCODE -eq 0 -and @(Get-ChildItem $logDir -Filter '*.log' | Get-Content).Count -ge ($logCount + 2)) 'Timer periódico ejecuta dos ciclos y se detiene normalmente'
    } finally { [IO.File]::WriteAllText($serviceConfig, $originalServiceConfig) }
    # Reiniciar únicamente la instancia MVC de prueba.
    Stop-Process -Id $mvcProcess.Id; $mvcProcess.WaitForExit(); $mvcProcess = StartMvc
    WaitSite 'https://localhost:44374/Account/Login'
    $adminCookies = New-Object Net.CookieContainer
    Check ((Login 'admin' $env:FACTURACION_ADMIN_PASSWORD $adminCookies).Status -eq 302 -and (Request "/Clientes/Details/$clientId").Body.Contains('editado@example.com') -and (Request "/Facturas/Details/$invoiceId").Body.Contains('ANULADA')) 'Persistencia después de reiniciar IIS Express'
    $logoutToken = Token (Request '/')
    Check ((Request '/Account/Logout' 'GET').Status -eq 404) 'Logout no admite GET'
    Check ((Request '/Account/Logout' 'POST' @{ __RequestVerificationToken = $logoutToken }).Status -eq 302 -and (Request '/Clientes').Status -eq 302) 'Logout y protección posterior'
    Write-Output "TOTAL: $script:checks verificaciones correctas."
}
finally {
    foreach ($process in @($mvcProcess, $wcfProcess)) { if ($null -ne $process -and -not $process.HasExited) { Stop-Process -Id $process.Id; $process.WaitForExit() } }
    # Eliminar solo la base temporal cuyo nombre aleatorio fue creado por esta ejecución.
    if ($databaseName -match '^FacturacionLegacyModulo2Pruebas_[a-f0-9]{32}$') {
        [Data.SqlClient.SqlConnection]::ClearAllPools()
        $cleanup = New-Object Data.SqlClient.SqlConnection 'Data Source=(LocalDB)\MSSQLLocalDB;Initial Catalog=master;Integrated Security=True'
        try { $cleanup.Open(); $cmd = $cleanup.CreateCommand(); $cmd.CommandText = "IF DB_ID('$databaseName') IS NOT NULL BEGIN ALTER DATABASE [$databaseName] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$databaseName]; END"; [void]$cmd.ExecuteNonQuery() }
        finally { $cleanup.Dispose() }
    }
    foreach ($name in $oldVariables.Keys) { [Environment]::SetEnvironmentVariable($name, $oldVariables[$name]) }
}
