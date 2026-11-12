using Facturacion.Infrastructure.Persistence.Models;
using Facturacion.Infrastructure.Persistence.Repository;

namespace Facturacion.Services.Clientes;

public sealed class ClienteService : IClienteService
{
    private readonly IUnitOfWork unitOfWork;

    public ClienteService(IUnitOfWork unitOfWork)
    {
        this.unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public Task<IReadOnlyList<Cliente>> GetAllAsync(CancellationToken cancellationToken = default)
        => unitOfWork.Clientes.GetAllAsync(cancellationToken);

    public async Task<Cliente> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => await unitOfWork.Clientes.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"No se encontró el cliente con id {id}.");

    public Task<Cliente?> GetByDocumentoAsync(
        string documento,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(documento);
        return unitOfWork.Clientes.GetByDocumentoAsync(documento.Trim(), cancellationToken);
    }

    public async Task<Cliente> CreateAsync(
        Cliente cliente,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cliente);
        Validate(cliente);

        if (await GetByDocumentoAsync(cliente.Documento, cancellationToken) is not null)
            throw new InvalidOperationException(
                $"Ya existe un cliente con el documento '{cliente.Documento}'.");

        await unitOfWork.Clientes.AddAsync(cliente, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return cliente;
    }

    public async Task<Cliente> UpdateAsync(
        Cliente cliente,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cliente);
        Validate(cliente);
        _ = await GetByIdAsync(cliente.Id, cancellationToken);

        var clienteConMismoDocumento = await GetByDocumentoAsync(cliente.Documento, cancellationToken);
        if (clienteConMismoDocumento is not null && clienteConMismoDocumento.Id != cliente.Id)
            throw new InvalidOperationException(
                $"Ya existe un cliente con el documento '{cliente.Documento}'.");

        unitOfWork.Clientes.Update(cliente);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return cliente;
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var cliente = await GetByIdAsync(id, cancellationToken);
        unitOfWork.Clientes.Delete(cliente);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static void Validate(Cliente cliente)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cliente.Nombre);
        ArgumentException.ThrowIfNullOrWhiteSpace(cliente.Apellido);
        ArgumentException.ThrowIfNullOrWhiteSpace(cliente.Documento);
        ArgumentException.ThrowIfNullOrWhiteSpace(cliente.Email);
    }
}
