using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace FacturacionLegacy.Datos.Clientes
{
    public class ConsultaClientesEf6 : IConsultaClientes
    {
        private readonly FacturacionDbContext db;
        public ConsultaClientesEf6(FacturacionDbContext db) { this.db = db; }
        public bool EstaActivo(int id) { return db.Clientes.Any(c => c.Id == id && c.Activo); }
        public IList<ClienteOpcionDto> Listar(bool soloActivos)
        {
            var clientes = db.Clientes.AsNoTracking().AsQueryable();
            if (soloActivos) clientes = clientes.Where(c => c.Activo);
            return clientes.OrderBy(c => c.Apellido).Select(c => new ClienteOpcionDto { Id = c.Id, Nombre = c.Nombre, Apellido = c.Apellido }).ToList();
        }
    }
}
