using System.Collections.Generic;
using System.Linq;

namespace legacyGestionDeFacturacion.web.Models
{
    // Una única colección para todas las peticiones de este AppDomain.
    // Se reinicia al reiniciar o reciclar la aplicación; no es una base de datos.
    public static class ClientesEnMemoria
    {
        private static readonly object Sincronizacion = new object();
        private static readonly List<Cliente> Clientes = new List<Cliente>
        {
            new Cliente { Id = 1, Nombre = "Ana", Apellido = "García", Documento = "30123456", Email = "ana.garcia@example.com", Activo = true },
            new Cliente { Id = 2, Nombre = "Luis", Apellido = "Pérez", Documento = "28987654", Email = "luis.perez@example.com", Activo = true },
            new Cliente { Id = 3, Nombre = "Marta", Apellido = "López", Documento = "32555666", Email = "marta.lopez@example.com", Activo = false }
        };
        private static int ProximoId = 4;

        public static List<Cliente> Listar()
        {
            lock (Sincronizacion)
            {
                return Clientes.Select(Copiar).ToList();
            }
        }

        public static Cliente Obtener(int id)
        {
            lock (Sincronizacion)
            {
                var cliente = Clientes.FirstOrDefault(c => c.Id == id);
                return cliente == null ? null : Copiar(cliente);
            }
        }

        public static int Crear(Cliente datos)
        {
            lock (Sincronizacion)
            {
                var cliente = Copiar(datos);
                cliente.Id = ProximoId++;
                cliente.Activo = true;
                Clientes.Add(cliente);
                return cliente.Id;
            }
        }

        public static bool Editar(int id, Cliente datos)
        {
            lock (Sincronizacion)
            {
                var cliente = Clientes.FirstOrDefault(c => c.Id == id);
                if (cliente == null)
                {
                    return false;
                }

                cliente.Nombre = datos.Nombre;
                cliente.Apellido = datos.Apellido;
                cliente.Documento = datos.Documento;
                cliente.Email = datos.Email;
                // Editar los datos personales no cambia el estado del cliente.
                return true;
            }
        }

        public static bool Desactivar(int id)
        {
            lock (Sincronizacion)
            {
                var cliente = Clientes.FirstOrDefault(c => c.Id == id);
                if (cliente == null)
                {
                    return false;
                }

                cliente.Activo = false;
                return true;
            }
        }

        // Las vistas y el model binding trabajan con copias: ningún consumidor
        // puede modificar la colección fuera del lock.
        private static Cliente Copiar(Cliente cliente)
        {
            return new Cliente
            {
                Id = cliente.Id,
                Nombre = cliente.Nombre,
                Apellido = cliente.Apellido,
                Documento = cliente.Documento,
                Email = cliente.Email,
                Activo = cliente.Activo
            };
        }
    }
}
