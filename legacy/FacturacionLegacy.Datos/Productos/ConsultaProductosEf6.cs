using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FacturacionLegacy.Datos.Productos
{
    public class ConsultaProductosEf6 : IConsultaProductos
    {
        private readonly FacturacionDbContext db;
        public ConsultaProductosEf6(FacturacionDbContext db) { this.db = db; }
        public IList<ProductoConsultaDto> ListarActivos()
        {
            return Proyectar(db.Productos.AsNoTracking().Where(p => p.Activo).OrderBy(p => p.Codigo)).ToList();
        }
        public IList<ProductoConsultaDto> Obtener(int[] ids)
        {
            return Proyectar(db.Productos.AsNoTracking().Where(p => ids.Contains(p.Id))).ToList();
        }
        public async Task<List<ProductoConsultaDto>> ListarAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await Proyectar(db.Productos.AsNoTracking()).OrderBy(p => p.Codigo).ThenBy(p => p.Id)
                .Take(20).ToListAsync(cancellationToken).ConfigureAwait(false);
        }
        public async Task<ProductoConsultaDto> ObtenerAsync(int id, CancellationToken cancellationToken)
        {
            if (id <= 0) throw new ArgumentOutOfRangeException("id");
            cancellationToken.ThrowIfCancellationRequested();
            return await Proyectar(db.Productos.AsNoTracking())
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken).ConfigureAwait(false);
        }
        private static IQueryable<ProductoConsultaDto> Proyectar(IQueryable<Entidades.Producto> productos)
        {
            return productos.Select(p => new ProductoConsultaDto { Id = p.Id, Codigo = p.Codigo, Nombre = p.Nombre, PrecioUnitario = p.PrecioUnitario, Activo = p.Activo });
        }
    }
}
