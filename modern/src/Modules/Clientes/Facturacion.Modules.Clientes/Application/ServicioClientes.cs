using Facturacion.BuildingBlocks.Resultados;
using Facturacion.Modules.Clientes.Domain;

namespace Facturacion.Modules.Clientes.Application;

/// <summary>
/// DTO de salida del módulo Clientes.
/// Desacopla la respuesta HTTP de la entidad de dominio y de la tabla SQL.
/// </summary>
public sealed record ClienteDto(
    int Id,
    string Nombre,
    string Apellido,
    string Documento,
    string Email,
    bool Activo);

public sealed record GuardarClienteComando(
    string? Nombre,
    string? Apellido,
    string? Documento,
    string? Email);

public interface IServicioClientes
{
    Task<IReadOnlyList<ClienteDto>> ListarAsync(CancellationToken cancellationToken = default);

    Task<Result<ClienteDto>> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Result<ClienteDto>> ObtenerPorDocumentoAsync(string documento, CancellationToken cancellationToken = default);

    Task<Result<ClienteDto>> CrearAsync(GuardarClienteComando comando, CancellationToken cancellationToken = default);

    Task<Result<ClienteDto>> ActualizarAsync(int id, GuardarClienteComando comando, CancellationToken cancellationToken = default);

    Task<Result> DesactivarAsync(int id, CancellationToken cancellationToken = default);
}

internal sealed class ServicioClientes(IRepositorioClientes repositorio) : IServicioClientes
{
    public async Task<IReadOnlyList<ClienteDto>> ListarAsync(CancellationToken cancellationToken = default)
    {
        var clientes = await repositorio.ListarAsync(cancellationToken);
        return clientes.Select(ADto).ToList();
    }

    public async Task<Result<ClienteDto>> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var cliente = await repositorio.ObtenerPorIdAsync(id, cancellationToken);
        return cliente is null
            ? ErroresCliente.NoEncontrado(id)
            : ADto(cliente);
    }

    public async Task<Result<ClienteDto>> ObtenerPorDocumentoAsync(string documento, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(documento))
            return ErroresCliente.DocumentoObligatorio;

        var cliente = await repositorio.ObtenerPorDocumentoAsync(documento.Trim(), cancellationToken);
        return cliente is null
            ? Error.NoEncontrado("Clientes.DocumentoNoEncontrado", $"No se encontró un cliente con documento '{documento.Trim()}'.")
            : ADto(cliente);
    }

    public async Task<Result<ClienteDto>> CrearAsync(GuardarClienteComando comando, CancellationToken cancellationToken = default)
    {
        var creacion = Cliente.Crear(comando.Nombre, comando.Apellido, comando.Documento, comando.Email);
        if (creacion.EsFallo)
            return creacion.Error;

        var cliente = creacion.Valor;
        if (await repositorio.ExisteDocumentoAsync(cliente.Documento, exceptoId: 0, cancellationToken))
            return ErroresCliente.DocumentoDuplicado(cliente.Documento);

        await repositorio.AgregarAsync(cliente, cancellationToken);
        await repositorio.GuardarCambiosAsync(cancellationToken);
        return ADto(cliente);
    }

    public async Task<Result<ClienteDto>> ActualizarAsync(int id, GuardarClienteComando comando, CancellationToken cancellationToken = default)
    {
        var actual = await repositorio.ObtenerPorIdAsync(id, cancellationToken);
        if (actual is null)
            return ErroresCliente.NoEncontrado(id);

        var actualizacion = actual.Actualizar(comando.Nombre, comando.Apellido, comando.Documento, comando.Email);
        if (actualizacion.EsFallo)
            return actualizacion.Error;

        if (await repositorio.ExisteDocumentoAsync(actual.Documento, exceptoId: actual.Id, cancellationToken))
            return ErroresCliente.DocumentoDuplicado(actual.Documento);

        await repositorio.GuardarCambiosAsync(cancellationToken);
        return ADto(actual);
    }

    public async Task<Result> DesactivarAsync(int id, CancellationToken cancellationToken = default)
    {
        var actual = await repositorio.ObtenerPorIdAsync(id, cancellationToken);
        if (actual is null)
            return ErroresCliente.NoEncontrado(id);

        actual.Desactivar();
        await repositorio.GuardarCambiosAsync(cancellationToken);
        return Result.Exito();
    }

    internal static ClienteDto ADto(Cliente cliente) =>
        new(cliente.Id, cliente.Nombre, cliente.Apellido, cliente.Documento, cliente.Email, cliente.Activo);
}

