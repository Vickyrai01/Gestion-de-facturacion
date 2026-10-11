using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FacturacionLegacy.Datos.Entidades
{
    public class Producto
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "El código es obligatorio.")]
        [StringLength(30)]
        [Display(Name = "Código")]
        public string Codigo { get; set; }
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(100)]
        public string Nombre { get; set; }
        [StringLength(500)]
        [Display(Name = "Descripción")]
        public string Descripcion { get; set; }
        [Range(typeof(decimal), "0", "9999999999", ErrorMessage = "El precio debe ser un importe no negativo, hasta 9999999999.")]
        [Display(Name = "Precio unitario")]
        public decimal PrecioUnitario { get; set; }
        [Range(0, int.MaxValue, ErrorMessage = "El stock no puede ser negativo.")]
        public int Stock { get; set; }
        public bool Activo { get; set; }
        public virtual ICollection<DetalleFactura> Detalles { get; set; }
    }
}
