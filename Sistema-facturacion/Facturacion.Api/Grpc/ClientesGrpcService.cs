using Facturacion.Services.Clientes;
using Grpc.Core;

namespace Facturacion.Api.Grpc;

public sealed class ClientesGrpcService : ClientesGrpc.ClientesGrpcBase
{
    private readonly IClienteService clienteService;

    public ClientesGrpcService(IClienteService clienteService)
    {
        this.clienteService = clienteService;
    }

    public override async Task<ClienteReply> GetCliente(
        GetClienteRequest request,
        ServerCallContext context)
    {
        if (request.Id <= 0)
        {
            throw new RpcException(
                new Status(StatusCode.InvalidArgument, "El id debe ser mayor que cero."));
        }

        try
        {
            var cliente = await clienteService.GetByIdAsync(request.Id, context.CancellationToken);

            return new ClienteReply
            {
                Id = cliente.Id,
                Nombre = cliente.Nombre,
                Apellido = cliente.Apellido,
                Documento = cliente.Documento,
                Email = cliente.Email,
                Activo = cliente.Activo
            };
        }
        catch (KeyNotFoundException exception)
        {
            throw new RpcException(new Status(StatusCode.NotFound, exception.Message));
        }
    }
}
