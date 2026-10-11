using System;
using System.Configuration;
using System.IO;
using System.Threading;
using FacturacionLegacy.Datos;

namespace FacturacionLegacy.WindowsService
{
    public sealed class ResumenPendientes : IDisposable
    {
        private Timer timer;
        private int ejecutando;
        private readonly object logLock = new object();
        public bool EjecutarUnaVez()
        {
            if (Interlocked.CompareExchange(ref ejecutando, 1, 0) != 0) return true;
            try
            {
                using (var db = new FacturacionDbContext())
                {
                    db.Database.CommandTimeout = 30;
                    int cantidad = FacturacionOperaciones.ContarPendientes(db);
                    Escribir("Facturas pendientes: " + cantidad);
                }
                return true;
            }
            catch (Exception error)
            {
                try { Escribir("Error de procesamiento: " + error.GetType().Name); }
                catch (Exception logError) { Console.Error.WriteLine("Error de registro: " + logError.GetType().Name); }
                return false;
            }
            finally { Interlocked.Exchange(ref ejecutando, 0); }
        }
        public void Iniciar()
        {
            int segundos;
            if (!int.TryParse(ConfigurationManager.AppSettings["IntervaloSegundos"], out segundos) || segundos < 5 || segundos > 86400)
                throw new ConfigurationErrorsException("IntervaloSegundos debe estar entre 5 y 86400.");
            timer = new Timer(_ => EjecutarUnaVez(), null, TimeSpan.Zero, TimeSpan.FromSeconds(segundos));
        }
        public void Dispose()
        {
            if (timer == null) return;
            using (var detenido = new ManualResetEvent(false))
            {
                // Dispose espera a que terminen los callbacks: no quedan tareas huérfanas.
                if (timer.Dispose(detenido)) detenido.WaitOne();
            }
            timer = null;
        }
        private void Escribir(string mensaje)
        {
            string directorio = Environment.ExpandEnvironmentVariables(ConfigurationManager.AppSettings["LogDirectory"] ?? "logs");
            if (!Path.IsPathRooted(directorio)) directorio = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, directorio);
            lock (logLock)
            {
                Directory.CreateDirectory(directorio);
                string linea = DateTimeOffset.Now.ToString("O") + " " + mensaje;
                File.AppendAllText(Path.Combine(directorio, DateTime.Now.ToString("yyyyMMdd") + ".log"), linea + Environment.NewLine);
                if (Environment.UserInteractive) Console.WriteLine(linea);
            }
        }
    }
}
