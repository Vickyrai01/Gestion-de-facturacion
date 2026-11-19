namespace Facturacion.Modules.Clientes.Domain;

/// <summary>
/// Puerto de persistencia del módulo Clientes.
///
/// A diferencia del `IRepository&lt;T&gt;` genérico de las Clases 5 y 6 (que exponía
/// entidades de base de datos), este repositorio pertenece a la capa de Dominio/Aplicación
/// del módulo, habla en términos de la entidad rica <see cref="Cliente"/> y sólo
/// ofrece las operaciones que el módulo realmente necesita.
/// </summary>
public interface IRepositorioClientes
{
    Task<IReadOnlyList<Cliente>> ListarAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Cliente>> ListarActivosAsync(CancellationToken cancellationToken = default);

    Task<Cliente?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Cliente?> ObtenerPorDocumentoAsync(string documento, CancellationToken cancellationToken = default);

    Task<bool> ExisteDocumentoAsync(string documento, int exceptoId = 0, CancellationToken cancellationToken = default);

    Task AgregarAsync(Cliente cliente, CancellationToken cancellationToken = default);

    Task GuardarCambiosAsync(CancellationToken cancellationToken = default);
}

