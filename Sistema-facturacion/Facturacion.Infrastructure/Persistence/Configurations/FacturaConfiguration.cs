using Facturacion.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Facturacion.Infrastructure.Persistence.Configurations;

public sealed class FacturaConfiguration : IEntityTypeConfiguration<Factura>
{
    public void Configure(EntityTypeBuilder<Factura> entity)
    {
        entity.HasKey(factura => factura.Id)
            .HasName("SYS_C008682");

        entity.ToTable("FACTURAS");

        entity.HasIndex(factura => factura.Numero, "UX_FACTURA_NUMERO")
            .IsUnique();

        entity.Property(factura => factura.Id)
            .HasPrecision(10)
            .HasColumnName("ID");
        entity.Property(factura => factura.Clienteid)
            .HasPrecision(10)
            .HasColumnName("CLIENTEID");
        entity.Property(factura => factura.Estado)
            .HasMaxLength(10)
            .IsUnicode(false)
            .HasColumnName("ESTADO");
        entity.Property(factura => factura.Fechaemision)
            .HasPrecision(6)
            .HasColumnName("FECHAEMISION");
        entity.Property(factura => factura.Impuestos)
            .HasColumnType("NUMBER(18,2)")
            .HasColumnName("IMPUESTOS");
        entity.Property(factura => factura.Numero)
            .HasMaxLength(30)
            .IsUnicode(false)
            .HasColumnName("NUMERO");
        entity.Property(factura => factura.Subtotal)
            .HasColumnType("NUMBER(18,2)")
            .HasColumnName("SUBTOTAL");
        entity.Property(factura => factura.Total)
            .HasColumnType("NUMBER(18,2)")
            .HasColumnName("TOTAL");

        entity.HasOne(factura => factura.Cliente)
            .WithMany(cliente => cliente.Facturas)
            .HasForeignKey(factura => factura.Clienteid)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK_FACTURAS_CLIENTES");
    }
}
