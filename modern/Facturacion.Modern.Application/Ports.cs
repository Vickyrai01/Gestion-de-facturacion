using Facturacion.Modern.Domain;
namespace Facturacion.Modern.Application;

public interface IClienteRepository { IReadOnlyCollection<Cliente> Listar(); Cliente? Obtener(int id); void Guardar(Cliente cliente); }
public interface IProductoRepository { IReadOnlyCollection<Producto> Listar(); Producto? Obtener(int id); bool ExisteCodigo(string codigo,int exceptoId=0); void Guardar(Producto producto); }
public interface IFacturaRepository { IReadOnlyCollection<Factura> Listar(int? clienteId=null,EstadoFactura? estado=null); Factura? Obtener(int id); void Guardar(Factura factura); }
public interface IUsuarioRepository { Usuario? Obtener(string nombre); }
public interface IPasswordService { string Hash(string password); bool Verify(string password,string hash); }
public interface IClock { DateTime UtcNow { get; } }
