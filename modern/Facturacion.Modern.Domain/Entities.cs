namespace Facturacion.Modern.Domain;

public sealed class DomainException(string message) : Exception(message);
public enum RolUsuario { Admin, Operador }
public enum EstadoFactura { Pendiente, Emitida, Anulada }

public sealed class Cliente
{
    private Cliente() { }
    public Cliente(int id, string nombre, string apellido, string documento, string email) { Id=id; Actualizar(nombre, apellido, documento, email); Activo=true; }
    public int Id { get; private set; }
    public string Nombre { get; private set; } = "";
    public string Apellido { get; private set; } = "";
    public string Documento { get; private set; } = "";
    public string Email { get; private set; } = "";
    public bool Activo { get; private set; }
    public void Actualizar(string nombre, string apellido, string documento, string email) { Nombre=Required(nombre,"El nombre es obligatorio",100); Apellido=Required(apellido,"El apellido es obligatorio",100); Documento=Required(documento,"El documento es obligatorio",30); Email=Required(email,"El email es obligatorio",254); if (!email.Contains('@')) throw new DomainException("Ingresá un email válido."); }
    public void Desactivar() => Activo=false;
    private static string Required(string value,string error,int max) { if(string.IsNullOrWhiteSpace(value)) throw new DomainException(error); value=value.Trim(); if(value.Length>max) throw new DomainException("El valor supera la longitud permitida."); return value; }
}

public sealed class Producto
{
    private Producto() { }
    public Producto(int id,string codigo,string nombre,string? descripcion,decimal precio,int stock) { Id=id; Actualizar(codigo,nombre,descripcion,precio,stock); Activo=true; }
    public int Id { get; private set; }
    public string Codigo { get; private set; } = "";
    public string Nombre { get; private set; } = "";
    public string? Descripcion { get; private set; }
    public decimal PrecioUnitario { get; private set; }
    public int Stock { get; private set; }
    public bool Activo { get; private set; }
    public void Actualizar(string codigo,string nombre,string? descripcion,decimal precio,int stock) { Codigo=Required(codigo,"El código es obligatorio",30).ToUpperInvariant(); Nombre=Required(nombre,"El nombre es obligatorio",100); Descripcion=descripcion?.Trim(); if(Descripcion?.Length>500) throw new DomainException("La descripción no puede superar los 500 caracteres."); if(precio<0) throw new DomainException("El precio debe ser no negativo."); if(stock<0) throw new DomainException("El stock no puede ser negativo."); PrecioUnitario=decimal.Round(precio,2,MidpointRounding.AwayFromZero); Stock=stock; }
    public void Desactivar()=>Activo=false;
    private static string Required(string value,string error,int max) { if(string.IsNullOrWhiteSpace(value)) throw new DomainException(error); value=value.Trim(); if(value.Length>max) throw new DomainException("El valor supera la longitud permitida."); return value; }
}

public sealed record LineaFactura(int ProductoId,int Cantidad);
public sealed class DetalleFactura
{
    public DetalleFactura(int productoId,int cantidad,decimal precio) { ProductoId=productoId; Cantidad=cantidad; PrecioUnitario=precio; Subtotal=decimal.Round(precio*cantidad,2,MidpointRounding.AwayFromZero); }
    public int ProductoId { get; }
    public int Cantidad { get; }
    public decimal PrecioUnitario { get; }
    public decimal Subtotal { get; }
}
public sealed class Factura
{
    private Factura() { }
    public Factura(int id,string numero,int clienteId,IReadOnlyCollection<DetalleFactura> detalles,decimal tasa) { Id=id; Numero=numero; ClienteId=clienteId; FechaEmision=DateTime.UtcNow; Estado=EstadoFactura.Pendiente; Detalles=detalles; Subtotal=detalles.Sum(x=>x.Subtotal); Impuestos=decimal.Round(Subtotal*tasa,2,MidpointRounding.AwayFromZero); Total=Subtotal+Impuestos; if(Total>9999999999999999.99m) throw new DomainException("El importe excede la capacidad permitida."); }
    public int Id { get; }
    public string Numero { get; } = "";
    public int ClienteId { get; }
    public DateTime FechaEmision { get; }
    public EstadoFactura Estado { get; private set; }
    public decimal Subtotal { get; }
    public decimal Impuestos { get; }
    public decimal Total { get; }
    public IReadOnlyCollection<DetalleFactura> Detalles { get; } = Array.Empty<DetalleFactura>();
    public bool Emitir() { if(Estado!=EstadoFactura.Pendiente) return false; Estado=EstadoFactura.Emitida; return true; }
    public bool Anular() { if(Estado==EstadoFactura.Anulada) return false; Estado=EstadoFactura.Anulada; return true; }
}

public sealed class Usuario
{
    private Usuario() { }
    public Usuario(int id,string nombre,string hash,RolUsuario rol) { Id=id; NombreUsuario=nombre.Trim().ToLowerInvariant(); PasswordHash=hash; Rol=rol; Activo=true; }
    public int Id { get; }
    public string NombreUsuario { get; } = "";
    public string PasswordHash { get; } = "";
    public RolUsuario Rol { get; }
    public bool Activo { get; private set; }
}
