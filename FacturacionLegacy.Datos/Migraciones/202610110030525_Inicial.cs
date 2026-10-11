namespace FacturacionLegacy.Datos.Migraciones
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Inicial : DbMigration
    {
        public override void Up()
        {
            Sql("CREATE SEQUENCE dbo.NumeroFactura AS BIGINT START WITH 1 INCREMENT BY 1 NO CYCLE");
            CreateTable(
                "dbo.Clientes",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Nombre = c.String(nullable: false, maxLength: 100),
                        Apellido = c.String(nullable: false, maxLength: 100),
                        Documento = c.String(nullable: false, maxLength: 30),
                        Email = c.String(nullable: false, maxLength: 254),
                        Activo = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.Facturas",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Numero = c.String(nullable: false, maxLength: 30),
                        ClienteId = c.Int(nullable: false),
                        FechaEmision = c.DateTime(nullable: false),
                        Estado = c.String(nullable: false, maxLength: 10),
                        Subtotal = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Impuestos = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Total = c.Decimal(nullable: false, precision: 18, scale: 2),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Clientes", t => t.ClienteId)
                .Index(t => t.Numero, unique: true, name: "UX_Factura_Numero")
                .Index(t => t.ClienteId);
            
            CreateTable(
                "dbo.DetallesFactura",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        FacturaId = c.Int(nullable: false),
                        ProductoId = c.Int(nullable: false),
                        Cantidad = c.Int(nullable: false),
                        PrecioUnitario = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Subtotal = c.Decimal(nullable: false, precision: 18, scale: 2),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Facturas", t => t.FacturaId)
                .ForeignKey("dbo.Productos", t => t.ProductoId)
                .Index(t => t.FacturaId)
                .Index(t => t.ProductoId);
            
            CreateTable(
                "dbo.Productos",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Codigo = c.String(nullable: false, maxLength: 30),
                        Nombre = c.String(nullable: false, maxLength: 100),
                        Descripcion = c.String(maxLength: 500),
                        PrecioUnitario = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Stock = c.Int(nullable: false),
                        Activo = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .Index(t => t.Codigo, unique: true, name: "UX_Producto_Codigo");
            Sql("ALTER TABLE dbo.Productos ADD CONSTRAINT CK_Producto_Importes CHECK (PrecioUnitario >= 0 AND Stock >= 0)");
            Sql("ALTER TABLE dbo.DetallesFactura ADD CONSTRAINT CK_Detalle_Importes CHECK (Cantidad > 0 AND PrecioUnitario >= 0 AND Subtotal >= 0)");
            Sql("ALTER TABLE dbo.Facturas ADD CONSTRAINT CK_Factura_Estado CHECK (Estado IN ('PENDIENTE', 'EMITIDA', 'ANULADA'))");
            Sql("ALTER TABLE dbo.Facturas ADD CONSTRAINT CK_Factura_Importes CHECK (Subtotal >= 0 AND Impuestos >= 0 AND Total >= 0)");
            
        }
        
        public override void Down()
        {
            Sql("DROP SEQUENCE dbo.NumeroFactura");
            DropForeignKey("dbo.DetallesFactura", "ProductoId", "dbo.Productos");
            DropForeignKey("dbo.DetallesFactura", "FacturaId", "dbo.Facturas");
            DropForeignKey("dbo.Facturas", "ClienteId", "dbo.Clientes");
            DropIndex("dbo.Productos", "UX_Producto_Codigo");
            DropIndex("dbo.DetallesFactura", new[] { "ProductoId" });
            DropIndex("dbo.DetallesFactura", new[] { "FacturaId" });
            DropIndex("dbo.Facturas", new[] { "ClienteId" });
            DropIndex("dbo.Facturas", "UX_Factura_Numero");
            DropTable("dbo.Productos");
            DropTable("dbo.DetallesFactura");
            DropTable("dbo.Facturas");
            DropTable("dbo.Clientes");
        }
    }
}
