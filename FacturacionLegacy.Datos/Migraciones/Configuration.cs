using System;
using System.Data.Entity.Migrations;
using System.Linq;
using FacturacionLegacy.Datos.Entidades;

namespace FacturacionLegacy.Datos.Migraciones
{
    public sealed class Configuration : DbMigrationsConfiguration<FacturacionDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
            ContextKey = "FacturacionLegacy.Datos.FacturacionDbContext";
            string conexionPruebas = Environment.GetEnvironmentVariable("FACTURACION_DB_CONNECTION");
            if (!string.IsNullOrEmpty(conexionPruebas))
                TargetDatabase = new System.Data.Entity.Infrastructure.DbConnectionInfo(conexionPruebas, "System.Data.SqlClient");
        }
        protected override void Seed(FacturacionDbContext db)
        {
            // Insertar los ejemplos faltantes, sin sobrescribir ediciones del usuario.
            string[] nombres = { "Ana", "Luis", "Marta", "Diego", "Sofía" };
            string[] apellidos = { "García", "Pérez", "López", "Ruiz", "Torres" };
            string[] documentos = { "30123456", "28987654", "32555666", "33444555", "35666777" };
            for (int i = 0; i < nombres.Length; i++)
            {
                string documento = documentos[i];
                if (!db.Clientes.Any(c => c.Documento == documento))
                    db.Clientes.Add(new Cliente { Nombre = nombres[i], Apellido = apellidos[i], Documento = documento, Email = "cliente" + (i + 1) + "@example.com", Activo = i != 2 });
            }
            for (int i = 1; i <= 8; i++)
            {
                string codigo = "PROD-" + i.ToString("D3");
                if (!db.Productos.Any(p => p.Codigo == codigo))
                    db.Productos.Add(new Producto { Codigo = codigo, Nombre = "Producto de ejemplo " + i, Descripcion = "Material didáctico", PrecioUnitario = i * 100m, Stock = i * 10, Activo = true });
            }
            db.SaveChanges();
            string documentoInicial = documentos[0];
            var cliente = db.Clientes.First(c => c.Documento == documentoInicial);
            var productos = db.Productos.OrderBy(p => p.Id).Take(2).ToList();
            for (int i = 0; i < EstadosFactura.Todos.Length; i++)
            {
                string numero = "DEMO-" + (i + 1).ToString("D3");
                if (db.Facturas.Any(f => f.Numero == numero)) continue;
                var factura = new Factura { Numero = numero, ClienteId = cliente.Id, FechaEmision = DateTime.UtcNow.Date.AddDays(-i), Estado = EstadosFactura.Todos[i] };
                foreach (var producto in productos)
                    factura.Detalles.Add(new DetalleFactura { ProductoId = producto.Id, Cantidad = 2, PrecioUnitario = producto.PrecioUnitario, Subtotal = producto.PrecioUnitario * 2 });
                factura.Subtotal = factura.Detalles.Sum(d => d.Subtotal);
                factura.Impuestos = decimal.Round(factura.Subtotal * 0.21m, 2, MidpointRounding.AwayFromZero);
                factura.Total = factura.Subtotal + factura.Impuestos;
                db.Facturas.Add(factura);
            }
            db.SaveChanges();
        }
    }
}
