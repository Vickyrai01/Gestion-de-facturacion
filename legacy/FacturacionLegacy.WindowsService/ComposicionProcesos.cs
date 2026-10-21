using FacturacionLegacy.Datos;
using FacturacionLegacy.Datos.Facturacion;
using FacturacionLegacy.Datos.Facturacion.Infraestructura;

namespace FacturacionLegacy.WindowsService
{
    // Mismo patrón de composición manual que FacturacionWeb; un contexto por ciclo.
    internal static class ComposicionProcesos
    {
        public static int ContarPendientes()
        {
            using (var db = new FacturacionDbContext())
            {
                db.Database.CommandTimeout = 30;
                IRepositorioFacturas facturas = new RepositorioFacturasEf6(db);
                return facturas.ContarPendientes();
            }
        }
    }
}
