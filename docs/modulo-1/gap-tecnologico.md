# Gap tecnológico: documentación conceptual

Este documento prepara el análisis de las clases 1 y 2. No se realizó una migración: el código de esta rama sigue siendo .NET Framework 4.8 con MVC 5. El sistema .NET 10 se construirá en un proyecto separado.

## Runtime y plataformas

Un runtime es el entorno que ejecuta el código compilado: carga ensamblados, administra memoria, recolecta objetos y gestiona excepciones, entre otras tareas. El compilador transforma C# en código intermedio; el runtime puede convertirlo en instrucciones de máquina mediante JIT. Instalar un runtime permite ejecutar; disponer del SDK, compiladores y referencias permite desarrollar. Son responsabilidades diferentes. Ver [Common Language Runtime](https://learn.microsoft.com/en-us/dotnet/standard/clr).

.NET Framework 4.8 pertenece a la implementación histórica de .NET integrada con Windows. .NET 10 pertenece a la línea moderna que sucedió a .NET Core y dispone de runtime, bibliotecas y herramientas propios. Los números no describen una actualización directa de Framework 4.8 a Framework 10: son plataformas diferentes.

| Aspecto | Framework 4.8 de este sistema | .NET 10 como destino conceptual |
| --- | --- | --- |
| Plataforma | Windows | Multiplataforma, sujeto a las APIs utilizadas |
| Runtime | CLR de Framework 4.x | Runtime de .NET moderno |
| Instalación | Actualizaciones de la familia Framework 4.x en Windows | Versiones modernas pueden coexistir por aplicación |
| Proyecto | `.csproj` tradicional con imports y archivos explícitos | Proyecto con SDK y destino net10.0 |
| Web | ASP.NET MVC 5 y `System.Web` | ASP.NET Core MVC |

Microsoft describe estas diferencias en [.NET y .NET Framework para aplicaciones de servidor](https://learn.microsoft.com/en-us/dotnet/standard/choosing-core-framework-server). En este ejemplo, la dependencia web impide resolver el cambio solamente modificando el framework objetivo.

## MVC 5 y ASP.NET Core

ASP.NET MVC 5 organiza la aplicación en modelos, controllers y vistas. `System.Web.Mvc.Controller` recibe la petición, ejecuta una acción y devuelve un `ActionResult`; Razor genera HTML en el servidor. Model binding convierte datos HTTP a propiedades del modelo y Data Annotations describe sus validaciones.

ASP.NET Core es el framework web de .NET moderno. También puede usar MVC y Razor, pero tiene otras APIs, inicialización y pipeline de middleware. Las clases con nombres parecidos no son necesariamente intercambiables. La [guía oficial de MVC clásico a Core](https://learn.microsoft.com/en-us/aspnet/core/migration/fx-to-core/examples/overview?view=aspnetcore-10.0) muestra este cambio.

En el repositorio dependen de la plataforma `System.Web`:

- `Global.asax.cs`: hereda de `System.Web.HttpApplication` y registra la aplicación en `Application_Start`.
- `Controllers/HomeController.cs` y `Controllers/ClientesController.cs`: utilizan `System.Web.Mvc`.
- `App_Start/RouteConfig.cs`: usa `System.Web.Routing.RouteCollection` y `MapRoute`.
- `App_Start/BundleConfig.cs`: integra `System.Web.Optimization` con las vistas.
- `Views/Web.config` y `_Layout.cshtml`: configuran el host Razor clásico y usan sus helpers.
- `Web.config`: contiene `system.web`, el proveedor CodeDom y un handler ASP.NET para IIS.

`Models/Cliente.cs` usa propiedades C# y Data Annotations; `ClientesEnMemoria.cs` utiliza colecciones y `lock`. Ninguno referencia `System.Web`. Esa separación sencilla ayuda a distinguir lógica de datos e integración web al analizar el proyecto, sin introducir nuevas capas.

## IIS, IIS Express y Kestrel

IIS es el servidor web de Windows que administra sitios y procesos para atender peticiones. IIS Express es una variante destinada al desarrollo local, integrada con Visual Studio. Este proyecto se ejecuta con IIS Express y el pipeline de ASP.NET clásico; en producción el hosting tradicional sería IIS. Ver [IIS Express Overview](https://learn.microsoft.com/en-us/iis/extensions/introduction-to-iis-express/iis-express-overview).

Kestrel es el servidor HTTP multiplataforma de ASP.NET Core. Puede recibir peticiones directamente o estar detrás de un proxy. ASP.NET Core también puede alojarse en IIS: en el modelo fuera de proceso IIS reenvía a Kestrel; dentro de proceso utiliza IIS HTTP Server. Por ello IIS y Kestrel no son opciones excluyentes en todos los despliegues. Ver [implementaciones de servidor de ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/?view=aspnetcore-10.0).

| Hosting | Papel en el curso | Configuración a observar |
| --- | --- | --- |
| IIS Express | Ejecuta este sistema localmente en Windows | `UseIISExpress`, URL del `.csproj`, configuración local de Visual Studio |
| IIS | Hosting Windows del stack clásico; también admite Core con su módulo | Sitio, application pool, pipeline, handlers y `Web.config` |
| Kestrel | Servidor habitual del proyecto moderno futuro | Configuración de endpoints y arranque de la aplicación Core |

Cambiar el servidor requiere revisar el arranque y el pipeline web, además de puertos y certificados. Un servidor HTTP no reemplaza al runtime que ejecuta C#.

## NuGet y gestión de paquetes

NuGet distribuye bibliotecas, herramientas y recursos como paquetes versionados. Resolver o restaurar dependencias no equivale a ejecutar la aplicación.

`packages.config` mantiene una lista XML separada con versiones instaladas, incluidas dependencias transitivas. Este proyecto referencia archivos de `packages/` mediante `HintPath`; algunos paquetes también agregan archivos de contenido e imports al proyecto. Ver [referencia de packages.config](https://learn.microsoft.com/en-us/nuget/reference/packages-config).

`PackageReference` declara paquetes en el `.csproj`, resuelve el grafo de dependencias durante la restauración y normalmente usa la caché global de NuGet. No todo paquete antiguo con scripts de instalación o contenido se comporta igual al cambiar el formato. El formato de paquetes y el framework objetivo son decisiones relacionadas pero distintas. Ver [PackageReference](https://learn.microsoft.com/en-us/nuget/consume-packages/package-references-in-project-files).

En este módulo se conserva `packages.config`. El inventario exacto y sus usos están en [dependencias.md](dependencias.md).

## Tipos de problemas que habrá que evaluar

Una migración puede encontrar APIs ausentes o cambiadas, paquetes sin assets adecuados, dependencias de Windows, tareas MSBuild antiguas, cambios en configuración y diferencias de comportamiento HTTP. `System.Web.HttpContext` y el contexto de ASP.NET Core tienen APIs distintas, como explica la [guía de migración de HttpContext](https://learn.microsoft.com/en-us/aspnet/core/migration/fx-to-core/areas/http-context?view=aspnetcore-10.0).

Para este ejemplo conviene observar rutas, binding, validación, antiforgery, generación de enlaces y compilación de vistas. La existencia de CSS o JavaScript antiguo no demuestra incompatibilidad con el runtime del servidor. Una incompatibilidad confirmada debe apoyarse en APIs, metadatos o pruebas; cuando falta esa evidencia se registra como evaluación pendiente. Los cambios posibles se describen en [puntos-de-migracion.md](puntos-de-migracion.md), sin implementarlos aquí.
