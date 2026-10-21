using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FacturacionLegacy.Datos.Productos
{
    public class ProductoConsultaDto
    {
        public int Id { get; set; }
        public string Codigo { get; set; }
        public string Nombre { get; set; }
        public decimal PrecioUnitario { get; set; }
        public bool Activo { get; set; }
    }

    public interface IConsultaProductos
    {
        IList<ProductoConsultaDto> ListarActivos();
        IList<ProductoConsultaDto> Obtener(int[] ids);
        // Lecturas de la demo del Módulo 4: incluyen activos e inactivos.
        Task<List<ProductoConsultaDto>> ListarAsync(CancellationToken cancellationToken);
        Task<ProductoConsultaDto> ObtenerAsync(int id, CancellationToken cancellationToken);
    }
}
