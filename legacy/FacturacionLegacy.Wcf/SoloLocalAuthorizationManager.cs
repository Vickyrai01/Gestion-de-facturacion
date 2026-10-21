using System.Net;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace FacturacionLegacy.Wcf
{
    // SOAP anónimo se limita al equipo local para las demostraciones.
    public class SoloLocalAuthorizationManager : ServiceAuthorizationManager
    {
        protected override bool CheckAccessCore(OperationContext context)
        {
            if (!context.IncomingMessageProperties.ContainsKey(RemoteEndpointMessageProperty.Name)) return false;
            var endpoint = (RemoteEndpointMessageProperty)context.IncomingMessageProperties[RemoteEndpointMessageProperty.Name];
            IPAddress address;
            return IPAddress.TryParse(endpoint.Address, out address) && IPAddress.IsLoopback(address);
        }
    }
}
