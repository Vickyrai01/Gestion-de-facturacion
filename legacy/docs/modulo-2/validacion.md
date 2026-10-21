# Validación del Módulo 2

Requisitos: Windows, Visual Studio/MSBuild, Developer Pack .NET Framework 4.8, IIS Express y SQL Server LocalDB.

## Escenarios de prueba

Compilar Debug y Release incluyendo las vistas Razor con `MvcBuildViews=true`. Preparar las migraciones Inicial y Usuarios y restaurar dependencias con `legacy/tools/RestaurarPaquetes.ps1`.

`legacy/tools/ValidarModulo2.ps1` utiliza una base temporal independiente, cuentas con contraseñas aleatorias e instancias propias de IIS Express para comprobar:

- Esquema desde migraciones, cinco clientes/ocho productos/tres facturas iniciales e inicialización idempotente.
- Hashes bcrypt con factor 12, login válido/inválido, cookies Secure/HttpOnly/SameSite=Lax, protección de rutas y logout POST.
- CRUD de clientes, validaciones y desactivación PATCH conservada del Módulo 1.
- CRUD de productos, normalización/duplicados, importes y stock no negativos.
- Facturas con varios detalles; cálculo 551.00 + 115.71 = 666.71; totales y precios enviados ignorados.
- Rechazo de cliente/producto inactivo, producto inexistente, cantidades no positivas y ausencia de detalles; sin guardar una factura parcial.
- Precios históricos independientes del precio actual, stock informativo y filtros por cliente/estado.
- Permisos directos de ADMIN/OPERADOR, emisión, anulación y conservación de detalles; inexistencia de edición de emitidas.
- WSDL y las tres operaciones SOAP, con fault controlado para estado inválido.
- Procesamiento manual de pendientes, logs de fecha/cantidad y ausencia de cambios de estado.
- Persistencia de clientes y facturas después de reiniciar la instancia MVC de prueba.
- Cuatro altas concurrentes de facturas con numeración única y dos altas simultáneas de un mismo código de producto con un único registro guardado.
- Timer con intervalo de cinco segundos, dos ciclos registrados y cierre normal del modo consola acotado.

La prueba valida el certificado IIS Express por su huella solo en el cliente local de pruebas. Usa HTTPS 44374, distinto del puerto 44372 habitual del MVC, y HTTP 60122 para WCF. Cierra los procesos que inició y elimina únicamente la base con el nombre aleatorio que creó.

## Ejecutar las pruebas

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\legacy\tools\ValidarModulo2.ps1
```

Este comando parte de la raíz del repositorio; el script establece internamente `legacy/` como directorio de trabajo y compila `SistemaFacturacion.Legacy.slnx`.

El script requiere permisos para LocalDB e iniciar IIS Express, certificado de desarrollo y puertos libres. La compilación puede omitirse con `-OmitirCompilacion` solo después de haber compilado la versión actual.

## Comprobaciones complementarias

Revisar MVC y JavaScript en navegador, incluido el agregado y la eliminación de filas de detalle con jQuery. La suite HTTP/SQL no sustituye esa comprobación visual.

Para ampliar la práctica, probar el servicio instalado en SCM bajo otra identidad, SQL Server Express, un cliente WCF generado y carga concurrente sostenida. Estos escenarios requieren configuración y pruebas específicas.

Los impuestos son didácticos, el stock no se descuenta y las facturas no tienen edición. Usar esos límites para distinguir el alcance del ejemplo de los requisitos de un sistema de producción.
