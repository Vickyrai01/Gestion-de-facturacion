# Trabajo paralelo con el Módulo 4

Las secciones siguientes describen el alcance original del Módulo 3 antes del merge. El estado integrado está en la nota final.

Módulo 3 parte de `modulo-2-migracion-tecnica`, commit `cad49ff`. El Módulo 4 también parte de esa base según la indicación del equipo; no se asume que ya incluya este refactor. No se ejecutan merge, rebase, commit ni push automáticamente.

## Cambios existentes compartidos

| Archivo, relativo a legacy/ | Cambio de esta rama | Qué coordinar si también cambia en el Módulo 4 |
| --- | --- | --- |
| `FacturacionLegacy.Datos/FacturacionOperaciones.cs` | Extrae cálculo/SQL a componentes y conserva firmas públicas mediante una fachada. | Conservar métodos/firmas agregados por la otra rama; trasladar una regla al componente correspondiente para no mantener dos cálculos diferentes. |
| `FacturacionLegacy.Datos/FacturacionLegacy.Datos.csproj` | Incluye los nuevos archivos/carpetas de Clientes, Productos y Facturación. | Combinar únicamente las entradas necesarias, conservando las inclusiones de ambas ramas. No cambiar paquetes ni framework. |
| `legacyGestionDeFacturacion.web/Controllers/FacturasController.cs` | Sustituye acceso EF por servicio; mantiene acciones, rutas, permisos y respuestas. | Adaptar nuevas acciones o reglas del Módulo 4 al servicio cuando corresponda, sin eliminar funcionalidades. |
| `legacyGestionDeFacturacion.web/legacyGestionDeFacturacion.web.csproj` | Incluye Composicion/FacturacionWeb.cs. | Conservar las entradas de ambas ramas. |
| `legacyGestionDeFacturacion.web/Views/Facturas/Index.cshtml` | Usa FacturaResumenDto y campos de cliente explícitos. | Revisar tipo de modelo y campos si aparecen columnas nuevas. |
| `legacyGestionDeFacturacion.web/Views/Facturas/Details.cshtml` | Usa FacturaDetalleDto y detalles sin navegaciones EF. | Mapear los campos nuevos que necesite la vista. |
| `legacyGestionDeFacturacion.web/Views/Facturas/Reportes.cshtml` | Usa ReporteEstadoDto. | Conservar nuevos datos de reporte si los hubiera. |

Los contratos, dominio, servicio, adaptadores, documentación y pruebas nuevos se agregan en rutas específicas; aun así, nombres coincidentes con archivos nuevos de otra rama podrían requerir revisión. No se inspeccionó el PR del Módulo 4: esta tabla identifica puntos probables, no conflictos confirmados.

## Superficie preservada

No se modifican la solución, paquetes NuGet, Web.config/App.config, rutas, entidades, DbContext, migraciones, CRUD de clientes/productos, autenticación, código/contratos WCF, Windows Service, script integrado del Módulo 2, README general ni modern/. No se movieron archivos existentes. Esto acota el cambio sin negar que FacturacionOperaciones y el controller sean puntos compartidos importantes.

Antes de integrar manualmente los trabajos, comparar ambos cambios con su base común y acordar el destino de cada regla. Después compilar las vistas, ejecutar `ValidarModulo3.ps1` y las pruebas integradas, además de las verificaciones nuevas del Módulo 4. No resolver un conflicto sobrescribiendo por completo los archivos de una de las ramas.

## Estado integrado

La rama modulo-4-modernizacion ya contiene Módulo 3 (`0772704`) y Módulo 4 (`d0822e6`) mediante el merge normal `71d866e`. No se encontraron rutas modificadas por ambas ramas desde cad49ff. Los ajustes posteriores eliminan el DTO/consulta de productos y contrato de pendientes duplicados del ejecutable, y usan los contratos compartidos. Se conservan configuración, negocio y composición manual. Ver [resoluciones y pruebas pendientes](../../../docs/modulo-4/integracion-modulo-3.md). La rama del Módulo 3 no se modifica.
