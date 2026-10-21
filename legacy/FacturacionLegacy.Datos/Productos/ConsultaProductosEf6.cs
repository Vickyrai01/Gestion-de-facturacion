using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace FacturacionLegacy.Datos.Productos
{
    public class ConsultaProductosEf6 : IConsultaProductos
    {
        private readonly FacturacionDbContext db;
        public ConsultaProductosEf6(FacturacionDbContext db) { this.db = db; }
        public IList<ProductoConsultaDto> ListarActivos()
        {
            return Proyectar(db.Productos.AsNoTracking().Where(p => p.Activo).OrderBy(p => p.Codigo));
        }
        public IList<ProductoConsultaDto> Obtener(int[] ids)
        {
            return Proyectar(db.Productos.AsNoTracking().Where(p => ids.Contains(p.Id)));
        }
        private static IList<ProductoConsultaDto> Proyectar(IQueryable<Entidades.Producto> productos)
        {
            return productos.Select(p => new ProductoConsultaDto { Id = p.Id, Codigo = p.Codigo, Nombre = p.Nombre, PrecioUnitario = p.PrecioUnitario, Activo = p.Activo }).ToList();
        }
    }
}
