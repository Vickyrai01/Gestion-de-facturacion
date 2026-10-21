using FacturacionLegacy.Datos;

namespace FacturacionLegacy.WindowsService
{
    // Contrato didáctico local: reconciliar con las interfaces del Módulo 3.
    public interface IConsultaPendientes
    {
        int Contar();
    }

    public sealed class ConsultaPendientesEf : IConsultaPendientes
    {
        public int Contar()
        {
            using (var db = new FacturacionDbContext())
            {
                db.Database.CommandTimeout = 30;
                return FacturacionOperaciones.ContarPendientes(db);
            }
        }
    }
}
