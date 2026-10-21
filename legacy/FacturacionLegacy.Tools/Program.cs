using System;
using System.Data.Entity.Migrations;
using System.Data.Entity.Migrations.Design;
using System.IO;
using System.Resources;
using System.Linq;
using System.Text;
using FacturacionLegacy.Datos;
using FacturacionLegacy.Datos.Entidades;

namespace FacturacionLegacy.Tools
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            try
            {
                var configuration = new Datos.Migraciones.Configuration();
                if (args.Length == 1 && args[0] == "usuarios-demo")
                {
                    using (var db = new FacturacionDbContext())
                    {
                        CrearUsuario(db, "admin", RolesUsuario.Admin, "FACTURACION_ADMIN_PASSWORD");
                        CrearUsuario(db, "operador", RolesUsuario.Operador, "FACTURACION_OPERADOR_PASSWORD");
                    }
                    Console.WriteLine("Cuentas locales preparadas; contraseñas almacenadas únicamente como hashes bcrypt.");
                    return 0;
                }
                if (args.Length == 1 && args[0] == "migrar")
                {
                    new DbMigrator(configuration).Update();
                    Console.WriteLine("Migraciones aplicadas y datos de ejemplo preparados.");
                    return 0;
                }
                if (args.Length == 3 && args[0] == "crear-migracion")
                {
                    var scaffold = new MigrationScaffolder(configuration).Scaffold(args[1]);
                    Directory.CreateDirectory(args[2]);
                    string path = Path.Combine(args[2], scaffold.MigrationId);
                    File.WriteAllText(path + ".cs", scaffold.UserCode);
                    File.WriteAllText(path + ".Designer.cs", scaffold.DesignerCode);
                    using (var writer = new ResXResourceWriter(path + ".resx"))
                        foreach (var resource in scaffold.Resources) writer.AddResource(resource.Key, resource.Value);
                    Console.WriteLine("Migración creada: " + scaffold.MigrationId);
                    return 0;
                }
                Console.WriteLine("Uso: FacturacionLegacy.Tools migrar | usuarios-demo | crear-migracion Nombre Directorio");
                return 1;
            }
            catch (Exception error)
            {
                // No imprimir cadenas de conexión, credenciales ni datos de clientes.
                Console.Error.WriteLine("No se pudo completar la operación: " + error.GetType().Name + ". Revisar configuración y acceso a SQL Server.");
                for (Exception causa = error.InnerException; causa != null; causa = causa.InnerException)
                {
                    Console.Error.WriteLine("Causa: " + causa.GetType().Name);
                    if (causa is FormatException) Console.Error.WriteLine("Formato inválido en metadatos: " + causa.Message);
                }
                return 1;
            }
        }
        private static void CrearUsuario(FacturacionDbContext db, string nombre, string rol, string variable)
        {
            if (db.Usuarios.Any(u => u.NombreUsuario == nombre)) return;
            string password = Environment.GetEnvironmentVariable(variable);
            if (string.IsNullOrEmpty(password))
            {
                Console.Write("Contraseña local para " + nombre + " (mínimo 12 caracteres, entrada oculta): ");
                var entrada = new StringBuilder();
                ConsoleKeyInfo tecla;
                while ((tecla = Console.ReadKey(true)).Key != ConsoleKey.Enter)
                {
                    if (tecla.Key == ConsoleKey.Backspace && entrada.Length > 0) entrada.Length--;
                    else if (!char.IsControl(tecla.KeyChar)) entrada.Append(tecla.KeyChar);
                }
                Console.WriteLine();
                password = entrada.ToString();
            }
            db.Usuarios.Add(new Usuario { NombreUsuario = nombre, Rol = rol, Activo = true, PasswordHash = PasswordSeguridad.CrearHash(password) });
            db.SaveChanges();
        }
    }
}
