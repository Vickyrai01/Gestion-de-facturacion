namespace Facturacion.Modules.Clientes.Contracts;

/// <summary>
/// DTO liviano que otros módulos consumen para validar o mostrar datos de un cliente
/// sin acoplarse a la entidad de dominio ni a la tabla Clientes.
/// </summary>
public sealed record ClienteCatalogoDto(
    int Id,
    string Nombre,
    string Apellido,
    string Documento,
    string Email,
    bool Activo)
{
    public string NombreCompleto => $"{Nombre} {Apellido}";
}

/// <summary>
/// Puerto de comunicación entre módulos (In-Process Contract).
/// Reemplaza a las consultas directas `db.Clientes.Any(...)` que hacía
/// FacturacionOperaciones y FacturasController en el legacy.
/// </summary>
public interface IConsultaClientes
{
    Task<ClienteCatalogoDto?> ObtenerPorIdAsync(int clienteId, CancellationToken cancellationToken = default);

    Task<bool> EstaActivoAsync(int clienteId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ClienteCatalogoDto>> ListarActivosAsync(CancellationToken cancellationToken = default);
}

