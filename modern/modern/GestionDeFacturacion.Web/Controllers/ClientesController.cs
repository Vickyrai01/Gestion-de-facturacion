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
}