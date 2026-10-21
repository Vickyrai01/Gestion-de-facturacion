# Validación del Módulo 3

Las pruebas permiten distinguir las reglas de dominio, la coordinación del caso de uso y la integración con SQL y los anfitriones. Ejecutarlas en Windows con Visual Studio/MSBuild, .NET Framework 4.8, IIS Express y SQL Server LocalDB.

## Comandos

Desde la raíz del repositorio:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\legacy\tools\ValidarModulo3.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\legacy\tools\ValidarModulo2.ps1 -OmitirCompilacion
```

El primer comando compila el legacy con vistas Razor y ejecuta las pruebas de arquitectura en un directorio temporal propio. El segundo requiere LocalDB/IIS Express, certificado de desarrollo y puertos 44374/60122 disponibles. Utiliza una base temporal independiente y cuentas aleatorias. Omitir la compilación solo si los binarios corresponden al código actual.

## Escenarios sin SQL Server

`tools/pruebas-modulo-3/PruebasArquitectura.cs` ejercita:

- Cálculos decimales, redondeo, fecha UTC, cantidades y límites de líneas, precios y tasas.
- Independencia de las colecciones de entrada y detalles de solo lectura.
- Rechazo de clientes/productos inactivos o inexistentes mediante dependencias de prueba.
- Orden de validación y guardado dentro de la transacción; confirmación únicamente después del guardado y disposición ante error.
- Controller construido mediante interfaz: DTOs, redirección al crear, ModelState inválido y respuestas HTTP 400/404/409.
- Lectura de la tasa únicamente cuando el caso de uso la necesita.

`tools/pruebas-modulo-3/PruebasIntegracionModulo4.cs` utiliza un proveedor EF6 y un logger en memoria para comprobar consultas async, orden/límite, productos inactivos, detalle inexistente, identificador inválido, cancelación previa, filtros síncronos y eventos estructurados del resumen.

## Escenarios con SQL y anfitriones

El validador del Módulo 2 comprueba CRUD de clientes/productos, validaciones, login/logout y roles, creación y totales de facturas, precios históricos, persistencia, transiciones, filtros y numeración concurrente. También cubre WSDL, consultas SOAP, conteo periódico y detención normal del servicio.

Compilar también Release con `MvcBuildViews=true` y completar las demostraciones de [async, logging y background jobs](../../../docs/modulo-4/README.md). Revisar MVC en navegador y, cuando corresponda, instalar el servicio con una identidad que tenga los permisos necesarios.

## Qué no demuestran los dobles

Una transacción simulada demuestra coordinación, pero no rollback real de SQL Server. Un proveedor en memoria no verifica traducción LINQ a SQL ni cancelación de consultas en vuelo. Las pruebas de reglas tampoco sustituyen la comprobación de permisos, cookies, JavaScript, configuración de WCF o ciclo de vida del servicio instalado.
