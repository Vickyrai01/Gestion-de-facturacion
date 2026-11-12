namespace Facturacion.Infrastructure.Persistence.Repository;

public interface IUnitOfWork : IDisposable
{
    IClienteRepository Clientes { get; }
    IProductoRepository Productos { get; }
    IUsuarioRepository Usuarios { get; }
    IFacturaRepository Facturas { get; }
    IDetallesfacturaRepository Detallesfacturas { get; }


    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default);
}