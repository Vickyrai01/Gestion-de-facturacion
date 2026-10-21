using System;
using System.Configuration;
using System.Threading;
using Serilog;

namespace FacturacionLegacy.WindowsService
{
    public sealed class ResumenPendientes : IDisposable
    {
        private readonly Func<int> contarPendientes;
        private readonly ILogger log;
        private readonly IDisposable registroPropio;
        private readonly object ciclo = new object();
        private Timer timer;
        private bool ejecutando;
        private bool detenido;

        // Composición manual compatible con MVC 5 / .NET Framework, sin contenedor global.
        public ResumenPendientes() : this(ComposicionProcesos.ContarPendientes, RegistroProcesamiento.Crear())
        {
            registroPropio = (IDisposable)log;
        }

        // Quien inyecta el logger conserva su propiedad y debe disponerlo.
        public ResumenPendientes(Func<int> contarPendientes, ILogger log)
        {
            if (contarPendientes == null) throw new ArgumentNullException("contarPendientes");
            if (log == null) throw new ArgumentNullException("log");
            this.contarPendientes = contarPendientes;
            this.log = log;
        }

        public bool EjecutarUnaVez()
        {
            lock (ciclo)
            {
                if (detenido) return false;
                if (ejecutando) return true; // Tick omitido: nunca se superponen consultas.
                ejecutando = true;
            }
            var inicio = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                log.Information("Inicio de procesamiento de facturas pendientes");
                int cantidad = contarPendientes();
                log.Information("Se encontraron {CantidadFacturas} facturas pendientes", cantidad);
                if (Environment.UserInteractive) Console.WriteLine("Facturas pendientes: " + cantidad);
                log.Information("Procesamiento finalizado en {DuracionMs} ms", inicio.ElapsedMilliseconds);
                return true;
            }
            catch (Exception error)
            {
                if (Environment.UserInteractive) Console.Error.WriteLine("Error de procesamiento: " + error.GetType().Name);
                try { log.Error("Procesamiento fallido: {TipoError}", error.GetType().Name); }
                catch (Exception logError) { Console.Error.WriteLine("Error de registro: " + logError.GetType().Name); }
                return false;
            }
            finally
            {
                lock (ciclo) { ejecutando = false; Monitor.PulseAll(ciclo); }
            }
        }

        public void Iniciar()
        {
            int segundos;
            if (!int.TryParse(ConfigurationManager.AppSettings["IntervaloSegundos"], out segundos) || segundos < 5 || segundos > 86400)
                throw new ConfigurationErrorsException("IntervaloSegundos debe estar entre 5 y 86400.");
            lock (ciclo)
            {
                if (detenido) throw new ObjectDisposedException("ResumenPendientes");
                if (timer != null) throw new InvalidOperationException("El procesamiento ya está iniciado.");
                timer = new Timer(_ => EjecutarUnaVez(), null, TimeSpan.Zero, TimeSpan.FromSeconds(segundos));
            }
        }

        public void Dispose()
        {
            lock (ciclo)
            {
                if (detenido) return;
                detenido = true;
                if (timer != null) timer.Dispose();
                timer = null;
                // Los callbacks encolados ven detenido=true; el activo termina antes del cierre del log.
                while (ejecutando) Monitor.Wait(ciclo);
                if (registroPropio != null) registroPropio.Dispose();
            }
        }
    }
}
