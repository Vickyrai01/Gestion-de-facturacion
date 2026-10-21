using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using FacturacionLegacy.Datos;

namespace legacyGestionDeFacturacion.web.Models
{
    public class FacturaCrearModel
    {
        public FacturaCrearModel() { Detalles = new List<LineaFactura> { new LineaFactura { Cantidad = 1 } }; }
        [Range(1, int.MaxValue, ErrorMessage = "Seleccioná un cliente.")]
        public int ClienteId { get; set; }
        public List<LineaFactura> Detalles { get; set; }
        public IEnumerable<SelectListItem> Clientes { get; set; }
        public IEnumerable<SelectListItem> Productos { get; set; }
        public decimal TasaImpuestos { get; set; }
    }
}
