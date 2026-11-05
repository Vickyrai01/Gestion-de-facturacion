using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Facturacion.Infrastructure.Persistence.Models;

public partial class FacturacionDbContext : DbContext
{
    public FacturacionDbContext()
    {
    }

    public FacturacionDbContext(DbContextOptions<FacturacionDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Cliente> Clientes { get; set; }

    public virtual DbSet<Detallesfactura> Detallesfacturas { get; set; }

    public virtual DbSet<Factura> Facturas { get; set; }

    public virtual DbSet<Producto> Productos { get; set; }

    public virtual DbSet<Usuario> Usuarios { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseOracle("User Id=facturacion_app;Password=TuPasswordSeguro123;Data Source=localhost:1521/FREEPDB1");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasDefaultSchema("FACTURACION_APP")
            .UseCollation("USING_NLS_COMP");

        modelBuilder.Entity<Cliente>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("SYS_C008665");

            entity.ToTable("CLIENTES");

            entity.Property(e => e.Id)
                .HasPrecision(10)
                .HasColumnName("ID");
            entity.Property(e => e.Activo)
                .HasColumnType("NUMBER(1)")
                .HasColumnName("ACTIVO");
            entity.Property(e => e.Apellido)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("APELLIDO");
            entity.Property(e => e.Documento)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("DOCUMENTO");
            entity.Property(e => e.Email)
                .HasMaxLength(254)
                .IsUnicode(false)
                .HasColumnName("EMAIL");
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("NOMBRE");
        });

        modelBuilder.Entity<Detallesfactura>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("SYS_C008691");

            entity.ToTable("DETALLESFACTURA");

            entity.Property(e => e.Id)
                .HasPrecision(10)
                .HasColumnName("ID");
            entity.Property(e => e.Cantidad)
                .HasPrecision(10)
                .HasColumnName("CANTIDAD");
            entity.Property(e => e.Facturaid)
                .HasPrecision(10)
                .HasColumnName("FACTURAID");
            entity.Property(e => e.Preciounitario)
                .HasColumnType("NUMBER(18,2)")
                .HasColumnName("PRECIOUNITARIO");
            entity.Property(e => e.Productoid)
                .HasPrecision(10)
                .HasColumnName("PRODUCTOID");
            entity.Property(e => e.Subtotal)
                .HasColumnType("NUMBER(18,2)")
                .HasColumnName("SUBTOTAL");

            entity.HasOne(d => d.Factura).WithMany(p => p.Detallesfacturas)
                .HasForeignKey(d => d.Facturaid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DETALLES_FACTURAS");

            entity.HasOne(d => d.Producto).WithMany(p => p.Detallesfacturas)
                .HasForeignKey(d => d.Productoid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DETALLES_PRODUCTOS");
        });

        modelBuilder.Entity<Factura>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("SYS_C008682");

            entity.ToTable("FACTURAS");

            entity.HasIndex(e => e.Numero, "UX_FACTURA_NUMERO").IsUnique();

            entity.Property(e => e.Id)
                .HasPrecision(10)
                .HasColumnName("ID");
            entity.Property(e => e.Clienteid)
                .HasPrecision(10)
                .HasColumnName("CLIENTEID");
            entity.Property(e => e.Estado)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("ESTADO");
            entity.Property(e => e.Fechaemision)
                .HasPrecision(6)
                .HasColumnName("FECHAEMISION");
            entity.Property(e => e.Impuestos)
                .HasColumnType("NUMBER(18,2)")
                .HasColumnName("IMPUESTOS");
            entity.Property(e => e.Numero)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("NUMERO");
            entity.Property(e => e.Subtotal)
                .HasColumnType("NUMBER(18,2)")
                .HasColumnName("SUBTOTAL");
            entity.Property(e => e.Total)
                .HasColumnType("NUMBER(18,2)")
                .HasColumnName("TOTAL");

            entity.HasOne(d => d.Cliente).WithMany(p => p.Facturas)
                .HasForeignKey(d => d.Clienteid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_FACTURAS_CLIENTES");
        });

        modelBuilder.Entity<Producto>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("SYS_C008672");

            entity.ToTable("PRODUCTOS");

            entity.HasIndex(e => e.Codigo, "UX_PRODUCTO_CODIGO").IsUnique();

            entity.Property(e => e.Id)
                .HasPrecision(10)
                .HasColumnName("ID");
            entity.Property(e => e.Activo)
                .HasColumnType("NUMBER(1)")
                .HasColumnName("ACTIVO");
            entity.Property(e => e.Codigo)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("CODIGO");
            entity.Property(e => e.Descripcion)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("DESCRIPCION");
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("NOMBRE");
            entity.Property(e => e.Preciounitario)
                .HasColumnType("NUMBER(18,2)")
                .HasColumnName("PRECIOUNITARIO");
            entity.Property(e => e.Stock)
                .HasPrecision(10)
                .HasColumnName("STOCK");
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("SYS_C008657");

            entity.ToTable("USUARIOS");

            entity.HasIndex(e => e.Nombreusuario, "UX_USUARIO_NOMBRE").IsUnique();

            entity.Property(e => e.Id)
                .HasPrecision(10)
                .HasColumnName("ID");
            entity.Property(e => e.Activo)
                .HasColumnType("NUMBER(1)")
                .HasColumnName("ACTIVO");
            entity.Property(e => e.Nombreusuario)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("NOMBREUSUARIO");
            entity.Property(e => e.Passwordhash)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("PASSWORDHASH");
            entity.Property(e => e.Rol)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("ROL");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
