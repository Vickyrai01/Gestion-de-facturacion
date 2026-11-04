using Facturacion.Modern.Domain;
namespace Facturacion.Modern.Application;
public sealed class CatalogoService(IClienteRepository clientes,IProductoRepository productos)
{
    public IReadOnlyCollection<Cliente> Clientes()=>clientes.Listar().OrderBy(x=>x.Id).ToArray();
    public IReadOnlyCollection<Producto> Productos()=>productos.Listar().OrderBy(x=>x.Codigo).ToArray();
    public Cliente CrearCliente(string n,string a,string d,string e){var x=new Cliente(clientes.Listar().Select(x=>x.Id).DefaultIfEmpty(0).Max()+1,n,a,d,e); clientes.Guardar(x); return x;}
    public Producto CrearProducto(string c,string n,string? d,decimal p,int s){if(productos.ExisteCodigo(c)) throw new DomainException("Ya existe un producto con ese código."); var x=new Producto(productos.Listar().Select(x=>x.Id).DefaultIfEmpty(0).Max()+1,c,n,d,p,s); productos.Guardar(x); return x;}
    public Cliente? ObtenerCliente(int id)=>clientes.Obtener(id); public Producto? ObtenerProducto(int id)=>productos.Obtener(id);
}
