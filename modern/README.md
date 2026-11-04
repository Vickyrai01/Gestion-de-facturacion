# Sistema moderno de gestión de facturación

Migración del sistema legacy a .NET 10 con Clean Architecture y Domain-Driven Design.

## Estructura

- `Facturacion.Modern.Domain`: entidades, agregados y reglas de negocio.
- `Facturacion.Modern.Application`: casos de uso y puertos.
- `Facturacion.Modern.Infrastructure`: repositorios en memoria y hashing PBKDF2.
- `Facturacion.Modern.Api`: ASP.NET Core Minimal API y composición de dependencias.

No se migró Entity Framework, SQL Server, WCF ni el Windows Service. Toda la información se conserva en memoria y se pierde al reiniciar.

## Ejecutar

```powershell
dotnet run --project .\Facturacion.Modern.Api\Facturacion.Modern.Api.csproj
```

Usuarios demo: `admin` / `FACTURACION_ADMIN_PASSWORD` y `operador` / `FACTURACION_OPERADOR_PASSWORD`.

Endpoints principales: `/api/auth/login`, `/api/clientes`, `/api/productos`, `/api/facturas` y `/api/reportes/pendientes`.
