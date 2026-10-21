using System.ComponentModel.DataAnnotations;

namespace legacyGestionDeFacturacion.web.Models
{
    public class LoginModel
    {
        [Required, StringLength(50), Display(Name = "Usuario")]
        public string NombreUsuario { get; set; }
        [Required, StringLength(72), DataType(DataType.Password), Display(Name = "Contraseña")]
        public string Password { get; set; }
    }
}
