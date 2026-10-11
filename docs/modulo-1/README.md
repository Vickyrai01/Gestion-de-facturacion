# Módulo 1: Gap Tecnológico .NET — clases 1 y 2

Este material describe la rama `modulo-1-gap-tecnologico`. En la rama acumulativa del Módulo 2, el almacenamiento en memoria se reemplazó por EF6 y las entidades se trasladaron a una biblioteca compartida; para ejecutar esa versión usar [la guía del Módulo 2](../modulo-2/README.md). Para recorrer los archivos originales del Módulo 1, consultar su rama.

El Sistema de Gestión de Facturación es una aplicación legacy que servirá como referencia para construir otro sistema independiente en .NET 10 durante la capacitación. En este módulo permite administrar clientes; todavía no emite facturas.

## Tecnologías y alcance

Un único proyecto web C# tradicional, no SDK, con .NET Framework 4.8, ASP.NET MVC 5.2.9 (`System.Web.Mvc`) y Razor 3.2.9. Conserva `Global.asax`, `App_Start`, routing convencional, `Web.config`, `ActionResult`, vistas `.cshtml`, bundles e IIS Express. NuGet utiliza `packages.config` y referencias con `HintPath` a `packages/`. La presentación usa Bootstrap 5.2.3, jQuery 3.7.0 y validación unobtrusive ya existentes.

No se agregaron ni actualizaron paquetes. No hay ASP.NET Core, base de datos, Entity Framework, autenticación, WCF, servicios de Windows, contenedores ni arquitectura avanzada.

## Requisitos y apertura en Windows

1. Instalar Visual Studio para Windows con la carga de trabajo **Desarrollo de ASP.NET y web**, las herramientas de proyectos web de .NET Framework y el Developer Pack/Targeting Pack de .NET Framework 4.8.
2. Tener IIS Express y el runtime .NET Framework 4.8 o una actualización compatible de la familia 4.x. El runtime ejecuta la aplicación; las referencias del Developer Pack permiten compilar para 4.8.
3. Abrir `legacyGestionDeFacturacion.web/legacyGestionDeFacturacion.web.slnx`. La solución XML existente contiene solamente `legacyGestionDeFacturacion.web.csproj`; usar una versión de Visual Studio que admita `.slnx` (el entorno de validación usa Visual Studio 2026). Si el IDE no reconoce el formato, abrir el `.csproj` directamente como proyecto o actualizar el IDE; no es necesario convertir el framework objetivo.
4. Establecer el proyecto web como proyecto de inicio.

## Restaurar NuGet

En el Explorador de soluciones, seleccionar **Restaurar paquetes NuGet**. Habilitar la restauración en las opciones de NuGet si estuviera desactivada. El repositorio ya contiene la carpeta de paquetes, pero también debe poder restaurarse cuando falte. Las versiones están fijadas en `packages.config`.

Para ubicar los paquetes exactamente donde los espera este `.csproj`, ejecutar desde la raíz con `nuget.exe` disponible:

```powershell
nuget restore .\legacyGestionDeFacturacion.web\packages.config -PackagesDirectory .\legacyGestionDeFacturacion.web\packages
```

Esta operación puede requerir acceso a nuget.org. No usar una actualización de paquetes como reemplazo de una restauración. Más detalles en [dependencias.md](dependencias.md).

## Compilar y ejecutar

En Visual Studio, seleccionar **Compilar solución**, configuración Debug. Desde Developer PowerShell de Visual Studio también se puede compilar directamente el proyecto:

```powershell
MSBuild .\legacyGestionDeFacturacion.web\legacyGestionDeFacturacion.web.csproj /t:Build /p:Configuration=Debug /p:MvcBuildViews=true
```

`MvcBuildViews=true` valida además las vistas Razor; el valor habitual del proyecto sigue siendo `false`. Usar MSBuild de Visual Studio con los targets de aplicaciones web y las referencias de Framework 4.8, no solamente el SDK de .NET moderno.

Seleccionar **IIS Express** y presionar F5 (con depuración) o Ctrl+F5 (sin depuración). La URL configurada en el proyecto es `https://localhost:44372/`; Visual Studio puede asignar otra según la configuración local. Si se solicita, confiar en el certificado de desarrollo de IIS Express. Ingresar a `/` y después a `/Clientes`.

Para una ejecución HTTP local de prueba, sin abrir Visual Studio, después de compilar:

```powershell
& "$env:ProgramFiles\IIS Express\iisexpress.exe" /path:"$PWD\legacyGestionDeFacturacion.web" /port:60121 /systray:false
```

Visitar `http://localhost:60121/` y detener esa instancia con Ctrl+C al terminar. El sitio usa el pipeline integrado de ASP.NET 4.x; [la configuración de handlers de IIS](https://learn.microsoft.com/en-us/iis/configuration/system.webserver/handlers/add) permite incorporar PATCH para las rutas sin extensión.

## Funcionalidades y rutas

| Operación | Ruta | Verbo |
| --- | --- | --- |
| Inicio | `/` | GET |
| Listar clientes activos e inactivos | `/Clientes` | GET |
| Consultar un cliente | `/Clientes/Details/1` | GET |
| Mostrar el alta / guardar un cliente | `/Clientes/Create` | GET / POST |
| Mostrar la edición / guardar cambios | `/Clientes/Edit/1` | GET / POST |
| Desactivar sin borrar | `/Clientes/Desactivar/1` | POST o PATCH |

Todos los campos personales son obligatorios; email tiene validación de formato y los textos tienen longitudes máximas. La validación corre en el servidor con Data Annotations y también se presenta en el navegador con los scripts existentes. Las escrituras requieren token antiforgery y su cookie. Un ID ausente o inexistente produce 404 en detalle, edición y desactivación.

Los formularios de desactivación envían POST y `Html.HttpMethodOverride(HttpVerbs.Patch)`; MVC interpreta el verbo efectivo como PATCH. La acción también admite POST sin override y PATCH real con cuerpo `application/x-www-form-urlencoded`, token `__RequestVerificationToken` y la cookie correspondiente. No es una API JSON ni utiliza JSON Patch. `Web.config` habilita PATCH en el handler de URLs sin extensión.

El almacenamiento estático inicia con tres clientes de ejemplo, asigna IDs crecientes dentro de un `lock` y comparte datos entre usuarios y peticiones del mismo AppDomain. Devuelve copias de los objetos. El alta siempre crea un cliente activo; editar datos personales conserva su estado; desactivar es idempotente. No hay eliminación física ni reactivación.

Los datos se pierden al reiniciar o reciclar la aplicación, incluso cuando cambia `Web.config`, y no se comparten entre procesos. La última edición válida prevalece si dos usuarios editan al mismo tiempo. No se exige documento o email único; el único identificador garantizado como único es `Id`. Estas limitaciones son deliberadas para el ejemplo educativo.

## Estructura y material de clase

```text
legacyGestionDeFacturacion.web/
  legacyGestionDeFacturacion.web.slnx
  legacyGestionDeFacturacion.web.csproj
  Global.asax / Global.asax.cs
  App_Start/                  # rutas, filtros y bundles
  Controllers/                # HomeController y ClientesController
  Models/                     # Cliente y ClientesEnMemoria
  Views/Home/Index.cshtml
  Views/Clientes/              # cuatro vistas y dos parciales
  Views/Shared/_Layout.cshtml
  Content/ y Scripts/         # recursos existentes
  Web.config / packages.config / packages/
docs/modulo-1/
  README.md
  dependencias.md
  gap-tecnologico.md
  recorrido-codigo.md
  puntos-de-migracion.md
  validacion.md
```

Para la clase 1, comparar runtimes y hosting usando [gap-tecnologico.md](gap-tecnologico.md) y seguir una petición con [recorrido-codigo.md](recorrido-codigo.md). Para la clase 2, revisar `packages.config`, `HintPath`, restauración y compatibilidad en [dependencias.md](dependencias.md); elaborar el inventario de cambios con [puntos-de-migracion.md](puntos-de-migracion.md). Demostrar un alta, una edición, una desactivación y el reinicio de los datos. Los controles realizados y sus límites están en [validacion.md](validacion.md).

## Ramas y módulos posteriores

`main` conserva el proyecto inicial. `modulo-1-gap-tecnologico` parte de `main`; el trabajo se entrega sin commits ni pushes automáticos. El Módulo 2 partirá del resultado de esta rama; luego seguirán `modulo-3-arquitectura` y `modulo-4-modernizacion`, cada uno desde el módulo anterior.

En el Módulo 2 habrá que definir e incorporar nuevas funcionalidades de facturación y las tecnologías legacy necesarias para su temario, incluyendo persistencia si se acuerda en su especificación. Entity Framework, autenticación, WCF y servicios de Windows quedan para módulos posteriores según el programa; no se fija aquí una asignación que aún no fue especificada. El Módulo 3 evolucionará la arquitectura legacy y el Módulo 4 sumará casos de modernización. La versión .NET 10 se desarrollará en otro proyecto independiente durante el curso.
