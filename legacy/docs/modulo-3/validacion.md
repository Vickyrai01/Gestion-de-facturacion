# Validación del Módulo 3

Fecha: 21 de octubre de 2026, America/Buenos_Aires. Entorno: Windows, Visual Studio/MSBuild, .NET Framework 4.8, IIS Express y SQL Server LocalDB.

## Pruebas de arquitectura ejecutadas

`tools/ValidarModulo3.ps1` compiló el legacy en Debug con vistas Razor y ejecutó 48 verificaciones correctas sin SQL Server. La prueba C# está en `tools/pruebas-modulo-3/PruebasArquitectura.cs`; se compila en un directorio temporal propio, sin agregar un proyecto a la solución.

Se comprobaron cálculos decimales, redondeo de impuestos, UTC, cantidades/líneas válidas e inválidas, precios ausentes/negativos/excesivos, tasa entre 0 y 1, precio cero y líneas repetidas. También se comprobó que el resultado calculado no conserva referencias mutables de entrada y que no permite quitar sus detalles.

Con dependencias de prueba se verificaron cliente/producto inactivo o inexistente, orden de llamadas dentro de la transacción, confirmación únicamente después del guardado y Dispose sin confirmación ante error. Esto demuestra la coordinación del servicio; un doble de prueba no demuestra por sí solo un rollback real de SQL Server.

El controller se ejecutó mediante su constructor de interfaz, sin DbContext, comprobando DTOs de lista/detalle, redirección al crear, ModelState inválido y HTTP 400/404/409. La tasa no se lee al consultar y se utiliza una sola lectura por creación.

## Comandos reproducibles

Desde la raíz del repositorio:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\legacy\tools\ValidarModulo3.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\legacy\tools\ValidarModulo2.ps1 -OmitirCompilacion
```

El segundo comando conserva la suite integrada existente y requiere acceso a LocalDB/IIS Express, certificado de desarrollo y puertos 44374/60122 disponibles. Utiliza una base temporal independiente y cuentas aleatorias, sin modificar la base principal. La compilación solo debe omitirse si los binarios corresponden al código actual.

## Integración y compilación ejecutadas

Después del refactor, `tools/ValidarModulo2.ps1 -OmitirCompilacion` pasó sus 63 verificaciones con código de salida 0. Se comprobó gestión de clientes/productos, validaciones, login/logout y roles, creación/cálculo de facturas, precios históricos, persistencia tras reiniciar MVC, transiciones, filtros, cuatro altas concurrentes con números únicos, desactivación PATCH, WSDL y las tres consultas SOAP, además de consulta periódica de pendientes y detención normal. La prueba dispuso sus instancias de IIS Express y eliminó únicamente su base temporal.

La solución compiló en Debug y Release con `MvcBuildViews=true`; ambas configuraciones terminaron con código 0. Se conservan los cinco proyectos, .NET Framework 4.8, MVC 5 y EF6. No se agregaron paquetes ni migraciones. `git diff` confirmó ausencia de cambios en entidades, DbContext, migraciones, configuración, código WCF/WindowsService/Tools, CRUD de clientes/productos, autenticación, solución y script integrado del Módulo 2.

`modern/README.md` conserva el SHA-256 `72E44BD8BA5806EDA6FAAA8794D37DF6B55F66512E2F57646F0C4F2F3F006A5E`; no hay cambios en modern/. El alcance sobre archivos existentes es de siete archivos: los dos csproj afectados, FacturacionOperaciones, FacturasController y las vistas Index/Details/Reportes de facturas. El resto del módulo son archivos nuevos en las carpetas específicas, sin movimientos masivos.

## Límites de la evidencia

No se realizó revisión visual/ejecución de JavaScript en navegador, instalación del servicio en SCM, despliegue de producción ni validación del PR del Módulo 4. No se construyó ningún proyecto .NET 10. Las pruebas de reglas no sustituyen pruebas de integración con EF6, permisos y los anfitriones existentes.

## Validación después de integrar Módulo 4

La evidencia anterior corresponde al refactor original. En la rama acumulativa se ampliaron los contratos IConsultaProductos/IRepositorioFacturas y sus dobles. El mismo `legacy/tools/ValidarModulo3.ps1` ahora compila también PruebasIntegracionModulo4.cs y referencia el ejecutable del servicio y Serilog. Añade pruebas de lecturas async con proveedor EF6 en memoria, cancelación previa, filtros síncronos preservados y logs/errores del resumen sin SQL. No se ejecutó ese arnés actualizado en macOS por falta de .NET Framework/MSBuild/PowerShell; tampoco se ejecutó SQL, MVC, WCF ni el servicio. Repetir en Windows la compilación, ambos validadores y las demostraciones según [Módulo 4](../../../docs/modulo-4/README.md).
