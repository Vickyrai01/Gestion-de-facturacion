using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Mvc;
using FacturacionLegacy.Datos;
using FacturacionLegacy.Datos.Entidades;
using legacyGestionDeFacturacion.web.Seguridad;

namespace legacyGestionDeFacturacion.web.Controllers
{
    [Permiso(Roles = RolesUsuario.Admin + "," + RolesUsuario.Operador)]
    public class ProductosController : Controller
    {
        private readonly FacturacionDbContext db = new FacturacionDbContext();
        [HttpGet]
        public ActionResult Index() { return View(db.Productos.AsNoTracking().OrderBy(p => p.Codigo).ToList()); }
        [HttpGet]
        public ActionResult Details(int? id)
        {
            var producto = id.HasValue ? db.Productos.Find(id.Value) : null;
            if (producto == null) return HttpNotFound();
            return View(producto);
        }
        [HttpGet]
        [Permiso(Roles = RolesUsuario.Admin)]
        public ActionResult Create() { return View(new Producto()); }
        [HttpPost, ValidateAntiForgeryToken]
        [Permiso(Roles = RolesUsuario.Admin)]
        public ActionResult Create([Bind(Include = "Codigo,Nombre,Descripcion,PrecioUnitario,Stock")] Producto producto)
        {
            ValidarCodigo(producto, 0);
            if (!ModelState.IsValid) return View(producto);
            producto.Activo = true;
            producto.PrecioUnitario = decimal.Round(producto.PrecioUnitario, 2, MidpointRounding.AwayFromZero);
            db.Productos.Add(producto);
            if (!Guardar()) return View(producto);
            TempData["Mensaje"] = "Producto creado correctamente.";
            return RedirectToAction("Details", new { id = producto.Id });
        }
        [HttpGet]
        [Permiso(Roles = RolesUsuario.Admin)]
        public ActionResult Edit(int? id)
        {
            var producto = id.HasValue ? db.Productos.Find(id.Value) : null;
            if (producto == null) return HttpNotFound();
            return View(producto);
        }
        [HttpPost, ValidateAntiForgeryToken]
        [Permiso(Roles = RolesUsuario.Admin)]
        public ActionResult Edit(int? id, [Bind(Include = "Codigo,Nombre,Descripcion,PrecioUnitario,Stock")] Producto producto)
        {
            var actual = id.HasValue ? db.Productos.Find(id.Value) : null;
            if (actual == null) return HttpNotFound();
            producto.Id = actual.Id;
            producto.Activo = actual.Activo;
            ValidarCodigo(producto, actual.Id);
            if (!ModelState.IsValid) return View(producto);
            actual.Codigo = producto.Codigo;
            actual.Nombre = producto.Nombre;
            actual.Descripcion = producto.Descripcion;
            actual.PrecioUnitario = decimal.Round(producto.PrecioUnitario, 2, MidpointRounding.AwayFromZero);
            actual.Stock = producto.Stock;
            if (!Guardar()) return View(producto);
            TempData["Mensaje"] = "Producto actualizado correctamente.";
            return RedirectToAction("Details", new { id = actual.Id });
        }
        [HttpPost, ValidateAntiForgeryToken]
        [Permiso(Roles = RolesUsuario.Admin)]
        public ActionResult Desactivar(int? id)
        {
            var producto = id.HasValue ? db.Productos.Find(id.Value) : null;
            if (producto == null) return HttpNotFound();
            producto.Activo = false;
            db.SaveChanges();
            TempData["Mensaje"] = "Producto desactivado correctamente.";
            return RedirectToAction("Index");
        }
        private void ValidarCodigo(Producto producto, int id)
        {
            producto.Codigo = (producto.Codigo ?? "").Trim().ToUpperInvariant();
            if (db.Productos.Any(p => p.Codigo == producto.Codigo && p.Id != id))
                ModelState.AddModelError("Codigo", "Ya existe un producto con ese código.");
        }
        private bool Guardar()
        {
            try { db.SaveChanges(); return true; }
            catch (DbUpdateException error)
            {
                var sql = error.GetBaseException() as SqlException;
                if (sql == null || (sql.Number != 2601 && sql.Number != 2627)) throw;
                ModelState.AddModelError("Codigo", "Ya existe un producto con ese código.");
                return false;
            }
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
