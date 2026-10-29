using GestionDeFacturacion.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace GestionDeFacturacion.Web.Controllers;

public class ClienteController: Controller
{
    private readonly ClienteService clienteService;

    public ClienteController(ClienteService service)
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
}