using System;
using System.Configuration;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;
using FacturacionLegacy.Datos;
using FacturacionLegacy.Datos.Entidades;
using legacyGestionDeFacturacion.web.Models;
using legacyGestionDeFacturacion.web.Seguridad;

namespace legacyGestionDeFacturacion.web.Controllers
{
    [Permiso(Roles = RolesUsuario.Admin + "," + RolesUsuario.Operador)]
    public class FacturasController : Controller
    {
        private readonly FacturacionDbContext db = new FacturacionDbContext();
        [HttpGet]
        public ActionResult Index(int? clienteId, string estado)
        {
            if (!string.IsNullOrEmpty(estado) && !EstadosFactura.Todos.Contains(estado)) return new HttpStatusCodeResult(400, "Estado inválido");
            ViewBag.Clientes = new SelectList(db.Clientes.AsNoTracking().OrderBy(c => c.Apellido).ToList().Select(c => new { c.Id, Nombre = c.Nombre + " " + c.Apellido }), "Id", "Nombre", clienteId);
            ViewBag.Estados = new SelectList(EstadosFactura.Todos, estado);
            return View(FacturacionOperaciones.Consultar(db, clienteId, estado).ToList());
        }
        [HttpGet]
        public ActionResult Details(int? id)
        {
            var factura = id.HasValue ? FacturacionOperaciones.Obtener(db, id.Value) : null;
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
                    int id = FacturacionOperaciones.Crear(db, model.ClienteId, model.Detalles, ObtenerTasa());
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
            if (!id.HasValue || !db.Facturas.Any(f => f.Id == id.Value)) return HttpNotFound();
            if (!FacturacionOperaciones.Emitir(db, id.Value)) return new HttpStatusCodeResult(409, "Solo se puede emitir una factura pendiente.");
            TempData["Mensaje"] = "Factura emitida.";
            return RedirectToAction("Details", new { id = id.Value });
        }
        [HttpPost, ValidateAntiForgeryToken]
        [Permiso(Roles = RolesUsuario.Admin)]
        public ActionResult Anular(int? id)
        {
            if (!id.HasValue || !db.Facturas.Any(f => f.Id == id.Value)) return HttpNotFound();
            if (!FacturacionOperaciones.Anular(db, id.Value)) return new HttpStatusCodeResult(409, "La factura ya está anulada.");
            TempData["Mensaje"] = "Factura anulada; se conserva su historial.";
            return RedirectToAction("Details", new { id = id.Value });
        }
        [HttpGet]
        [Permiso(Roles = RolesUsuario.Admin)]
        public ActionResult Reportes()
        {
            return View(db.Facturas.AsNoTracking().GroupBy(f => f.Estado).Select(g => new ReporteEstado { Estado = g.Key, Cantidad = g.Count(), Total = g.Sum(f => f.Total) }).ToList());
        }
        private static decimal ObtenerTasa()
        {
            decimal tasa;
            if (!decimal.TryParse(ConfigurationManager.AppSettings["TasaImpuestos"], NumberStyles.Number, CultureInfo.InvariantCulture, out tasa) || tasa < 0 || tasa > 1)
                throw new ConfigurationErrorsException("TasaImpuestos debe ser un decimal entre 0 y 1.");
            return tasa;
        }
        private void PrepararFormulario(FacturaCrearModel model)
        {
            model.Clientes = db.Clientes.AsNoTracking().Where(c => c.Activo).OrderBy(c => c.Apellido).ToList().Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Nombre + " " + c.Apellido });
            model.Productos = db.Productos.AsNoTracking().Where(p => p.Activo).OrderBy(p => p.Codigo).ToList().Select(p => new SelectListItem { Value = p.Id.ToString(), Text = p.Codigo + " — " + p.Nombre + " (" + p.PrecioUnitario.ToString("N2") + ")" });
            model.TasaImpuestos = ObtenerTasa();
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
    public class ReporteEstado
    {
        public string Estado { get; set; }
        public int Cantidad { get; set; }
        public decimal Total { get; set; }
    }
}
