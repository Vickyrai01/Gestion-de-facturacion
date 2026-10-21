using System;
using System.Collections.Generic;
using System.Linq;
using FacturacionLegacy.Datos.Clientes;
using FacturacionLegacy.Datos.Productos;
using FacturacionLegacy.Datos.Entidades;
using FacturacionLegacy.Datos.Facturacion.Dominio;

namespace FacturacionLegacy.Datos.Facturacion.Aplicacion
{
    public class ServicioFacturacion : IServicioFacturacion
    {
        private readonly IRepositorioFacturas facturas;
        private readonly IConsultaClientes clientes;
        private readonly IConsultaProductos productos;
        private readonly Func<decimal> obtenerTasa;
        public decimal TasaImpuestos { get { return obtenerTasa(); } }

        public ServicioFacturacion(IRepositorioFacturas facturas, IConsultaClientes clientes, IConsultaProductos productos, decimal tasaImpuestos)
            : this(facturas, clientes, productos, () => tasaImpuestos) { }

        public ServicioFacturacion(IRepositorioFacturas facturas, IConsultaClientes clientes, IConsultaProductos productos, Func<decimal> obtenerTasa)
        {
            if (facturas == null) throw new ArgumentNullException("facturas");
            if (clientes == null) throw new ArgumentNullException("clientes");
            if (productos == null) throw new ArgumentNullException("productos");
            if (obtenerTasa == null) throw new ArgumentNullException("obtenerTasa");
            this.facturas = facturas;
            this.clientes = clientes;
            this.productos = productos;
            this.obtenerTasa = obtenerTasa;
        }

        public int Crear(CrearFacturaRequest request)
        {
            if (request == null) throw new ArgumentException("La factura debe contener entre 1 y 100 detalles.");
            decimal tasa = TasaImpuestos;
            FacturaEnPreparacion.ValidarSolicitud(request.Detalles, tasa);
            using (var transaccion = facturas.IniciarCreacion())
            {
                if (!clientes.EstaActivo(request.ClienteId)) throw new ArgumentException("El cliente no existe o está inactivo.");
                int[] ids = request.Detalles.Select(l => l.ProductoId).Distinct().ToArray();
                var vigentes = productos.Obtener(ids);
                if (vigentes.Count != ids.Length || vigentes.Any(p => !p.Activo))
                    throw new ArgumentException("Uno de los productos no existe o está inactivo.");
                var precios = vigentes.ToDictionary(p => p.Id, p => p.PrecioUnitario);
                var factura = FacturaEnPreparacion.Crear(request.ClienteId, request.Detalles, precios, tasa);
                int id = facturas.Guardar(factura);
                transaccion.Confirmar();
                return id;
            }
        }
        public IList<FacturaResumenDto> Consultar(int? clienteId, string estado)
        {
            if (!string.IsNullOrEmpty(estado) && !EstadosFactura.Todos.Contains(estado)) throw new ArgumentException("Estado inválido");
            return facturas.Consultar(clienteId, estado);
        }
        public FacturaDetalleDto Obtener(int id) { return facturas.Obtener(id); }
        public ResultadoCambioEstado Emitir(int id)
        {
            if (!facturas.Existe(id)) return ResultadoCambioEstado.NoExiste;
            return facturas.Emitir(id) ? ResultadoCambioEstado.Actualizado : ResultadoCambioEstado.Conflicto;
        }
        public ResultadoCambioEstado Anular(int id)
        {
            if (!facturas.Existe(id)) return ResultadoCambioEstado.NoExiste;
            return facturas.Anular(id) ? ResultadoCambioEstado.Actualizado : ResultadoCambioEstado.Conflicto;
        }
        public IList<ReporteEstadoDto> Reportes() { return facturas.ResumirPorEstado(); }
        public IList<ClienteOpcionDto> Clientes(bool soloActivos) { return clientes.Listar(soloActivos); }
        public IList<ProductoConsultaDto> ProductosActivos() { return productos.ListarActivos(); }
    }
}
