using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using FacturacionLegacy.Datos;
using FacturacionLegacy.Datos.Entidades;

namespace FacturacionLegacy.Wcf
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerCall, IncludeExceptionDetailInFaults = false)]
    public class FacturaConsultaService : IFacturaConsultaService
    {
        public FacturaConsulta ObtenerFacturaPorId(int id)
        {
            if (id <= 0) throw Error("El identificador debe ser positivo.");
            return Consultar(() =>
            {
                using (var db = new FacturacionDbContext())
                {
                    var factura = FacturacionOperaciones.Obtener(db, id);
                    if (factura == null) throw Error("Factura inexistente.");
                    return new FacturaConsulta
                    {
                        Id = factura.Id, Numero = factura.Numero, ClienteId = factura.ClienteId,
                        FechaEmision = DateTime.SpecifyKind(factura.FechaEmision, DateTimeKind.Utc), Estado = factura.Estado,
                        Subtotal = factura.Subtotal, Impuestos = factura.Impuestos, Total = factura.Total,
                        Detalles = factura.Detalles.Select(d => new DetalleConsulta { ProductoId = d.ProductoId, Codigo = d.Producto.Codigo, Cantidad = d.Cantidad, PrecioUnitario = d.PrecioUnitario, Subtotal = d.Subtotal }).ToList()
                    };
                }
            });
        }
        public List<FacturaResumen> ObtenerFacturasPorCliente(int clienteId)
        {
            if (clienteId <= 0) throw Error("El identificador debe ser positivo.");
            return Listar(clienteId, null);
        }
        public List<FacturaResumen> ObtenerFacturasPorEstado(string estado)
        {
            if (!EstadosFactura.Todos.Contains(estado)) throw Error("Estado inválido.");
            return Listar(null, estado);
        }
        private static List<FacturaResumen> Listar(int? clienteId, string estado)
        {
            return Consultar(() =>
            {
                using (var db = new FacturacionDbContext())
                    return FacturacionOperaciones.Consultar(db, clienteId, estado).ToList().Select(f => new FacturaResumen { Id = f.Id, Numero = f.Numero, ClienteId = f.ClienteId, FechaEmision = DateTime.SpecifyKind(f.FechaEmision, DateTimeKind.Utc), Estado = f.Estado, Subtotal = f.Subtotal, Impuestos = f.Impuestos, Total = f.Total }).ToList();
            });
        }
        private static T Consultar<T>(Func<T> consulta)
        {
            try { return consulta(); }
            catch (FaultException<ConsultaError>) { throw; }
            catch (Exception) { throw Error("No se pudo realizar la consulta. Revisar el servicio y la base de datos."); }
        }
        private static FaultException<ConsultaError> Error(string mensaje)
        {
            return new FaultException<ConsultaError>(new ConsultaError { Mensaje = mensaje }, new FaultReason(mensaje));
        }
    }
}
