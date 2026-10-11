using System.Web.Mvc;
using System.Linq;
using System.Data.Entity;
using FacturacionLegacy.Datos;
using FacturacionLegacy.Datos.Entidades;
using legacyGestionDeFacturacion.web.Seguridad;

namespace legacyGestionDeFacturacion.web.Controllers
{
    [Permiso(Roles = RolesUsuario.Admin + "," + RolesUsuario.Operador)]
    public class ClientesController : Controller
    {
        private readonly FacturacionDbContext db = new FacturacionDbContext();
        [HttpGet]
        public ActionResult Index()
        {
            return View(db.Clientes.AsNoTracking().OrderBy(c => c.Id).ToList());
        }

        [HttpGet]
        public ActionResult Details(int? id)
        {
            var cliente = id.HasValue ? db.Clientes.Find(id.Value) : null;
            if (cliente == null)
            {
                return HttpNotFound();
            }

            return View(cliente);
        }

        [HttpGet]
        [Permiso(Roles = RolesUsuario.Admin)]
        public ActionResult Create()
        {
            return View(new Cliente());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso(Roles = RolesUsuario.Admin)]
        public ActionResult Create([Bind(Include = "Nombre,Apellido,Documento,Email")] Cliente cliente)
        {
            if (!ModelState.IsValid)
            {
                return View(cliente);
            }

            cliente.Activo = true;
            db.Clientes.Add(cliente);
            db.SaveChanges();
            TempData["Mensaje"] = "Cliente creado correctamente.";
            return RedirectToAction("Details", new { id = cliente.Id });
        }

        [HttpGet]
        [Permiso(Roles = RolesUsuario.Admin)]
        public ActionResult Edit(int? id)
        {
            var cliente = id.HasValue ? db.Clientes.Find(id.Value) : null;
            if (cliente == null)
            {
                return HttpNotFound();
            }

            return View(cliente);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso(Roles = RolesUsuario.Admin)]
        public ActionResult Edit(int? id, [Bind(Include = "Nombre,Apellido,Documento,Email")] Cliente cliente)
        {
            var actual = id.HasValue ? db.Clientes.Find(id.Value) : null;
            if (actual == null)
            {
                return HttpNotFound();
            }

            cliente.Id = actual.Id;
            cliente.Activo = actual.Activo;
            if (!ModelState.IsValid)
            {
                return View(cliente);
            }

            actual.Nombre = cliente.Nombre;
            actual.Apellido = cliente.Apellido;
            actual.Documento = cliente.Documento;
            actual.Email = cliente.Email;
            db.SaveChanges();

            TempData["Mensaje"] = "Cliente actualizado correctamente.";
            return RedirectToAction("Details", new { id = actual.Id });
        }

        // Acepta POST tradicional y PATCH real o mediante HttpMethodOverride.
        [AcceptVerbs(HttpVerbs.Post | HttpVerbs.Patch)]
        [ValidateAntiForgeryToken]
        [Permiso(Roles = RolesUsuario.Admin)]
        public ActionResult Desactivar(int? id)
        {
            var cliente = id.HasValue ? db.Clientes.Find(id.Value) : null;
            if (cliente == null)
            {
                return HttpNotFound();
            }

            cliente.Activo = false;
            db.SaveChanges();
            TempData["Mensaje"] = "Cliente desactivado correctamente.";
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
