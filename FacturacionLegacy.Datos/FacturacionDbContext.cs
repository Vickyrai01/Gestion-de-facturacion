using System.Data.Entity;
using System.Data.Entity.Infrastructure.Annotations;
using System.ComponentModel.DataAnnotations.Schema;
using FacturacionLegacy.Datos.Entidades;

namespace FacturacionLegacy.Datos
{
    public class FacturacionDbContext : DbContext
    {
        static FacturacionDbContext() { Database.SetInitializer<FacturacionDbContext>(null); }
        public FacturacionDbContext() : base(ObtenerConexion())
        {
            Configuration.LazyLoadingEnabled = false;
            Configuration.ProxyCreationEnabled = false;
        }
        private static string ObtenerConexion()
        {
            // Override opcional para pruebas aisladas; la ejecución habitual usa Web/App.config.
            return System.Environment.GetEnvironmentVariable("FACTURACION_DB_CONNECTION") ?? "name=FacturacionDb";
        }
        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<Producto> Productos { get; set; }
        public DbSet<Factura> Facturas { get; set; }
        public DbSet<DetalleFactura> DetallesFactura { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Cliente>().ToTable("Clientes");
            modelBuilder.Entity<Producto>().ToTable("Productos");
            modelBuilder.Entity<Factura>().ToTable("Facturas");
            modelBuilder.Entity<DetalleFactura>().ToTable("DetallesFactura");
            modelBuilder.Entity<Usuario>().ToTable("Usuarios");
            modelBuilder.Entity<Usuario>().Property(u => u.NombreUsuario).HasColumnAnnotation("Index", new IndexAnnotation(new IndexAttribute("UX_Usuario_Nombre") { IsUnique = true }));
            modelBuilder.Entity<Producto>().Property(p => p.Codigo).HasColumnAnnotation("Index", new IndexAnnotation(new IndexAttribute("UX_Producto_Codigo") { IsUnique = true }));
            modelBuilder.Entity<Factura>().Property(f => f.Numero).HasColumnAnnotation("Index", new IndexAnnotation(new IndexAttribute("UX_Factura_Numero") { IsUnique = true }));
            modelBuilder.Entity<Producto>().Property(p => p.PrecioUnitario).HasPrecision(18, 2);
            modelBuilder.Entity<Factura>().Property(f => f.Subtotal).HasPrecision(18, 2);
            modelBuilder.Entity<Factura>().Property(f => f.Impuestos).HasPrecision(18, 2);
            modelBuilder.Entity<Factura>().Property(f => f.Total).HasPrecision(18, 2);
            modelBuilder.Entity<DetalleFactura>().Property(d => d.PrecioUnitario).HasPrecision(18, 2);
            modelBuilder.Entity<DetalleFactura>().Property(d => d.Subtotal).HasPrecision(18, 2);
            modelBuilder.Entity<Factura>().HasRequired(f => f.Cliente).WithMany(c => c.Facturas).HasForeignKey(f => f.ClienteId).WillCascadeOnDelete(false);
            modelBuilder.Entity<DetalleFactura>().HasRequired(d => d.Factura).WithMany(f => f.Detalles).HasForeignKey(d => d.FacturaId).WillCascadeOnDelete(false);
            modelBuilder.Entity<DetalleFactura>().HasRequired(d => d.Producto).WithMany(p => p.Detalles).HasForeignKey(d => d.ProductoId).WillCascadeOnDelete(false);
            base.OnModelCreating(modelBuilder);
        }
    }
}
