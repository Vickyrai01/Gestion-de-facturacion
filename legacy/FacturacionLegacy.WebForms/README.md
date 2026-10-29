# Sistema de Facturación Legacy — Web Forms

Ejemplo de la Clase 2: listar y registrar clientes con Web Forms sobre .NET Framework 4.8. No implementa la migración. Los datos empiezan vacíos y se comparten entre solicitudes y usuarios del mismo AppDomain; se pierden al reciclar IIS Express, reiniciar la aplicación o modificar Web.config. No se comparten entre procesos.

## Ubicación y proyectos anteriores

Se incorpora una solución independiente en `legacy/FacturacionLegacy.WebForms/`, sin referencias a otros proyectos. El proyecto anterior `legacyGestionDeFacturacion.web/` es MVC 5 sobre Framework 4.8: usa packages.config con MVC/Razor/WebPages, Optimization, CodeDom, Newtonsoft.Json y recursos jQuery/Bootstrap. `legacy/` tenía artefactos locales de proyectos anteriores, sin proyectos versionados. `modern/GestionDeFacturacion.Web/` ya contiene MVC sobre net10.0 con Newtonsoft.Json 13.0.3 mediante PackageReference. Se conservan todos esos archivos y soluciones.

## Abrir y ejecutar en Windows

1. En Visual Studio con soporte para .NET Framework, instalar la carga **Desarrollo de ASP.NET y web**, IIS Express y el **Developer/Targeting Pack de .NET Framework 4.8**.
2. Abrir `FacturacionLegacy.WebForms.sln` en esta carpeta; establecer el proyecto como proyecto de inicio.
3. Clic derecho sobre `Clientes.aspx` → **Establecer como página de inicio**. En las propiedades Web usar **IIS Express**, URL `http://localhost:50848/` (puerto independiente del MVC anterior).
4. Compilar en Debug y presionar F5. Abrir `http://localhost:50848/Clientes.aspx`; la raíz también tiene ese documento predeterminado.
5. Registrar un nombre y un email. Verificar mensaje y fila. Abrir otra pestaña y comprobar que aparece el cliente. Probar campos vacíos, email inválido y email repetido cambiando mayúsculas: no deben agregar filas.

No hay paquetes para restaurar. No usar `dotnet run`: este proyecto requiere MSBuild de Visual Studio y System.Web bajo IIS/IIS Express.

## Archivos y responsabilidades

```text
FacturacionLegacy.WebForms/
├── FacturacionLegacy.WebForms.sln       # solución independiente
├── FacturacionLegacy.WebForms.csproj    # Web Application tradicional, v4.8
├── Clientes.aspx                       # formulario, validadores y GridView
├── Clientes.aspx.cs                    # ciclo de vida, validación, alta y mensajes
├── Clientes.aspx.designer.cs           # campos tipados de los controles
├── Site.Master                         # título, navegación y formulario servidor
├── Site.Master.cs                      # clase parcial de la Master Page
├── Site.Master.designer.cs             # campos de los controles de la Master
├── Models/
│   └── Cliente.cs                      # Id, Nombre, Email
├── Datos/
│   └── ClientesEnMemoria.cs             # lista estática, copias y sincronización
├── Web.config                         # runtime, appSettings y documento inicial
└── README.md                          # ejecución y guía de las demostraciones
```

## Recorrido de las seis demos y breakpoints

1. **Aplicación:** F5, registrar un cliente y observar el GridView. Breakpoint en `CargarClientes()`, línea `grdClientes.DataSource = ...`.
2. **PostBack:** breakpoint en la primera instrucción de `btnGuardar_Click` (`Page.Validate`). Completar ambos campos para que la validación cliente permita enviar el formulario. Guardar envía un POST a la misma página, ejecuta su ciclo de vida y el evento del botón, y devuelve HTML completo.
3. **ViewState:** inspeccionar el HTML generado y buscar `__VIEWSTATE`, `__VIEWSTATEGENERATOR` y `__EVENTVALIDATION`. El GridView y Label conservan estado entre postbacks; TextBox también recupera valores del formulario enviado. El ViewState viaja en campos ocultos, no es secreto ni almacena la lista compartida. En un alta inválida no se vuelve a enlazar el GridView: conserva la representación anterior. Una nueva petición GET lee la lista estática actual. Otro usuario puede haber agregado filas desde el último render.
4. **Code-behind:** breakpoint en `Page_Load`, línea `if (!IsPostBack)`. Comparar GET inicial (false) y POST (true). Seguir `OnClick="btnGuardar_Click"`, `txtNombre.Text`, el registro y `DataBind`. Poner otro breakpoint en el `lock` de `Registrar` para explicar por qué buscar duplicados y agregar deben ser atómicos.
5. **Incompatibilidades:** mostrar `Clientes : Page`, los campos WebControls de designer y `SiteMaster : MasterPage`. System.Web y su ciclo de vida no existen en ASP.NET Core: cambiar el framework del proyecto no convierte páginas en controladores/vistas.
6. **Configuración:** breakpoint en `ConfigurationManager.AppSettings["Clientes.MensajeAlta"]`. Cambiar el mensaje en Web.config y observar el nuevo texto (el cambio reinicia la aplicación y borra la lista). Mostrar las cuatro referencias del csproj; no hay NuGet ni packages.config artificial.

## Problemas arquitectónicos para discutir

El evento de la página mezcla lectura de controles, reglas de entrada, coordinación del alta, duplicados, mensajes y actualización de UI. Se accede directamente a una clase estática, sin inyección de dependencias. El almacenamiento y las operaciones de página son sincrónicos (no hay E/S que justifique async en este ejemplo). La interfaz depende de eventos de servidor, PostBack y ViewState, de System.Web y de la configuración XML. La memoria global es temporal; no equivale a persistencia durable ni a ViewState. Son limitaciones didácticas, sin agregar capas o módulos.

Se mantienen la validación de servidor, la codificación HTML de filas/mensajes, request validation, MAC de ViewState y event validation con sus valores seguros predeterminados. El catch de FormatException convierte un error esperado de formato en un mensaje; otros errores no se silencian. El almacenamiento devuelve copias y protege lectura, duplicados e IDs con el mismo lock.

## Dependencias y próxima migración (solo planificación)

| Dependencia actual | Origen | Equivalente o tratamiento en ASP.NET Core |
| --- | --- | --- |
| System, System.Core | .NET Framework 4.8 | APIs del runtime moderno; revisar usos al portar |
| System.Web, UI.Page, UI.WebControls y MasterPage | .NET Framework 4.8 | Reescribir con MVC/Razor y peticiones HTTP; no hay reemplazo binario de Web Forms |
| System.Configuration / ConfigurationManager | .NET Framework 4.8 | IConfiguration y appsettings.json para este ejemplo |
| Web.config | Configuración ASP.NET/IIS | appsettings.json y Program.cs; IIS puede seguir usando un Web.config de hosting |
| NuGet | Ninguno en este proyecto | PackageReference solo si se necesitan paquetes; no trasladar los del MVC anterior |

| Categoría | Código actual | Archivos MVC que construiremos en clase |
| --- | --- | --- |
| Código reutilizable | POCO Cliente, comparación de email y lógica C# independiente de System.Web | `Models/Cliente.cs` |
| Código que requiere adaptación | Lista estática/lock, reglas de validación, mensajes y lectura de configuración | `Services/ClientesEnMemoria.cs` (servicio registrado en DI), `Models/AltaClienteViewModel.cs`, `appsettings.json` |
| Código que debe reescribirse | aspx, code-behind, designer, Master Page, ViewState/PostBack y proyecto Web Application clásico | `Controllers/ClientesController.cs` (GET/POST), `Views/Clientes/Index.cshtml`, `Views/Shared/_Layout.cshtml`, `Views/_ViewImports.cshtml`, `Views/_ViewStart.cshtml`, `Program.cs`, proyecto SDK Web net10.0 |

En la clase el POST tendrá validación del modelo y antiforgery, y se evaluará redirigir al GET tras un alta correcta. No se crean ahora esos archivos ni se modifica modern/.

## Verificación realizada

Compilado en Windows con MSBuild de Visual Studio en Debug y Release sin advertencias ni errores. Ejecutado con IIS Express en el puerto 50848: se probaron GET inicial, documento predeterminado, POST de alta, email duplicado con distintas mayúsculas, campos vacíos y con espacios, email mal formado, límite de nombre, codificación HTML de `Ana & Hijos`, campos ViewState/event validation y listado desde otra sesión HTTP. Solo las altas válidas agregaron filas. Se verificaron también las directivas Inherits, clases parciales, controles designer y referencias mediante compilación y ejecución de las páginas.

Una dirección con formato `Nombre <email@example.com>` fue rechazada por request validation antes del evento: se conserva esa protección de ASP.NET. Las pruebas fueron HTTP automatizadas; no se verificaron visualmente el navegador ni la interacción con breakpoints de Visual Studio. El servidor de prueba se detuvo al finalizar.
