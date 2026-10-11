using System;
using System.Collections.Generic;
using System.Linq;

namespace legacyGestionDeFacturacion.web.Seguridad
{
    // Límite básico de demostración, compartido por cuenta en este proceso.
    internal static class IntentosLogin
    {
        private class Ventana { public DateTime Hasta; public int Cantidad; }
        private static readonly object Sincronizacion = new object();
        private static readonly Dictionary<string, Ventana> Intentos = new Dictionary<string, Ventana>();
        public static bool Admitir(string nombre)
        {
            lock (Sincronizacion)
            {
                var ahora = DateTime.UtcNow;
                foreach (string key in Intentos.Where(p => p.Value.Hasta <= ahora).Select(p => p.Key).ToList()) Intentos.Remove(key);
                Ventana ventana;
                if (!Intentos.TryGetValue(nombre, out ventana))
                {
                    if (Intentos.Count >= 10000) return false;
                    ventana = new Ventana { Hasta = ahora.AddMinutes(15) };
                    Intentos.Add(nombre, ventana);
                }
                if (ventana.Cantidad >= 5) return false;
                ventana.Cantidad++;
                return true;
            }
        }
        public static void Limpiar(string nombre) { lock (Sincronizacion) Intentos.Remove(nombre); }
    }
}
