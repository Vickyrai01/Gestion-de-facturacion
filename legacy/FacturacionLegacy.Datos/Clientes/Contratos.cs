using System.Collections.Generic;

namespace FacturacionLegacy.Datos.Clientes
{
    public class ClienteOpcionDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
    }

    public interface IConsultaClientes
    {
        bool EstaActivo(int id);
        IList<ClienteOpcionDto> Listar(bool soloActivos);
    }
}
