using System.Collections.Generic;

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
    }
}
