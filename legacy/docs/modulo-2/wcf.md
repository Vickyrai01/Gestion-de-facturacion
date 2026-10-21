# Servicio WCF de consulta

El proyecto está en `legacy/FacturacionLegacy.Wcf/`, dentro de `legacy/SistemaFacturacion.Legacy.slnx`. Los comandos del README se ejecutan desde `legacy/`.

Windows Communication Foundation (WCF) ofrece contratos y endpoints para comunicar aplicaciones .NET Framework. SOAP transporta mensajes XML estructurados; no es una ruta MVC que devuelve HTML. Aquí se usa SOAP 1.1 mediante `basicHttpBinding` y HTTP solo para la demostración local.

[`Contratos.cs`](../../FacturacionLegacy.Wcf/Contratos.cs) define `IFacturaConsultaService`: `[ServiceContract]` identifica el contrato y `[OperationContract]` sus operaciones. `[DataContract]` y `[DataMember]` delimitan los datos serializados. FacturaConsulta, FacturaResumen y DetalleConsulta son DTOs separados de EF6; no se serializan DbContext, proxies ni grafos de navegación. `ConsultaError` es un fault tipado.

Un endpoint combina dirección, binding y contrato. La dirección identifica el servicio; el binding define transporte/formato; el contrato indica sus operaciones. [`Web.config`](../../FacturacionLegacy.Wcf/Web.config) configura esos elementos y habilita metadatos HTTP. [`FacturaConsultaService.svc`](../../FacturacionLegacy.Wcf/FacturaConsultaService.svc) permite que IIS Express active la implementación compilada. Ver [publicación de metadatos con configuración](https://learn.microsoft.com/en-us/dotnet/framework/wcf/feature-details/how-to-publish-metadata-for-a-service-using-a-configuration-file).

## Operaciones y prueba local

[`FacturaConsultaService.svc.cs`](../../FacturacionLegacy.Wcf/FacturaConsultaService.svc.cs) expone:

| Operación | Entrada | Resultado |
| --- | --- | --- |
| ObtenerFacturaPorId | ID positivo | Factura con importes y detalles; fault si no existe. |
| ObtenerFacturasPorCliente | ID positivo de cliente | Lista de resúmenes; vacía si no hay coincidencias. |
| ObtenerFacturasPorEstado | PENDIENTE, EMITIDA o ANULADA | Lista de resúmenes; fault para estado inválido. |

Cada consulta abre/libera su contexto y reutiliza FacturacionOperaciones. Los fallos de infraestructura se convierten en un mensaje genérico; no se publican excepciones internas SQL. `SoloLocalAuthorizationManager` acepta operaciones únicamente desde loopback. No utiliza las cookies Forms del MVC. Esta configuración no se propone como autenticación para una integración remota; para otro equipo habría que definir identidad, permisos y transporte protegido.

Compilar, preparar la base e iniciar IIS Express como indica el README. Abrir `http://localhost:60122/FacturaConsultaService.svc?wsdl` para ver WSDL: operaciones, mensajes y tipos. Visual Studio permite **Agregar referencia de servicio** con esa URL; un cliente generado puede invocar las tres operaciones. WCF Test Client también puede cargar el WSDL si está instalado.

Ejemplo mínimo SOAP; adaptar el ID a una factura existente:

```xml
<s:Envelope xmlns:s="http://schemas.xmlsoap.org/soap/envelope/">
  <s:Body>
    <ObtenerFacturaPorId xmlns="urn:facturacion-legacy"><id>1</id></ObtenerFacturaPorId>
  </s:Body>
</s:Envelope>
```

Enviar POST a `/FacturaConsultaService.svc`, `Content-Type: text/xml; charset=utf-8` y `SOAPAction: "urn:facturacion-legacy/IFacturaConsultaService/ObtenerFacturaPorId"`. `tools/ValidarModulo2.ps1` contiene un ejemplo ejecutable de las tres operaciones y un fault.

## Migración conceptual

REST organiza recursos y verbos HTTP; puede utilizar JSON y códigos de estado. gRPC utiliza contratos protobuf y llamadas RPC, con otro transporte y herramientas de cliente. No son cambios de nombre para el mismo SOAP/WSDL. Durante el curso habrá que diseñar contratos equivalentes, serialización decimal/fechas, errores, seguridad y clientes, conservando las consultas y sus resultados. Comparar explícitamente el número, estado, importes y precios históricos. No se implementó REST ni gRPC aquí.
