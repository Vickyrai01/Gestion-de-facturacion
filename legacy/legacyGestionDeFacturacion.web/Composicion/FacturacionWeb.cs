using System;
using System.Configuration;
using System.Globalization;
using FacturacionLegacy.Datos;
using FacturacionLegacy.Datos.Clientes;
using FacturacionLegacy.Datos.Productos;
using FacturacionLegacy.Datos.Facturacion;
using FacturacionLegacy.Datos.Facturacion.Aplicacion;
using FacturacionLegacy.Datos.Facturacion.Infraestructura;

namespace legacyGestionDeFacturacion.web.Composicion
{
    // Composición manual por petición: los tres adaptadores comparten el contexto
    // y por lo tanto la transacción Serializable de la creación.
    internal sealed class FacturacionWeb : IDisposable
    {
        private readonly FacturacionDbContext db;
        public IServicioFacturacion Servicio { get; private set; }

        public FacturacionWeb()
        {
            db = new FacturacionDbContext();
            Servicio = new ServicioFacturacion(new RepositorioFacturasEf6(db), new ConsultaClientesEf6(db), new ConsultaProductosEf6(db), ObtenerTasa);
        }
        private static decimal ObtenerTasa()
        {
            decimal tasa;
            if (!decimal.TryParse(ConfigurationManager.AppSettings["TasaImpuestos"], NumberStyles.Number, CultureInfo.InvariantCulture, out tasa) || tasa < 0 || tasa > 1)
                throw new ConfigurationErrorsException("TasaImpuestos debe ser un decimal entre 0 y 1.");
            return tasa;
        }
        public void Dispose() { db.Dispose(); }
    }
}
