using Facturacion.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Facturacion.Infrastructure.Persistence.Repository;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly FacturacionDbContext context;
    private bool disposed;
    private IUsuarioRepository? usuarios;
    private IClienteRepository? clientes;
    private IProductoRepository? productos;
    private IFacturaRepository? facturas;
    private IDetallesfacturaRepository? detallesfacturas;

    public UnitOfWork(FacturacionDbContext context)
    {
        this.context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public IUsuarioRepository Usuarios
    {
        get
        {
            ThrowIfDisposed();
            return usuarios ??= new UsuarioRepository(context);
        }
    }

    public IClienteRepository Clientes
    {
        get
        {
            ThrowIfDisposed();
            return clientes ??= new ClienteRepository(context);
        }
    }

    public IProductoRepository Productos
    {
        get
        {
            ThrowIfDisposed();
            return productos ??= new ProductoRepository(context);
        }
    }

    public IFacturaRepository Facturas
    {
        get
        {
            ThrowIfDisposed();
            return facturas ??= new FacturaRepository(context);
        }
    }

    public IDetallesfacturaRepository Detallesfacturas
    {
        get
        {
            ThrowIfDisposed();
            return detallesfacturas ??= new DetallesfacturaRepository(context);
        }
    }

    public int SaveChanges()
    {
        ThrowIfDisposed();
        return context.SaveChanges();
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return context.SaveChangesAsync(cancellationToken);
    }

    public async Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(operation);

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var result = await operation(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public void Dispose()
    {
        if (disposed)
            return;

        context.Dispose();
        disposed = true;
        GC.SuppressFinalize(this);
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
    }
}
