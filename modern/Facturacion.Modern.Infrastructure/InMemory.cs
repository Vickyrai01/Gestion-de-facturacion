using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Facturacion.Modern.Application;
using Facturacion.Modern.Domain;
namespace Facturacion.Modern.Infrastructure;

public sealed class InMemoryClienteRepository : IClienteRepository { private readonly ConcurrentDictionary<int,Cliente> data=new(); public IReadOnlyCollection<Cliente> Listar()=>data.Values.ToArray(); public Cliente? Obtener(int id)=>data.GetValueOrDefault(id); public void Guardar(Cliente x)=>data[x.Id]=x; }
public sealed class InMemoryProductoRepository : IProductoRepository { private readonly ConcurrentDictionary<int,Producto> data=new(); public IReadOnlyCollection<Producto> Listar()=>data.Values.ToArray(); public Producto? Obtener(int id)=>data.GetValueOrDefault(id); public bool ExisteCodigo(string c,int exceptoId=0)=>data.Values.Any(x=>x.Codigo.Equals(c.Trim(),StringComparison.OrdinalIgnoreCase)&&x.Id!=exceptoId); public void Guardar(Producto x)=>data[x.Id]=x; }
public sealed class InMemoryFacturaRepository : IFacturaRepository { private readonly ConcurrentDictionary<int,Factura> data=new(); public IReadOnlyCollection<Factura> Listar(int? c=null,EstadoFactura? e=null)=>data.Values.Where(x=>(c is null||x.ClienteId==c)&&(e is null||x.Estado==e)).ToArray(); public Factura? Obtener(int id)=>data.GetValueOrDefault(id); public void Guardar(Factura x)=>data[x.Id]=x; }
public sealed class InMemoryUsuarioRepository : IUsuarioRepository { private readonly List<Usuario> data; public InMemoryUsuarioRepository(IPasswordService passwords) { data=[new Usuario(1,"admin",passwords.Hash("FACTURACION_ADMIN_PASSWORD"),RolUsuario.Admin),new Usuario(2,"operador",passwords.Hash("FACTURACION_OPERADOR_PASSWORD"),RolUsuario.Operador)]; } public Usuario? Obtener(string nombre)=>data.SingleOrDefault(x=>x.NombreUsuario==nombre); }
public sealed class PasswordService : IPasswordService
{
    public string Hash(string password) { Validate(password); var salt=RandomNumberGenerator.GetBytes(16); var key=Rfc2898DeriveBytes.Pbkdf2(password,salt,100_000,HashAlgorithmName.SHA256,32); return $"pbkdf2$100000${Convert.ToBase64String(salt)}${Convert.ToBase64String(key)}"; }
    public bool Verify(string password,string hash) { try { var p=hash.Split('$'); if(p.Length!=4||p[0]!="pbkdf2") return false; var expected=Convert.FromBase64String(p[3]); var actual=Rfc2898DeriveBytes.Pbkdf2(password,Convert.FromBase64String(p[2]),int.Parse(p[1]),HashAlgorithmName.SHA256,expected.Length); return CryptographicOperations.FixedTimeEquals(actual,expected); } catch { return false; } }
    private static void Validate(string password) { if(string.IsNullOrEmpty(password)||password.Length<12||Encoding.UTF8.GetByteCount(password)>72) throw new ArgumentException("La contraseña debe tener al menos 12 caracteres y hasta 72 bytes UTF-8."); }
}
