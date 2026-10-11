using System.ComponentModel.DataAnnotations;

namespace FacturacionLegacy.Datos.Entidades
{
    public class Usuario
    {
        public int Id { get; set; }
        [Required, StringLength(50)]
        public string NombreUsuario { get; set; }
        [Required, StringLength(100)]
        public string PasswordHash { get; set; }
        [Required, StringLength(10)]
        public string Rol { get; set; }
        public bool Activo { get; set; }
    }
    public static class RolesUsuario
    {
        public const string Admin = "ADMIN";
        public const string Operador = "OPERADOR";
    }
}
