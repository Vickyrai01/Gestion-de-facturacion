using System;
using System.Collections.Generic;
using FacturacionLegacy.Datos.Clientes;
using FacturacionLegacy.Datos.Productos;
using FacturacionLegacy.Datos.Facturacion.Dominio;

namespace FacturacionLegacy.Datos.Facturacion
{
    public class CrearFacturaRequest
    {
        public int ClienteId { get; set; }
        // Se reutiliza el contrato de línea del Módulo 2; no es una entidad EF.
        public IList<LineaFactura> Detalles { get; set; }
    }

    public class FacturaResumenDto
    {
        public int Id { get; set; }
        public string Numero { get; set; }
        public int ClienteId { get; set; }
        public string ClienteNombre { get; set; }
        public string ClienteApellido { get; set; }
        public DateTime FechaEmision { get; set; }
        public string Estado { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Impuestos { get; set; }
        public decimal Total { get; set; }
    }

    public class FacturaDetalleDto : FacturaResumenDto
    {
        public string ClienteDocumento { get; set; }
        public IList<FacturaLineaDto> Detalles { get; set; }
    }

    public class FacturaLineaDto
    {
        public int ProductoId { get; set; }
        public string ProductoCodigo { get; set; }
        public string ProductoNombre { get; set; }
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Subtotal { get; set; }
    }

    public class ReporteEstadoDto
    {
        public string Estado { get; set; }
        public int Cantidad { get; set; }
        public decimal Total { get; set; }
    }

    public enum ResultadoCambioEstado { Actualizado, NoExiste, Conflicto }

    public interface ITransaccionFacturacion : IDisposable
    {
        void Confirmar();
    }

    // Operaciones del caso de uso, sin DbSet, IQueryable ni repositorio genérico.
    public interface IRepositorioFacturas
    {
        ITransaccionFacturacion IniciarCreacion();
        int Guardar(FacturaEnPreparacion factura);
        IList<FacturaResumenDto> Consultar(int? clienteId, string estado);
        FacturaDetalleDto Obtener(int id);
        int ContarPendientes();
        bool Existe(int id);
        bool Emitir(int id);
        bool Anular(int id);
        IList<ReporteEstadoDto> ResumirPorEstado();
    }

    public interface IServicioFacturacion
    {
        decimal TasaImpuestos { get; }
        int Crear(CrearFacturaRequest request);
        IList<FacturaResumenDto> Consultar(int? clienteId, string estado);
        FacturaDetalleDto Obtener(int id);
        ResultadoCambioEstado Emitir(int id);
        ResultadoCambioEstado Anular(int id);
        IList<ReporteEstadoDto> Reportes();
        IList<ClienteOpcionDto> Clientes(bool soloActivos);
        IList<ProductoConsultaDto> ProductosActivos();
    }
}
