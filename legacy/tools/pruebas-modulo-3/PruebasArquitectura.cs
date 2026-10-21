using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using FacturacionLegacy.Datos;
using FacturacionLegacy.Datos.Clientes;
using FacturacionLegacy.Datos.Productos;
using FacturacionLegacy.Datos.Facturacion;
using FacturacionLegacy.Datos.Facturacion.Aplicacion;
using FacturacionLegacy.Datos.Facturacion.Dominio;
using legacyGestionDeFacturacion.web.Controllers;
using legacyGestionDeFacturacion.web.Models;

internal static class PruebasArquitectura
{
    private static int verificaciones;
    private static void Comprobar(bool condicion, string descripcion)
    {
        if (!condicion) throw new Exception("FALLÓ: " + descripcion);
        verificaciones++;
        Console.WriteLine("OK: " + descripcion);
    }
    private static void Rechaza(Action accion, string descripcion)
    {
        bool rechazo = false;
        try { accion(); } catch (ArgumentException) { rechazo = true; }
        Comprobar(rechazo, descripcion);
    }
    private static List<LineaFactura> Lineas()
    {
        return new List<LineaFactura> { new LineaFactura { ProductoId = 1, Cantidad = 2 }, new LineaFactura { ProductoId = 2, Cantidad = 3 } };
    }
    private static Dictionary<int, decimal> Precios()
    {
        return new Dictionary<int, decimal> { { 1, 125.50m }, { 2, 100m } };
    }

    public static int Main()
    {
        try
        {
            ProbarDominio();
            ProbarServicio();
            ProbarController();
            Console.WriteLine("TOTAL: " + verificaciones + " verificaciones de arquitectura correctas, sin SQL Server.");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error.Message);
            return 1;
        }
    }

    private static void ProbarDominio()
    {
        var lineas = Lineas();
        var precios = Precios();
        var factura = FacturaEnPreparacion.Crear(5, lineas, precios, 0.21m);
        Comprobar(factura.Subtotal == 551m && factura.Impuestos == 115.71m && factura.Total == 666.71m, "Cálculo decimal de la factura de referencia");
        Comprobar(factura.FechaEmision.Kind == DateTimeKind.Utc && factura.ClienteId == 5, "Cabecera con cliente y fecha UTC");
        lineas[0].Cantidad = 90; precios[1] = 999m;
        Comprobar(factura.Detalles[0].Cantidad == 2 && factura.Detalles[0].PrecioUnitario == 125.50m, "El resultado no conserva referencias mutables de entrada");
        bool soloLectura = false;
        try { ((IList<DetalleCalculado>)factura.Detalles).Clear(); } catch (NotSupportedException) { soloLectura = true; }
        Comprobar(soloLectura, "La colección calculada no admite quitar detalles");
        var mitad = FacturaEnPreparacion.Crear(1, new List<LineaFactura> { new LineaFactura { ProductoId = 1, Cantidad = 1 } }, new Dictionary<int, decimal> { { 1, 0.50m } }, 0.01m);
        Comprobar(mitad.Impuestos == 0.01m && mitad.Total == 0.51m, "Redondeo AwayFromZero en el punto medio");
        Comprobar(FacturaEnPreparacion.Crear(1, Lineas(), Precios(), 0m).Total == 551m, "Tasa cero admitida");
        Comprobar(FacturaEnPreparacion.Crear(1, Lineas(), Precios(), 1m).Total == 1102m, "Tasa uno admitida");
        Rechaza(() => FacturaEnPreparacion.Crear(1, null, Precios(), 0.21m), "Rechaza detalles nulos");
        Rechaza(() => FacturaEnPreparacion.Crear(1, new List<LineaFactura>(), Precios(), 0.21m), "Rechaza factura vacía");
        Rechaza(() => FacturaEnPreparacion.Crear(1, Enumerable.Range(1, 101).Select(_ => new LineaFactura { ProductoId = 1, Cantidad = 1 }).ToList(), Precios(), 0.21m), "Rechaza más de cien detalles");
        Rechaza(() => FacturaEnPreparacion.Crear(1, new List<LineaFactura> { null }, Precios(), 0.21m), "Rechaza línea nula");
        foreach (int cantidad in new[] { -1, 0, 1000001 })
            Rechaza(() => FacturaEnPreparacion.Crear(1, new List<LineaFactura> { new LineaFactura { ProductoId = 1, Cantidad = cantidad } }, Precios(), 0.21m), "Rechaza cantidad fuera de rango: " + cantidad);
        Rechaza(() => FacturaEnPreparacion.Crear(1, new List<LineaFactura> { new LineaFactura { ProductoId = 0, Cantidad = 1 } }, Precios(), 0.21m), "Rechaza identificador de producto inválido");
        Rechaza(() => FacturaEnPreparacion.Crear(1, Lineas(), Precios(), -0.01m), "Rechaza tasa negativa");
        Rechaza(() => FacturaEnPreparacion.Crear(1, Lineas(), Precios(), 1.01m), "Rechaza tasa mayor a uno");
        Rechaza(() => FacturaEnPreparacion.Crear(1, Lineas(), new Dictionary<int, decimal>(), 0.21m), "Rechaza un precio ausente");
        Rechaza(() => FacturaEnPreparacion.Crear(1, Lineas(), new Dictionary<int, decimal> { { 1, -1m }, { 2, 100m } }, 0.21m), "Rechaza precio negativo");
        Rechaza(() => FacturaEnPreparacion.Crear(1, Lineas(), new Dictionary<int, decimal> { { 1, decimal.MaxValue }, { 2, 100m } }, 0.21m), "Rechaza importes excesivos sin overflow");
        var repetidos = FacturaEnPreparacion.Crear(1, new List<LineaFactura> { new LineaFactura { ProductoId = 1, Cantidad = 1 }, new LineaFactura { ProductoId = 1, Cantidad = 2 } }, Precios(), 0m);
        Comprobar(repetidos.Detalles.Count == 2 && repetidos.Total == 376.50m, "Conserva líneas repetidas permitidas por el Módulo 2");
        var limite = FacturaEnPreparacion.Crear(1, Enumerable.Range(1, 100).Select(_ => new LineaFactura { ProductoId = 1, Cantidad = 1000000 }).ToList(), new Dictionary<int, decimal> { { 1, 0m } }, 0.21m);
        Comprobar(limite.Detalles.Count == 100 && limite.Total == 0m, "Admite límites válidos y productos de precio cero");
    }

    private static void ProbarServicio()
    {
        var caso = new Caso();
        int id = caso.Servicio.Crear(caso.Request());
        Comprobar(id == 37 && caso.Repo.Guardada.Total == 666.71m, "Servicio usa precios vigentes de su contrato de productos");
        Comprobar(string.Join(",", caso.Pasos) == "iniciar,cliente,productos,guardar,confirmar,liberar", "Validación y guardado ocurren dentro de una transacción, confirmada al final");
        caso = new Caso();
        Rechaza(() => caso.Servicio.Crear(null), "Servicio rechaza solicitud nula");
        Comprobar(caso.Pasos.Count == 0, "Solicitud nula no abre transacción");
        caso = new Caso();
        var invalida = caso.Request(); invalida.Detalles[0].Cantidad = 0;
        Rechaza(() => caso.Servicio.Crear(invalida), "Servicio valida cantidades antes de persistir");
        Comprobar(caso.Pasos.Count == 0, "Solicitud inválida no consulta ni guarda");
        caso = new Caso(); caso.Clientes.Activo = false;
        Rechaza(() => caso.Servicio.Crear(caso.Request()), "Servicio rechaza cliente inactivo");
        Comprobar(string.Join(",", caso.Pasos) == "iniciar,cliente,liberar", "Cliente inválido libera sin guardar ni confirmar");
        caso = new Caso(); caso.Productos.Valores.RemoveAt(1);
        Rechaza(() => caso.Servicio.Crear(caso.Request()), "Servicio rechaza producto inexistente");
        Comprobar(caso.Repo.Guardada == null && caso.Pasos.Last() == "liberar" && !caso.Pasos.Contains("confirmar"), "Producto inexistente no confirma la operación");
        caso = new Caso(); caso.Productos.Valores[0].Activo = false;
        Rechaza(() => caso.Servicio.Crear(caso.Request()), "Servicio rechaza producto inactivo");
        caso = new Caso(); caso.Repo.FallarGuardado = true;
        bool fallo = false;
        try { caso.Servicio.Crear(caso.Request()); } catch (InvalidOperationException) { fallo = true; }
        Comprobar(fallo && !caso.Pasos.Contains("confirmar") && caso.Pasos.Last() == "liberar", "Error de persistencia propaga y dispone la transacción sin confirmar");
        caso = new Caso(); caso.Repo.HayFactura = false;
        Comprobar(caso.Servicio.Emitir(999) == ResultadoCambioEstado.NoExiste && caso.Servicio.Anular(999) == ResultadoCambioEstado.NoExiste, "Servicio distingue factura inexistente");
        caso = new Caso(); caso.Repo.PermitirCambio = false;
        Comprobar(caso.Servicio.Emitir(1) == ResultadoCambioEstado.Conflicto && caso.Servicio.Anular(1) == ResultadoCambioEstado.Conflicto, "Servicio distingue conflicto de estado");
        caso.Repo.PermitirCambio = true;
        Comprobar(caso.Servicio.Emitir(1) == ResultadoCambioEstado.Actualizado && caso.Servicio.Anular(1) == ResultadoCambioEstado.Actualizado, "Servicio informa cambio aplicado");
        Rechaza(() => caso.Servicio.Consultar(null, "INVALIDO"), "Servicio valida filtro de estado");
        int lecturasTasa = 0;
        var servicio = new ServicioFacturacion(caso.Repo, caso.Clientes, caso.Productos, () => { lecturasTasa++; return 0.21m; });
        servicio.Consultar(null, null); servicio.Obtener(1); servicio.Reportes();
        Comprobar(lecturasTasa == 0, "Las consultas no necesitan leer la tasa configurada");
        servicio.Crear(caso.Request());
        Comprobar(lecturasTasa == 1, "La creación usa una sola lectura de tasa para toda la factura");
    }

    private static void ProbarController()
    {
        var caso = new Caso();
        using (var controller = new FacturasController(caso.Servicio))
        {
            var modelo = new FacturaCrearModel { ClienteId = 5, Detalles = Lineas() };
            var resultado = controller.Create(modelo) as RedirectToRouteResult;
            Comprobar(resultado != null && (string)resultado.RouteValues["action"] == "Details" && (int)resultado.RouteValues["id"] == 37, "Controller sustituible crea y redirige sin DbContext");
            var detalle = controller.Details(1) as ViewResult;
            Comprobar(detalle != null && detalle.Model is FacturaDetalleDto, "Detalle entrega DTO a la vista");
            var lista = controller.Index(null, null) as ViewResult;
            Comprobar(lista != null && lista.Model is IList<FacturaResumenDto>, "Listado entrega DTOs a la vista");
            Comprobar((controller.Index(null, "INVALIDO") as HttpStatusCodeResult).StatusCode == 400, "Filtro inválido conserva HTTP 400");
            caso.Repo.HayFactura = false;
            Comprobar(controller.Emitir(999) is HttpNotFoundResult && controller.Anular(999) is HttpNotFoundResult, "Transiciones conservan HTTP 404");
            caso.Repo.HayFactura = true; caso.Repo.PermitirCambio = false;
            Comprobar((controller.Emitir(1) as HttpStatusCodeResult).StatusCode == 409 && (controller.Anular(1) as HttpStatusCodeResult).StatusCode == 409, "Transiciones conservan HTTP 409");
        }
        caso = new Caso();
        using (var controller = new FacturasController(caso.Servicio))
        {
            controller.ModelState.AddModelError("ClienteId", "Requerido");
            var resultado = controller.Create(new FacturaCrearModel { Detalles = null }) as ViewResult;
            Comprobar(resultado != null && !controller.ModelState.IsValid && caso.Repo.Guardada == null, "ModelState inválido vuelve al formulario sin guardar");
            Comprobar(((FacturaCrearModel)resultado.Model).Detalles != null, "Formulario inválido reconstruye la colección de detalles");
        }
    }

    private sealed class Caso
    {
        public readonly List<string> Pasos = new List<string>();
        public readonly RepositorioDoble Repo;
        public readonly ClientesDoble Clientes;
        public readonly ProductosDoble Productos;
        public readonly ServicioFacturacion Servicio;
        public Caso()
        {
            Repo = new RepositorioDoble(Pasos); Clientes = new ClientesDoble(Pasos); Productos = new ProductosDoble(Pasos);
            Servicio = new ServicioFacturacion(Repo, Clientes, Productos, 0.21m);
        }
        public CrearFacturaRequest Request() { return new CrearFacturaRequest { ClienteId = 5, Detalles = Lineas() }; }
    }
    private sealed class RepositorioDoble : IRepositorioFacturas
    {
        private readonly List<string> pasos;
        public FacturaEnPreparacion Guardada;
        public bool FallarGuardado, HayFactura = true, PermitirCambio = true;
        public RepositorioDoble(List<string> pasos) { this.pasos = pasos; }
        public ITransaccionFacturacion IniciarCreacion() { pasos.Add("iniciar"); return new TransaccionDoble(pasos); }
        public int Guardar(FacturaEnPreparacion factura)
        {
            pasos.Add("guardar"); if (FallarGuardado) throw new InvalidOperationException("Fallo simulado"); Guardada = factura; return 37;
        }
        public IList<FacturaResumenDto> Consultar(int? clienteId, string estado) { return new List<FacturaResumenDto>(); }
        public FacturaDetalleDto Obtener(int id) { return HayFactura ? new FacturaDetalleDto { Id = id, Detalles = new List<FacturaLineaDto>() } : null; }
        public bool Existe(int id) { return HayFactura; }
        public bool Emitir(int id) { return PermitirCambio; }
        public bool Anular(int id) { return PermitirCambio; }
        public IList<ReporteEstadoDto> ResumirPorEstado() { return new List<ReporteEstadoDto>(); }
    }
    private sealed class TransaccionDoble : ITransaccionFacturacion
    {
        private readonly List<string> pasos;
        public TransaccionDoble(List<string> pasos) { this.pasos = pasos; }
        public void Confirmar() { pasos.Add("confirmar"); }
        public void Dispose() { pasos.Add("liberar"); }
    }
    private sealed class ClientesDoble : IConsultaClientes
    {
        private readonly List<string> pasos;
        public bool Activo = true;
        public ClientesDoble(List<string> pasos) { this.pasos = pasos; }
        public bool EstaActivo(int id) { pasos.Add("cliente"); return Activo; }
        public IList<ClienteOpcionDto> Listar(bool soloActivos) { return new List<ClienteOpcionDto>(); }
    }
    private sealed class ProductosDoble : IConsultaProductos
    {
        private readonly List<string> pasos;
        public readonly List<ProductoConsultaDto> Valores = new List<ProductoConsultaDto>
        {
            new ProductoConsultaDto { Id = 1, PrecioUnitario = 125.50m, Activo = true },
            new ProductoConsultaDto { Id = 2, PrecioUnitario = 100m, Activo = true }
        };
        public ProductosDoble(List<string> pasos) { this.pasos = pasos; }
        public IList<ProductoConsultaDto> ListarActivos() { return Valores.Where(p => p.Activo).ToList(); }
        public IList<ProductoConsultaDto> Obtener(int[] ids) { pasos.Add("productos"); return Valores.Where(p => ids.Contains(p.Id)).ToList(); }
    }
}
