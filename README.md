# Sistema de Gestión de Facturación

Aplicación de referencia para una capacitación de migración desde .NET Framework a .NET moderno. El sistema reúne los contenidos de los cuatro módulos: clientes, productos, facturas, EF6/SQL Server, consultas SOAP WCF, Windows Service, Forms Authentication, separación de responsabilidades, consultas asíncronas y logging estructurado, conservando ASP.NET MVC 5 sobre .NET Framework 4.8.

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

El material del curso se organiza en cuatro módulos:

1. [Análisis del sistema legacy](legacy/docs/modulo-1/README.md): runtime, hosting, dependencias y puntos de migración.
2. [Tecnologías legacy](legacy/docs/modulo-2/README.md): EF6, SQL Server, cuentas locales, WCF y Windows Service.
3. [Arquitectura](legacy/docs/modulo-3/README.md): dominio, aplicación, contratos, repositorios y composición manual.
4. [Modernización de código](docs/modulo-4/README.md): async/await, DI, logging, background jobs y comparación con .NET 10.

Los módulos estudian y evolucionan el mismo sistema. La documentación incluye ejemplos iniciales y recorridos sobre la aplicación actual para comparar responsabilidades y tecnologías.
