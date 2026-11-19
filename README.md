# Sistema de Gestión de Facturación — Curso de Migración .NET Framework 4.8 a .NET 10

Repositorio de referencia para la capacitación práctica de migración desde **.NET Framework 4.8** hacia **.NET 10**.

## Estructura del repositorio en la Clase 7 (`clase-7`)

```text
Gestion-de-facturacion/
├── legacy/                   # Sistema original en .NET Framework 4.8 (MVC 5, EF6, WCF, Windows Service)
├── Sistema-facturacion/      # Evolución técnica de las Clases 4, 5 y 6 (.NET 10 por capas horizontales)
└── modern/                   # Clase 7: Refactor de Arquitectura a Monolito Modular + Clean Architecture + DDD (.NET 10)
```

---

## Clase 7: Refactor de Arquitectura .NET (`modern/`)

En [modern/Facturacion.Modern.slnx](modern/Facturacion.Modern.slnx) se encuentra la migración completa del sistema legacy a un **Monolito Modular** en **.NET 10**, diseñada para dictar el laboratorio sobre código propio cubriendo los 5 ejes del temario:

1. **Monolito a Modular Monolith:** División vertical en 4 módulos autónomos (`Clientes`, `Productos`, `Facturacion`, `Usuarios`) con proyectos `.Contracts` para comunicación en memoria y un `DbContext` `internal` por módulo.
2. **Separación de capas:** `Domain` $\leftarrow$ `Application` $\leftarrow$ `Infrastructure` / `Presentation` dentro de cada módulo.
3. **Eliminación de lógica en controllers:** Controllers de 1 a 4 líneas por acción, sin referencias a `DbContext`, sin `ConfigurationManager` y sin bloques `try/catch` repetitivos.
4. **Desacople DTO / repositorios:** Requests y DTOs específicos por caso de uso (`UsuarioDto` nunca expone `PasswordHash`; las entidades de dominio no tienen propiedades de navegación circulares entre módulos) y repositorios orientados al agregado.
5. **Clean Architecture / DDD:** Entidades con invariantes encapsuladas, Value Objects (`EmailCliente`, `CodigoProducto`), patrón `Result<T>` / `Error`, y `Factura` como **Aggregate Root** que gobierna sus `Detalles`, cálculos financieros y transiciones de estado (`Emitir`, `Anular`).

```powershell
# Ejecutar tests de arquitectura (Fitness Functions), tests unitarios DDD y tests de integración HTTP
dotnet test --solution .\modern\Facturacion.Modern.slnx

# Levantar el Host del Monolito Modular (http://localhost:5246/swagger)
dotnet run --project .\modern\src\Facturacion.Api\Facturacion.Api.csproj
```

---

## Contexto histórico de los módulos anteriores

* **Módulos 1 y 2 (`legacy/`):** Abrir [legacy/SistemaFacturacion.Legacy.slnx](legacy/SistemaFacturacion.Legacy.slnx). Guías en [legacy/docs/modulo-1/README.md](legacy/docs/modulo-1/README.md) y [legacy/docs/modulo-2/README.md](legacy/docs/modulo-2/README.md).
* **Clases 4, 5 y 6 (`Sistema-facturacion/`):** Migración técnica inicial a .NET 10 (EF Core, gRPC, BackgroundService, Identity/JWT). Sirve como punto de comparación en la Clase 7 para mostrar por qué una migración técnica 1:1 necesita luego un refactor arquitectónico.
