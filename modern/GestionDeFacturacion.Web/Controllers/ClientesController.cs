using GestionDeFacturacion.Web.Models;
using GestionDeFacturacion.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace GestionDeFacturacion.Web.Controllers;

public class ClientesController: Controller
{
    private readonly ClientesService clienteService;

    public ClientesController(ClientesService service)
    {
        clienteService = service;
    }

    [HttpGet]
    public IActionResult Index()
    {
        var clientes = clienteService.Listar();
        return View(clientes);

        // return View(clienteService.Listar());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Crear([Bind("Nombre,Email")] Cliente cliente)
    {
        if (ModelState.IsValid)
        {
            if (clienteService.Registrar(cliente.Nombre, cliente.Email))
            {
                TempData["Mensaje"] = "Cliente registrado correctamente.";
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError(nameof(cliente.Email), "Ya existe un cliente con ese email.");
        }

        return View("Index", clienteService.Listar());
    }

}
