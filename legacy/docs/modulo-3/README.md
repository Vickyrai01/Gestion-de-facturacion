# Módulo 3 — Refactor de arquitectura

Esta rama `modulo-3-arquitectura` parte de `modulo-2-migracion-tecnica`, commit `cad49ff`. Refactoriza el flujo de facturación del mismo sistema dentro de `legacy/`, conservando .NET Framework 4.8, MVC 5, EF6, SQL Server, Forms Authentication, WCF y Windows Service. No crea proyectos ni tablas, no cambia las rutas y no modifica `modern/`.

El alcance es progresivo: Clientes y Productos ofrecen consultas de catálogo al caso de facturación, pero sus CRUD conservan la implementación del Módulo 2. Usuarios y seguridad tampoco se refactorizan. Se mantiene un monolito con límites lógicos más claros, sin prometer aislamiento completo de módulos o una implementación integral de Clean Architecture.

## Lectura para las clases

1. [Diagnóstico](diagnostico.md): evidencia del código previo y aspectos que ya estaban correctamente resueltos.
2. [Arquitectura](arquitectura.md): antes/después, responsabilidades, dirección de dependencias y evolución a monolito modular.
3. [Recorrido de una factura](recorrido-factura.md): desde POST hasta la transacción y el resultado HTTP.
4. [Contratos y repositorios](contratos-y-repositorios.md): DTOs, consultas, persistencia y composición manual.
5. [Clean Architecture, DDD y patrones](principios-y-patrones.md): subconjunto aplicado y propuestas que no se implementaron.
6. [Trabajo paralelo](trabajo-paralelo.md): alcance y archivos que podrían coincidir con cambios del Módulo 4.
7. [Validación](validacion.md): pruebas ejecutadas, comandos y límites de la evidencia.

Todas las rutas de código citadas en estas guías parten de `legacy/`.

## Proyectos y archivos

Los cinco proyectos y sus referencias se conservan en [SistemaFacturacion.Legacy.slnx](../../SistemaFacturacion.Legacy.slnx): MVC, Datos, WCF, WindowsService y Tools. Dentro de los dos proyectos afectados, la estructura relevante queda así:

```text
FacturacionLegacy.Datos/
  Clientes/
    Contratos.cs
    ConsultaClientesEf6.cs
  Productos/
    Contratos.cs
    ConsultaProductosEf6.cs
  Facturacion/
    Contratos.cs
    Aplicacion/ServicioFacturacion.cs
    Dominio/FacturaEnPreparacion.cs
    Infraestructura/RepositorioFacturasEf6.cs
  Entidades/                         # modelo EF existente, sin cambios
  Migraciones/                       # esquema existente, sin cambios
  FacturacionDbContext.cs            # configuración EF existente
  FacturacionOperaciones.cs          # fachada compatible con el Módulo 2
legacyGestionDeFacturacion.web/
  Composicion/FacturacionWeb.cs
  Controllers/FacturasController.cs
  Views/Facturas/                    # listado, detalle y reporte con DTOs
```

La clase de entrada `LineaFactura` ya existía y se reutiliza: no es una entidad EF. `CrearFacturaRequest` agrupa cliente y líneas. El dominio produce una factura calculada con detalles de solo lectura y el repositorio la convierte a las entidades existentes. No se agregaron paquetes NuGet, contenedores de DI, repositorios genéricos, eventos de dominio ni infraestructura de mensajería.

## Preparar y ejecutar

Abrir `legacy/SistemaFacturacion.Legacy.slnx` en Visual Studio para Windows. Los requisitos, base LocalDB, cuentas locales y servicios se preparan igual que en el [Módulo 2](../modulo-2/README.md). No se necesita una migración de esquema para este refactor.

Desde la raíz del repositorio en Developer PowerShell:

```powershell
MSBuild .\legacy\SistemaFacturacion.Legacy.slnx /t:Build /p:Configuration=Debug /p:MvcBuildViews=true
powershell -NoProfile -ExecutionPolicy Bypass -File .\legacy\tools\ValidarModulo3.ps1 -OmitirCompilacion
powershell -NoProfile -ExecutionPolicy Bypass -File .\legacy\tools\ValidarModulo2.ps1 -OmitirCompilacion
```

La prueba nueva no requiere SQL Server: compila un ejecutable de prueba de .NET Framework en una carpeta temporal y la elimina al terminar. La segunda prueba usa LocalDB e IIS Express con recursos temporales propios. La aplicación MVC sigue iniciándose con IIS Express/HTTPS y las mismas cuentas, rutas y permisos.

## Demostrar antes y después

Consultar el código anterior sin cambiar de rama ni descartar trabajo:

```powershell
git show cad49ff:legacy/legacyGestionDeFacturacion.web/Controllers/FacturasController.cs
git show cad49ff:legacy/FacturacionLegacy.Datos/FacturacionOperaciones.cs
git diff cad49ff -- legacy/legacyGestionDeFacturacion.web/Controllers/FacturasController.cs
```

Colocar breakpoints en `FacturasController.Create`, `ServicioFacturacion.Crear`, `FacturaEnPreparacion.Crear` y `RepositorioFacturasEf6.Guardar`. Crear una factura con 125.50×2 y 100×3 a tasa 0.21: subtotal 551.00, impuestos 115.71 y total 666.71. Ejecutar después las pruebas sin SQL para mostrar que las reglas y la coordinación se prueban sin arrancar la web. Comparar la estructura y dependencias, sin afirmar que mover código a carpetas por sí solo cambió el despliegue o completó la modularización.

En el curso, el traslado a .NET 10 se realizará en el proyecto independiente de `modern/`. Estos contratos facilitan analizar dependencias, pero todavía habrá que adaptar MVC, persistencia, composición, seguridad y consumidores. No se implementa ese traslado aquí.
