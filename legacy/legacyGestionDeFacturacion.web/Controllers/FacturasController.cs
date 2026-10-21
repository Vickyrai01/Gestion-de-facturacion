using System;
using System.Linq;
using System.Web.Mvc;
using FacturacionLegacy.Datos;
using FacturacionLegacy.Datos.Entidades;
using legacyGestionDeFacturacion.web.Models;
using legacyGestionDeFacturacion.web.Seguridad;
using FacturacionLegacy.Datos.Facturacion;
using legacyGestionDeFacturacion.web.Composicion;

namespace legacyGestionDeFacturacion.web.Controllers
{
    [Permiso(Roles = RolesUsuario.Admin + "," + RolesUsuario.Operador)]
    public class FacturasController : Controller
    {
        private readonly IServicioFacturacion servicio;
        private readonly IDisposable recursos;

        public FacturasController() : this(new FacturacionWeb()) { }
        private FacturasController(FacturacionWeb composicion)
        {
            servicio = composicion.Servicio;
            recursos = composicion;
        }
        public FacturasController(IServicioFacturacion servicio)
        {
            if (servicio == null) throw new ArgumentNullException("servicio");
            this.servicio = servicio;
        }
        [HttpGet]
        public ActionResult Index(int? clienteId, string estado)
        {
            System.Collections.Generic.IList<FacturaResumenDto> facturas;
            try
            {
                facturas = servicio.Consultar(clienteId, estado);
            }
            catch (ArgumentException) { return new HttpStatusCodeResult(400, "Estado inválido"); }
            ViewBag.Clientes = new SelectList(servicio.Clientes(false).Select(c => new { c.Id, Nombre = c.Nombre + " " + c.Apellido }), "Id", "Nombre", clienteId);
            ViewBag.Estados = new SelectList(EstadosFactura.Todos, estado);
            return View(facturas);
        }
        [HttpGet]
        public ActionResult Details(int? id)
        {
            var factura = id.HasValue ? servicio.Obtener(id.Value) : null;
            if (factura == null) return HttpNotFound();
            return View(factura);
        }
        [HttpGet]
        public ActionResult Create()
        {
            var model = new FacturaCrearModel();
            PrepararFormulario(model);
            return View(model);
        }
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "ClienteId,Detalles")] FacturaCrearModel model)
        {
            if (model.Detalles == null || model.Detalles.Count == 0) ModelState.AddModelError("", "Agregá al menos un detalle.");
            if (ModelState.IsValid)
            {
                try
                {
                    int id = servicio.Crear(new CrearFacturaRequest { ClienteId = model.ClienteId, Detalles = model.Detalles });
                    TempData["Mensaje"] = "Factura creada en estado PENDIENTE.";
                    return RedirectToAction("Details", new { id = id });
                }
                catch (ArgumentException error) { ModelState.AddModelError("", error.Message); }
            }
            if (model.Detalles == null) model.Detalles = new System.Collections.Generic.List<LineaFactura>();
            PrepararFormulario(model);
            return View(model);
        }
        [HttpPost, ValidateAntiForgeryToken]
        [Permiso(Roles = RolesUsuario.Admin)]
        public ActionResult Emitir(int? id)
        {
            if (!id.HasValue) return HttpNotFound();
            var resultado = servicio.Emitir(id.Value);
            if (resultado == ResultadoCambioEstado.NoExiste) return HttpNotFound();
            if (resultado == ResultadoCambioEstado.Conflicto) return new HttpStatusCodeResult(409, "Solo se puede emitir una factura pendiente.");
            TempData["Mensaje"] = "Factura emitida.";
            return RedirectToAction("Details", new { id = id.Value });
        }
        [HttpPost, ValidateAntiForgeryToken]
        [Permiso(Roles = RolesUsuario.Admin)]
        public ActionResult Anular(int? id)
        {
            if (!id.HasValue) return HttpNotFound();
            var resultado = servicio.Anular(id.Value);
            if (resultado == ResultadoCambioEstado.NoExiste) return HttpNotFound();
            if (resultado == ResultadoCambioEstado.Conflicto) return new HttpStatusCodeResult(409, "La factura ya está anulada.");
            TempData["Mensaje"] = "Factura anulada; se conserva su historial.";
            return RedirectToAction("Details", new { id = id.Value });
        }
        [HttpGet]
        [Permiso(Roles = RolesUsuario.Admin)]
        public ActionResult Reportes()
        {
            return View(servicio.Reportes());
        }
        private void PrepararFormulario(FacturaCrearModel model)
        {
            model.Clientes = servicio.Clientes(true).Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Nombre + " " + c.Apellido });
            model.Productos = servicio.ProductosActivos().Select(p => new SelectListItem { Value = p.Id.ToString(), Text = p.Codigo + " — " + p.Nombre + " (" + p.PrecioUnitario.ToString("N2") + ")" });
            model.TasaImpuestos = servicio.TasaImpuestos;
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing && recursos != null) recursos.Dispose();
            base.Dispose(disposing);
        }
    }
}
