using System;
using System.Collections.Generic;
using System.Linq;
using FacturacionLegacy.WebForms.Models;

namespace FacturacionLegacy.WebForms.Datos
{
    public static class ClientesEnMemoria
    {
        private static readonly object Sincronizacion = new object();
        private static readonly List<Cliente> Clientes = new List<Cliente>();
        private static int siguienteId = 1;

        public static List<Cliente> Listar()
        {
            lock (Sincronizacion)
            {
                // Copias: la página no puede modificar la colección compartida.
                return Clientes.Select(c => new Cliente
                {
                    Id = c.Id, Nombre = c.Nombre, Email = c.Email
                }).ToList();
            }
        }

        public static bool Registrar(string nombre, string email)
        {
            lock (Sincronizacion)
            {
                // Comprobación y alta atómicas, incluso con dos solicitudes simultáneas.
                if (Clientes.Any(c => string.Equals(c.Email, email, StringComparison.OrdinalIgnoreCase)))
                    return false;

                Clientes.Add(new Cliente { Id = siguienteId++, Nombre = nombre, Email = email });
                return true;
            }
        }
    }
}
