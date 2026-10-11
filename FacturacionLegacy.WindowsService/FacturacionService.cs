using System.ServiceProcess;

namespace FacturacionLegacy.WindowsService
{
    public class FacturacionService : ServiceBase
    {
        private ResumenPendientes procesamiento;
        public FacturacionService()
        {
            ServiceName = "FacturacionLegacyResumen";
            CanStop = true;
            AutoLog = true;
        }
        protected override void OnStart(string[] args)
        {
            procesamiento = new ResumenPendientes();
            procesamiento.Iniciar();
        }
        protected override void OnStop()
        {
            RequestAdditionalTime(60000);
            if (procesamiento != null) procesamiento.Dispose();
            procesamiento = null;
        }
    }
}
