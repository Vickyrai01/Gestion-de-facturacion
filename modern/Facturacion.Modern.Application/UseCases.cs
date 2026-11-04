using Facturacion.Modern.Domain;
namespace Facturacion.Modern.Application;

public sealed record CrearFacturaCommand(int ClienteId,IReadOnlyCollection<LineaFactura> Lineas,decimal TasaImpuestos);
public sealed record FacturaResumen(int Id,string Numero,int ClienteId,DateTime FechaEmision,EstadoFactura Estado,decimal Subtotal,decimal Impuestos,decimal Total);
public sealed record DetalleResultado(int ProductoId,string Codigo,int Cantidad,decimal PrecioUnitario,decimal Subtotal);
public sealed record FacturaResultado(FacturaResumen Resumen,IReadOnlyCollection<DetalleResultado> Detalles);

public sealed class FacturacionService(IClienteRepository clientes,IProductoRepository productos,IFacturaRepository facturas)
{
    public FacturaResultado Crear(CrearFacturaCommand command)
    {
        if(command.Lineas is null || command.Lineas.Count is < 1 or > 100) throw new DomainException("La factura debe contener entre 1 y 100 detalles.");
        if(command.Lineas.Any(x=>x is null || x.ProductoId<=0 || x.Cantidad<=0 || x.Cantidad>1_000_000)) throw new DomainException("Seleccioná productos y cantidades positivas, hasta 1000000 por detalle.");
        if(command.TasaImpuestos is < 0 or > 1) throw new DomainException("La tasa de impuestos debe estar entre 0 y 1.");
        if(clientes.Obtener(command.ClienteId) is not { Activo:true }) throw new DomainException("El cliente no existe o está inactivo.");
        var ids=command.Lineas.Select(x=>x.ProductoId).Distinct().ToArray(); var found=ids.Select(productos.Obtener).ToArray();
        if(found.Any(x=>x is not { Activo:true })) throw new DomainException("Uno de los productos no existe o está inactivo.");
        var byId=found.OfType<Producto>().ToDictionary(x=>x.Id); var detalles=command.Lineas.Select(x=>new DetalleFactura(x.ProductoId,x.Cantidad,byId[x.ProductoId].PrecioUnitario)).ToArray();
        var factura=new Factura(facturas.Listar().Select(x=>x.Id).DefaultIfEmpty(0).Max()+1,$"FAC-{facturas.Listar().Select(x=>x.Id).DefaultIfEmpty(0).Max()+1:D10}",command.ClienteId,detalles,command.TasaImpuestos); facturas.Guardar(factura); return Map(factura,byId);
    }
    public IReadOnlyCollection<FacturaResumen> Consultar(int? clienteId,EstadoFactura? estado)=>facturas.Listar(clienteId,estado).OrderByDescending(x=>x.FechaEmision).ThenByDescending(x=>x.Id).Select(x=>Map(x)).ToArray();
    public FacturaResultado? Obtener(int id) { var f=facturas.Obtener(id); if(f is null) return null; var p=f.Detalles.ToDictionary(x=>x.ProductoId,x=>productos.Obtener(x.ProductoId)?.Codigo??""); return new(Map(f),f.Detalles.Select(x=>new DetalleResultado(x.ProductoId,p[x.ProductoId],x.Cantidad,x.PrecioUnitario,x.Subtotal)).ToArray()); }
    public bool Emitir(int id)=>facturas.Obtener(id)?.Emitir() is true;
    public bool Anular(int id)=>facturas.Obtener(id)?.Anular() is true;
    public int ContarPendientes()=>facturas.Listar(null,EstadoFactura.Pendiente).Count;
    private static FacturaResumen Map(Factura x)=>new(x.Id,x.Numero,x.ClienteId,x.FechaEmision,x.Estado,x.Subtotal,x.Impuestos,x.Total);
    private static FacturaResultado Map(Factura x,Dictionary<int,Producto> p)=>new(Map(x),x.Detalles.Select(d=>new DetalleResultado(d.ProductoId,p[d.ProductoId].Codigo,d.Cantidad,d.PrecioUnitario,d.Subtotal)).ToArray());
}

public sealed class AutenticacionService(IUsuarioRepository usuarios,IPasswordService passwords)
{
    public (Usuario Usuario,string Token)? Login(string nombre,string password) { var u=usuarios.Obtener(nombre.Trim().ToLowerInvariant()); return u is { Activo:true } && passwords.Verify(password,u.PasswordHash) ? (u,"memoria-token") : null; }
}
