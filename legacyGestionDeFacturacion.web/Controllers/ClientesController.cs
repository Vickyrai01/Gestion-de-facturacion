using System.Web.Mvc;
using legacyGestionDeFacturacion.web.Models;

namespace legacyGestionDeFacturacion.web.Controllers
{
    public class ClientesController : Controller
    {
        [HttpGet]
        public ActionResult Index()
        {
            return View(ClientesEnMemoria.Listar());
        }

        [HttpGet]
        public ActionResult Details(int? id)
        {
            var cliente = id.HasValue ? ClientesEnMemoria.Obtener(id.Value) : null;
            if (cliente == null)
            {
                return HttpNotFound();
            }

            return View(cliente);
        }

        [HttpGet]
        public ActionResult Create()
        {
            return View(new Cliente());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Nombre,Apellido,Documento,Email")] Cliente cliente)
        {
            if (!ModelState.IsValid)
            {
                return View(cliente);
            }

            int id = ClientesEnMemoria.Crear(cliente);
            TempData["Mensaje"] = "Cliente creado correctamente.";
            return RedirectToAction("Details", new { id = id });
        }

        [HttpGet]
        public ActionResult Edit(int? id)
        {
            var cliente = id.HasValue ? ClientesEnMemoria.Obtener(id.Value) : null;
            if (cliente == null)
            {
                return HttpNotFound();
            }

            return View(cliente);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int? id, [Bind(Include = "Nombre,Apellido,Documento,Email")] Cliente cliente)
        {
            var actual = id.HasValue ? ClientesEnMemoria.Obtener(id.Value) : null;
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

            if (!ClientesEnMemoria.Editar(actual.Id, cliente))
            {
                return HttpNotFound();
            }

            TempData["Mensaje"] = "Cliente actualizado correctamente.";
            return RedirectToAction("Details", new { id = actual.Id });
        }

        // Acepta POST tradicional y PATCH real o mediante HttpMethodOverride.
        [AcceptVerbs(HttpVerbs.Post | HttpVerbs.Patch)]
        [ValidateAntiForgeryToken]
        public ActionResult Desactivar(int? id)
        {
            if (!id.HasValue || !ClientesEnMemoria.Desactivar(id.Value))
            {
                return HttpNotFound();
            }

            TempData["Mensaje"] = "Cliente desactivado correctamente.";
            return RedirectToAction("Index");
        }
    }
}
