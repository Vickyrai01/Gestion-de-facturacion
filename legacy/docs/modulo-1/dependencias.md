# Dependencias del Módulo 1

Inventario histórico de la rama `modulo-1-gap-tecnologico`. En la rama acumulativa reorganizada, los paquetes se ubican en `legacy/packages/`; usar `legacy/tools/RestaurarPaquetes.ps1` para restaurar la versión actual. Los comandos de este documento conservan las rutas de la rama histórica.

Inventario obtenido de [`packages.config`](../../legacyGestionDeFacturacion.web/packages.config) y del [proyecto web](../../legacyGestionDeFacturacion.web/legacyGestionDeFacturacion.web.csproj). Se conservaron las 18 entradas y sus versiones, sin agregar paquetes. Todos declaran `targetFramework="net48"`; esto registra el destino al instalarlos, no significa que todas sus bibliotecas solo admitan net48.

## Paquetes existentes

| Paquete | Versión | Uso en el proyecto | Revisión para el proyecto .NET 10 |
| --- | --- | --- | --- |
| Antlr | 3.5.0.2 | Runtime de análisis utilizado por WebGrease; referencia `Antlr3.Runtime`. | Evaluar si sigue siendo necesario y sus assets si se reutiliza. |
| bootstrap | 5.2.3 | Estilos y componentes del layout y vistas. | Recursos de navegador: evaluar frontend y bundles, sin asumir incompatibilidad de runtime .NET. |
| jQuery | 3.7.0 | Scripts base y soporte de validación. | Evaluar integración frontend; no es una DLL del servidor. |
| jQuery.Validation | 1.19.5 | Validación de formularios en el navegador. | Evaluar reglas e integración con el nuevo HTML. |
| Microsoft.AspNet.Mvc | 5.2.9 | Controllers, model binding, `ActionResult`, filtros y helpers MVC 5. | Depende del stack `System.Web`; no se reutiliza directamente como MVC de ASP.NET Core. |
| Microsoft.AspNet.Mvc.es | 5.2.9 | Recursos de localización de MVC 5 en español. | Sigue al paquete MVC 5; replantear localización al cambiar el stack. |
| Microsoft.AspNet.Razor | 3.2.9 | Analizador Razor de la plataforma ASP.NET clásica. | No asumir que este motor sea el Razor de ASP.NET Core; revisar y reemplazar la integración. |
| Microsoft.AspNet.Razor.es | 3.2.9 | Recursos en español del motor Razor clásico. | Revisar junto con Razor clásico. |
| Microsoft.AspNet.Web.Optimization | 1.1.3 | `BundleConfig`, `Styles.Render` y `Scripts.Render`. | La integración actual usa `System.Web`; revisar una estrategia de recursos para Core. |
| Microsoft.AspNet.Web.Optimization.es | 1.1.3 | Recursos en español de Optimization. | Revisar junto con el sistema de bundles. |
| Microsoft.AspNet.WebPages | 3.2.9 | Infraestructura de vistas, helpers y antiforgery (`System.Web.Helpers`). | ASP.NET Web Pages no está incluido en ASP.NET Core; revisar helpers y motor de vistas. |
| Microsoft.AspNet.WebPages.es | 3.2.9 | Recursos en español de WebPages. | Revisar junto con WebPages. |
| Microsoft.CodeDom.Providers.DotNetCompilerPlatform | 2.0.1 | Proveedor C#/VB configurado en `Web.config`, compilación dinámica y props de MSBuild. | Evaluar modelo de compilación; no trasladar automáticamente esta configuración a Core. |
| Microsoft.jQuery.Unobtrusive.Validation | 3.2.11 | Traduce atributos `data-val` generados por MVC a reglas jQuery Validation. | Evaluar integración con los helpers del proyecto nuevo. |
| Microsoft.Web.Infrastructure | 2.0.0 | Infraestructura de hosting de ASP.NET clásico, referenciada por el proyecto. | Evaluar usos transitivos; no conservarla sin necesidad comprobada. |
| Modernizr | 2.8.3 | Detección de capacidades del navegador; bundle del layout. | Evaluar si el frontend aún lo necesita. |
| Newtonsoft.Json | 13.0.3 | Biblioteca JSON ya referenciada; las nuevas acciones de clientes no la invocan. | Incluye assets net6.0 y netstandard2.0: no hay evidencia de incompatibilidad general con .NET moderno. Validar usos concretos. |
| WebGrease | 1.6.0 | Infraestructura de minificación de Optimization. | Evaluar APIs y necesidad; no se verificó su ejecución en .NET 10. |

La versión del paquete no siempre coincide con la versión del ensamblado: Razor/WebPages 3.2.9 usan referencias de ensamblado 3.0.0.0 y Newtonsoft.Json 13.0.3 usa un binding redirect a 13.0.0.0. No corregir estas diferencias como si fueran un error de restauración.

## Restauración tradicional

`packages.config` enumera paquetes con versiones exactas, incluidos paquetes transitivos instalados. Las referencias del `.csproj` apuntan a `packages/<id>.<versión>/lib/...`; CodeDom además importa un archivo `.props`. La carpeta está dentro del proyecto y ya estaba versionada en el repositorio inicial; se conservó esa decisión.

Desde Visual Studio, usar **Restaurar paquetes NuGet**. La alternativa explícita desde la raíz es:

```powershell
nuget restore .\legacyGestionDeFacturacion.web\packages.config -PackagesDirectory .\legacyGestionDeFacturacion.web\packages
```

Verificar que existan `packages/Microsoft.AspNet.Mvc.5.2.9/lib/net45/System.Web.Mvc.dll` y el `.props` de CodeDom antes de compilar. Restaurar recupera las versiones declaradas, sin actualizarlas. Este comportamiento está documentado en [NuGet Package Restore](https://learn.microsoft.com/en-us/nuget/consume-packages/package-restore).

## Compatibilidad: evidencias y tareas pendientes

La ficha oficial de [Microsoft.AspNet.Mvc 5.2.9](https://www.nuget.org/packages/Microsoft.AspNet.Mvc/5.2.9) y las referencias locales muestran su destino .NET Framework. La [guía de migración de Microsoft](https://learn.microsoft.com/en-us/aspnet/core/migration/fx-to-core/examples/overview?view=aspnetcore-10.0) explica el cambio de MVC clásico a ASP.NET Core MVC. Se confirma que los controllers y la integración `System.Web` actuales requieren adaptación; no basta copiar estas DLL a un proyecto net10.0.

ASP.NET Web Pages figura entre las [tecnologías de Framework ausentes en .NET moderno](https://learn.microsoft.com/en-us/dotnet/standard/choosing-core-framework-server). Esto no impide usar el lenguaje Razor en Core: el framework y su implementación cambian.

La ficha de [Newtonsoft.Json 13.0.3](https://www.nuget.org/packages/Newtonsoft.Json/13.0.3), junto con `packages/Newtonsoft.Json.13.0.3/lib`, muestra distintos destinos, incluidos net6.0 y netstandard2.0. Hay assets candidatos para .NET moderno; no se ejecutaron pruebas en .NET 10 ni se auditó cada API. Para Antlr, WebGrease, CodeDom e Infrastructure la compatibilidad concreta queda pendiente. Una biblioteca de Framework requiere examinar sus APIs y dependencias; su TFM por sí solo no prueba todos los escenarios posibles.

Para cada paquete en el proyecto paralelo: identificar usos directos y transitivos, examinar TFMs/assets y APIs, consultar al proveedor, determinar si se conserva o reemplaza y probar el comportamiento. Los archivos CSS/JavaScript se evalúan por su integración y compatibilidad con el navegador. Una revisión de actualizaciones o seguridad queda como actividad separada; este módulo no declara que las versiones existentes sean las más recientes.
