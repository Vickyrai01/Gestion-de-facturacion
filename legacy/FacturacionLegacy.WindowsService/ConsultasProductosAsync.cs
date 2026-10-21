using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FacturacionLegacy.Datos;

namespace FacturacionLegacy.WindowsService
{
    public sealed class ProductoConsulta
    {
        public int Id { get; set; }
        public string Codigo { get; set; }
        public decimal PrecioUnitario { get; set; }
    }

    // Recorrido de lectura independiente: no cambia ProductosController ni FacturacionOperaciones.
    public sealed class ConsultasProductosAsync
    {
        private static IQueryable<ProductoConsulta> Proyectar(FacturacionDbContext db)
        {
            return db.Productos.AsNoTracking().Select(p => new ProductoConsulta
            {
                Id = p.Id, Codigo = p.Codigo, PrecioUnitario = p.PrecioUnitario
            });
        }

        public async Task<List<ProductoConsulta>> ListarAsync(CancellationToken cancellationToken)
        {
            using (var db = new FacturacionDbContext())
            {
                db.Database.CommandTimeout = 30;
                return await Proyectar(db).OrderBy(p => p.Codigo).ThenBy(p => p.Id)
                    .Take(20).ToListAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task<ProductoConsulta> ObtenerAsync(int id, CancellationToken cancellationToken)
        {
            if (id <= 0) throw new ArgumentOutOfRangeException("id");
            using (var db = new FacturacionDbContext())
            {
                db.Database.CommandTimeout = 30;
                return await Proyectar(db).FirstOrDefaultAsync(p => p.Id == id, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
