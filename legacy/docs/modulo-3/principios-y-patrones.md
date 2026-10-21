# Clean Architecture, DDD y patrones aplicados

## Principios aplicados, con límites explícitos

El servicio de aplicación coordina un caso de uso y depende de contratos. Infraestructura implementa esos contratos con EF6. El controller depende de `IServicioFacturacion` y se ocupa de HTTP. El modelo `FacturaEnPreparacion` protege el cálculo y los detalles, sin usar EF o System.Web.

Esto aplica separación de responsabilidades y dependencia hacia abstracciones, pero no convierte el proyecto entero en Clean Architecture. Datos conserva su nombre y todas esas clases en el mismo ensamblado; los CRUD restantes, seguridad y la fachada compatible mantienen acoplamientos. No hay una barrera entre proyectos que impida una referencia incorrecta. En el proyecto futuro se deberá decidir si las necesidades justifican esa barrera, sin confundirla con una obligación de crear muchas bibliotecas.

## Entidades, creación e invariantes

Las entidades EF existentes conservan su identidad y mapeo: Cliente, Producto, Factura, DetalleFactura y Usuario. No se les agregan columnas ni se reemplazan por DTOs dentro de la base.

`Facturacion/Dominio/FacturaEnPreparacion.cs` introduce un modelo de creación con comportamiento. Controla número de líneas, cantidades, precios, redondeo, importes y una colección de resultados que no se puede modificar después del cálculo. El servicio comprueba las invariantes que requieren datos actuales: cliente y productos activos. La validación de la forma HTTP sigue ocurriendo en MVC; no sustituye la protección del caso de uso cuando este se llama desde otro anfitrión.

Factura y sus detalles forman una unidad de consistencia: se guardan juntos y no se confirma una cabecera sin sus líneas. Ese límite permite introducir el concepto de agregado. No se presenta `FacturaEnPreparacion` como un agregado DDD completo con todo el ciclo de vida de una factura persistida: no administra identidad final, eventos ni transiciones de un modelo rico. Las transiciones existentes siguen protegidas mediante UPDATE condicional en SQL. La [guía de validaciones de dominio de Microsoft](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/domain-model-layer-validations) distingue invariantes del dominio y validación de modelos de entrada.

Se conservan excepciones ArgumentException y sus mensajes para las violaciones esperadas; no se agrega un sistema genérico de resultados, specifications, eventos o mediadores que el ejemplo no necesita. La tasa sigue siendo un decimal configurable y los importes siguen siendo didácticos.

## Patrones seleccionados

| Patrón | Problema real | Aplicación concreta |
| --- | --- | --- |
| Repository | La creación y las consultas estaban ligadas a DbContext y SQL; no se podía sustituir esa dependencia para probar el caso de uso. | IRepositorioFacturas y RepositorioFacturasEf6 acotan esas operaciones. Las consultas de catálogo usan interfaces pequeñas de sus módulos. |
| Facade | WCF y Windows Service ya utilizan métodos públicos de FacturacionOperaciones; cambiarlos ampliaría el refactor y los conflictos con el Módulo 4. | FacturacionOperaciones conserva firmas y delega en los componentes nuevos donde corresponde. El nuevo controller no utiliza esa fachada. |

La composición manual por constructor es el mecanismo de ensamblado de dependencias, no un contenedor nuevo. El beneficio se observa en `tools/pruebas-modulo-3/PruebasArquitectura.cs`: el servicio y el controller se ejecutan con dependencias de prueba, sin crear una base SQL.

## Propuestas que se dejan para el aprendizaje

Strategy tendría sentido si aparecen políticas de impuestos realmente distintas con un requisito definido. Factory podría justificarse si existen tipos de comprobante con construcción diferente. Hoy solo hay tasa fija y un tipo de factura: introducir esas variantes crearía negocio y complejidad que el módulo no pidió. Por eso se documentan como propuestas y no se implementan.

Tampoco se agregan microservicios, CQRS formal, MediatR, event sourcing, buses, repositorios genéricos ni nuevas tablas. El monolito modular se trabaja como evolución de límites funcionales, no como una colección de patrones a instalar.

## Traslado conceptual a .NET 10

En `modern/`, durante el curso, se podrán reproducir primero los contratos y las pruebas de reglas. Después habrá que adaptar MVC, las consultas de EF, la transacción, secuencia y composición y verificar precios/estados/permisos. No copiar referencias a System.Web ni asumir que DbContext de EF6 y EF Core son intercambiables.

El caso de uso no obtiene IConfiguration ni HttpContext: recibe sus dependencias y una tasa mediante composición. Esa decisión facilita cambiar el anfitrión. Sin embargo, conservar un contrato C# no demuestra por sí solo compatibilidad de runtime, comportamiento SQL o cookies. La versión moderna se desarrollará en un proyecto independiente.
