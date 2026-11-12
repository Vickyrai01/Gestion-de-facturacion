using Facturacion.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Facturacion.Infrastructure.Persistence.Configurations;

public sealed class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> entity)
    {
        entity.HasKey(cliente => cliente.Id)
            .HasName("SYS_C008665");

        entity.ToTable("CLIENTES");

        entity.Property(cliente => cliente.Id)
            .HasPrecision(10)
            .HasColumnName("ID");
        entity.Property(cliente => cliente.Activo)
            .HasColumnType("NUMBER(1)")
            .HasColumnName("ACTIVO");
        entity.Property(cliente => cliente.Apellido)
            .HasMaxLength(100)
            .IsUnicode(false)
            .HasColumnName("APELLIDO");
        entity.Property(cliente => cliente.Documento)
            .HasMaxLength(30)
            .IsUnicode(false)
            .HasColumnName("DOCUMENTO");
        entity.Property(cliente => cliente.Email)
            .HasMaxLength(254)
            .IsUnicode(false)
            .HasColumnName("EMAIL");
        entity.Property(cliente => cliente.Nombre)
            .HasMaxLength(100)
            .IsUnicode(false)
            .HasColumnName("NOMBRE");
    }
}
