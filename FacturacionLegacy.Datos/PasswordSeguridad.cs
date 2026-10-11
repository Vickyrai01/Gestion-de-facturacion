using System;
using System.Text;

namespace FacturacionLegacy.Datos
{
    public static class PasswordSeguridad
    {
        public static string CrearHash(string password)
        {
            if (string.IsNullOrEmpty(password) || password.Length < 12 || Encoding.UTF8.GetByteCount(password) > 72)
                throw new ArgumentException("La contraseña debe tener al menos 12 caracteres y hasta 72 bytes UTF-8.");
            return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
        }
        public static bool Verificar(string password, string hash)
        {
            if (string.IsNullOrEmpty(password) || Encoding.UTF8.GetByteCount(password) > 72 || string.IsNullOrEmpty(hash)) return false;
            try { return BCrypt.Net.BCrypt.Verify(password, hash); }
            catch (BCrypt.Net.SaltParseException) { return false; }
        }
    }
}
