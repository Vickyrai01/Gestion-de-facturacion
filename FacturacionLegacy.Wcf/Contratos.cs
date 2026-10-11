using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace FacturacionLegacy.Wcf
{
    [ServiceContract(Namespace = "urn:facturacion-legacy")]
    public interface IFacturaConsultaService
    {
        [OperationContract, FaultContract(typeof(ConsultaError))]
        FacturaConsulta ObtenerFacturaPorId(int id);
        [OperationContract, FaultContract(typeof(ConsultaError))]
        List<FacturaResumen> ObtenerFacturasPorCliente(int clienteId);
        [OperationContract, FaultContract(typeof(ConsultaError))]
        List<FacturaResumen> ObtenerFacturasPorEstado(string estado);
    }
    [DataContract(Namespace = "urn:facturacion-legacy")]
    public class FacturaResumen
    {
        [DataMember] public int Id { get; set; }
        [DataMember] public string Numero { get; set; }
        [DataMember] public int ClienteId { get; set; }
        [DataMember] public DateTime FechaEmision { get; set; }
        [DataMember] public string Estado { get; set; }
        [DataMember] public decimal Subtotal { get; set; }
        [DataMember] public decimal Impuestos { get; set; }
        [DataMember] public decimal Total { get; set; }
    }
    [DataContract(Namespace = "urn:facturacion-legacy")]
    public class FacturaConsulta : FacturaResumen
    {
        [DataMember] public List<DetalleConsulta> Detalles { get; set; }
    }
    [DataContract(Namespace = "urn:facturacion-legacy")]
    public class DetalleConsulta
    {
        [DataMember] public int ProductoId { get; set; }
        [DataMember] public string Codigo { get; set; }
        [DataMember] public int Cantidad { get; set; }
        [DataMember] public decimal PrecioUnitario { get; set; }
        [DataMember] public decimal Subtotal { get; set; }
    }
    [DataContract(Namespace = "urn:facturacion-legacy")]
    public class ConsultaError
    {
        [DataMember] public string Mensaje { get; set; }
    }
}
