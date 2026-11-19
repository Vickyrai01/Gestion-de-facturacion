namespace Facturacion.BuildingBlocks.Dominio;

/// <summary>
/// Base de las entidades: se identifican por Id, no por sus valores.
/// El setter es protegido: sólo la propia entidad (o EF Core por reflexión) lo asigna.
/// </summary>
public abstract class Entidad
{
    public int Id { get; protected set; }
}

