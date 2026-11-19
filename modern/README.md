# Sistema de Gestión de Facturación — Monolito Modular (.NET 10)

> **Clase 7 — Refactor de Arquitectura .NET**
> **Objetivo:** Repensar la arquitectura legacy (`.NET Framework 4.8` en `legacy/`) y el primer acercamiento por capas horizontales (`Sistema-facturacion/` de las Clases 4 a 6) hacia un **Monolito Modular** con **Clean Architecture** y **DDD** en `modern/`.

---

## 1. Mapa de la solución (`modern/Facturacion.Modern.slnx`)

```text
modern/
├── Directory.Build.props              # TargetFramework net10.0, Nullable, TreatWarningsAsErrors
├── Directory.Packages.props           # Central Package Management (versiones NuGet unificadas)
├── Facturacion.Modern.slnx            # Solución completa del Monolito Modular
├── src/
│   ├── BuildingBlocks/
│   │   ├── Facturacion.BuildingBlocks/                 # Kernel puro (Result<T>, Error, Entidad, Validar, Dinero, Roles)
│   │   └── Facturacion.BuildingBlocks.Infrastructure/  # Configuración EF Core (InMemory / SQL Server) y traducción HTTP
│   ├── Modules/
│   │   ├── Clientes/
│   │   │   ├── Facturacion.Modules.Clientes.Contracts/ # Contrato público: IConsultaClientes + ClienteCatalogoDto
│   │   │   └── Facturacion.Modules.Clientes/           # Domain, Application, Infrastructure (internal), Presentation
│   │   ├── Productos/
│   │   │   ├── Facturacion.Modules.Productos.Contracts/# Contrato público: IConsultaProductos + ProductoCatalogoDto
│   │   │   └── Facturacion.Modules.Productos/          # Domain, Application, Infrastructure (internal), Presentation
│   │   ├── Facturacion/
│   │   │   └── Facturacion.Modules.Facturacion/        # Aggregate Root Factura, ServicioFacturacion, Worker, Controller
│   │   └── Usuarios/
│   │       └── Facturacion.Modules.Usuarios/           # Login JWT, BCrypt legacy, ControlIntentosLogin, UsuariosController
│   └── Facturacion.Api/                                # Host (Composition Root): ensambla los 4 módulos en un proceso
└── tests/
    └── Facturacion.Tests/                              # Fitness Functions de arquitectura + tests unitarios DDD + integración HTTP
```

---

## 2. Cómo se cubren los 5 temas de la Clase 7 en el código

| Tema de la Clase 7 | Problema en `legacy/` y `Sistema-facturacion/` | Solución implementada en `modern/` |
| :--- | :--- | :--- |
| **1. Monolito a Modular Monolith** | Un único `FacturacionDbContext` global con las 5 tablas (`Clientes`, `Productos`, `Facturas`, `DetallesFactura`, `Usuarios`) y navegaciones cruzadas entre todos los conceptos. | 4 módulos verticales autónomos (`Clientes`, `Productos`, `Facturacion`, `Usuarios`), cada uno con su propio `DbContext` (`internal`). `Facturacion` sólo referencia `Clientes.Contracts` y `Productos.Contracts`. |
| **2. Separación de capas** | En `legacy/`, las vistas, controllers, WCF y Windows Service tocaban directamente `FacturacionDbContext`. En `Sistema-facturacion/`, `Facturacion.Services` referenciaba `Facturacion.Infrastructure` (dirección invertida). | Dentro de cada módulo: `Domain` (0 dependencias externas) $\leftarrow$ `Application` (casos de uso + puertos) $\leftarrow$ `Infrastructure` / `Presentation`. |
| **3. Eliminación de lógica en controllers** | `ProductosController` normalizaba códigos, redondeaba precios y atrapaba `SqlException` `2601`/`2627`. `FacturasController` leía `ConfigurationManager.AppSettings["TasaImpuestos"]` y armaba reportes `GroupBy` sobre `db.Facturas`. `AccountController` manejaba el rate-limit y el anti-timing attack. | Todos los controllers (`ClientesController`, `ProductosController`, `FacturasController`, `AuthController`, `UsuariosController`) tienen acciones de 1 a 4 líneas que delegan en el servicio de aplicación y traducen `Result<T>` a HTTP (`ProblemDetails`). |
| **4. Desacople DTO / repositorios** | En `Sistema-facturacion/`, los controllers recibían y devolvían `Persistence.Models.Cliente`, `Producto` y `Usuario` (exponiendo incluso `Passwordhash` en `/api/usuarios`), y usaban un `IRepository<T>` genérico acoplado a las tablas. | Contratos de entrada (`*Request` / `*Comando`) y DTOs de salida (`ClienteDto`, `ProductoDto`, `FacturaDetalleDto`, `UsuarioDto` sin hash). Repositorios específicos de dominio por agregado (`IRepositorioClientes`, `IRepositorioProductos`, `IRepositorioFacturas`, `IRepositorioUsuarios`). |
| **5. Clean Architecture / DDD** | Entidades anémicas con setters públicos y `[Required]`/`[Range]`; lógica de facturación en un método estático procedural (`FacturacionOperaciones.Crear`); `FacturasService.cs` vacío en `Sistema-facturacion/`. | Entidades ricas sin setters públicos (`Cliente`, `Producto`, `Usuario`), Value Objects (`EmailCliente`, `CodigoProducto`) y `Factura` como **Aggregate Root** que protege `Detalles`, calcula subtotales/impuestos/total con `MidpointRounding.AwayFromZero` y valida las transiciones `Emitir()` y `Anular()`. |

---

## 3. Ejecución rápida para la clase

### Compilar y correr todos los tests (Arquitectura + Dominio + Integración)

```powershell
dotnet test --solution .\modern\Facturacion.Modern.slnx
```

### Levantar la API del Monolito Modular

```powershell
dotnet run --project .\modern\src\Facturacion.Api\Facturacion.Api.csproj
```

* Por defecto arranca con `Persistencia:Proveedor = "InMemory"` y **carga automáticamente los mismos datos de demostración del legacy**:
  * **Usuarios:** `admin` (contraseña: `FACTURACION_ADMIN_PASSWORD`) y `operador` (contraseña: `FACTURACION_OPERADOR_PASSWORD`).
  * **Clientes:** 5 clientes (`Ana García`, `Luis Pérez`, `Marta López` [inactiva], `Diego Ruiz`, `Sofía Torres`).
  * **Productos:** 8 productos (`PROD-001` a `PROD-008`).
  * **Facturas:** 3 facturas iniciales (`DEMO-001` en `PENDIENTE`, `DEMO-002` en `EMITIDA`, `DEMO-003` en `ANULADA`).
* Si se quiere conectar contra la base SQL Server / LocalDB del legacy (`FacturacionLegacyModulo2`), basta con cambiar en `appsettings.json` `"Persistencia:Proveedor": "SqlServer"`.

### Explorar endpoints

* **Swagger UI:** `http://localhost:5246/swagger`
* **Documento OpenAPI:** `http://localhost:5246/openapi/v1.json`
* **Colección de pruebas HTTP para VS / VS Code:** [Facturacion.Api.http](src/Facturacion.Api/Facturacion.Api.http)
