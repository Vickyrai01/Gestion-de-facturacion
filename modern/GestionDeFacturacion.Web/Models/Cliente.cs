using System.ComponentModel.DataAnnotations;

namespace GestionDeFacturacion.web.Models
{
    public class Cliente
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El apellido es obligatorio.")]
        [StringLength(100, ErrorMessage = "El apellido no puede superar los 100 caracteres.")]
        public string Apellido { get; set; } = string.Empty;

        [Required(ErrorMessage = "El documento es obligatorio.")]
        [StringLength(30, ErrorMessage = "El documento no puede superar los 30 caracteres.")]
        public string Documento { get; set; } = string.Empty;

        [Required(ErrorMessage = "El email es obligatorio.")]
        [EmailAddress(ErrorMessage = "Ingresá un email válido.")]
        [StringLength(254, ErrorMessage = "El email no puede superar los 254 caracteres.")]
        public string Email { get; set; } = string.Empty;

        public bool Activo { get; set; }
    }
}
