# Sistema de Gestión de Facturación

Aplicación de referencia para una capacitación de migración desde .NET Framework a .NET moderno. Esta rama acumula el Módulo 1 y el Módulo 2: clientes, productos, facturas, EF6/SQL Server, consultas SOAP WCF, Windows Service y Forms Authentication, conservando ASP.NET MVC 5 sobre .NET Framework 4.8.

El repositorio separa los dos sistemas en carpetas independientes:

```text
SistemaFacturacion/
  legacy/     # Sistema existente en .NET Framework 4.8
  modern/     # Espacio reservado para el futuro proyecto .NET 10
```

Abrir [legacy/SistemaFacturacion.Legacy.slnx](legacy/SistemaFacturacion.Legacy.slnx) en Visual Studio para Windows. Dentro de `legacy/` se conservan los cinco proyectos existentes, sus paquetes NuGet, herramientas y documentación. Para restaurar y compilar desde Developer PowerShell:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\legacy\tools\RestaurarPaquetes.ps1
MSBuild .\legacy\SistemaFacturacion.Legacy.slnx /t:Build /p:Configuration=Debug /p:MvcBuildViews=true
```

Ejecutar el proyecto MVC con IIS Express. La preparación de base de datos y cuentas locales está en la guía del Módulo 2. `modern/` solo contiene un [README explicativo](modern/README.md); todavía no existe un proyecto .NET 10.

La [guía del Módulo 2](legacy/docs/modulo-2/README.md) explica preparación de la base, cuentas locales, ejecución y material de las clases 3, 4 y 5. La [guía del Módulo 1](legacy/docs/modulo-1/README.md) conserva el contexto histórico de su rama y las clases 1 y 2.

Los módulos evolucionan el mismo sistema legacy mediante ramas acumulativas; no hay carpetas con copias del código por módulo. La reorganización está aplicada en `modulo-2-migracion-tecnica`; las ramas históricas conservan su estructura original y las siguientes ramas heredarán `legacy/`. La documentación por módulo es material de capacitación, no una copia de la aplicación.
