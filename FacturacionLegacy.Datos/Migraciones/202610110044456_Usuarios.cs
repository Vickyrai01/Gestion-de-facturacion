namespace FacturacionLegacy.Datos.Migraciones
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Usuarios : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Usuarios",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        NombreUsuario = c.String(nullable: false, maxLength: 50),
                        PasswordHash = c.String(nullable: false, maxLength: 100),
                        Rol = c.String(nullable: false, maxLength: 10),
                        Activo = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .Index(t => t.NombreUsuario, unique: true, name: "UX_Usuario_Nombre");
            Sql("ALTER TABLE dbo.Usuarios ADD CONSTRAINT CK_Usuario_Rol CHECK (Rol IN ('ADMIN', 'OPERADOR'))");
            
        }
        
        public override void Down()
        {
            DropIndex("dbo.Usuarios", "UX_Usuario_Nombre");
            DropTable("dbo.Usuarios");
        }
    }
}
