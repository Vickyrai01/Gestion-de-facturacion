using GestionDeFacturacion.Web.Models;

namespace GestionDeFacturacion.Web.Services;

public class ClientesService
{
    private readonly List<Cliente> _clientes = [];
    private readonly object _sincronizacion = new();
    private int _siguienteId = 1;

    public List<Cliente> Listar()
    {
        lock (_sincronizacion)
        {
            return _clientes.Select(cliente => new Cliente
            {
                Id = cliente.Id,
                Nombre = cliente.Nombre,
                Email = cliente.Email
            }).ToList();
        }
    }

    public bool Registrar(string nombre, string email)
    {
        nombre = nombre.Trim();
        email = email.Trim();

        lock (_sincronizacion)
        {
            bool existe = _clientes.Any(cliente =>
                string.Equals(
                    cliente.Email,
                    email,
                    StringComparison.OrdinalIgnoreCase));

            if (existe)
            {
                return false;
            }

            _clientes.Add(new Cliente
            {
                Id = _siguienteId++,
                Nombre = nombre,
                Email = email
            });

            return true;
        }
    }
}