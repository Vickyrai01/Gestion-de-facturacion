using Microsoft.AspNetCore.Mvc;
using GestionDeFacturacion.web.Models;
using Newtonsoft.Json;

namespace GestionDeFacturacion.web.Controllers
{
    public class ClientesController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View(ClientesEnMemoria.Listar());
        }

        [HttpGet]
        public IActionResult Details(int? id)
        {
            var cliente = id.HasValue ? ClientesEnMemoria.Obtener(id.Value) : null;
            if (cliente == null)
            {
                return NotFound();
            }

            return View(cliente);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new Cliente());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create([Bind("Nombre,Apellido,Documento,Email")] Cliente cliente)
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
        public IActionResult Edit(int? id)
        {
            var cliente = id.HasValue ? ClientesEnMemoria.Obtener(id.Value) : null;
            if (cliente == null)
            {
                return NotFound();
            }

            return View(cliente);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int? id, [Bind("Nombre,Apellido,Documento,Email")] Cliente cliente)
        {
            var actual = id.HasValue ? ClientesEnMemoria.Obtener(id.Value) : null;
            if (actual == null)
            {
                return NotFound();
            }

            cliente.Id = actual.Id;
            cliente.Activo = actual.Activo;
            if (!ModelState.IsValid)
            {
                return View(cliente);
            }

            if (!ClientesEnMemoria.Editar(actual.Id, cliente))
            {
                return NotFound();
            }

            TempData["Mensaje"] = "Cliente actualizado correctamente.";
            return RedirectToAction("Details", new { id = actual.Id });
        }

        // Acepta POST tradicional y PATCH real o mediante HttpMethodOverride.
        [AcceptVerbs("POST" , "PATCH")]
        [ValidateAntiForgeryToken]
        public IActionResult Desactivar(int? id)
        {
            if (!id.HasValue || !ClientesEnMemoria.Desactivar(id.Value))
            {
                return NotFound();
            }

            TempData["Mensaje"] = "Cliente desactivado correctamente.";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult EjemploJson()
        {
            var cliente = new
            {
                Id = 1,
                Nombre = "Ana",
                Apellido = "Pérez"
            };

            var json = JsonConvert.SerializeObject(cliente);

            return Content(json, "application/json");
        }
    }
}
