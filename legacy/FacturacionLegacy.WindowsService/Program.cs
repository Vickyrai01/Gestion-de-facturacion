using System;
using System.ServiceProcess;
using System.Threading;

namespace FacturacionLegacy.WindowsService
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            if (args.Length == 1 && args[0] == "--una-vez")
                using (var proceso = new ResumenPendientes()) return proceso.EjecutarUnaVez() ? 0 : 1;
            if (args.Length >= 1 && args[0] == "--consola")
            {
                int segundos = 0;
                if (args.Length != 1 && (args.Length != 3 || args[1] != "--segundos" || !int.TryParse(args[2], out segundos) || segundos < 1 || segundos > 3600))
                {
                    Console.Error.WriteLine("Uso: --consola [--segundos 1..3600]");
                    return 1;
                }
                using (var detener = new ManualResetEvent(false))
                using (var proceso = new ResumenPendientes())
                {
                    Console.CancelKeyPress += (sender, e) => { e.Cancel = true; detener.Set(); };
                    proceso.Iniciar();
                    Console.WriteLine("Procesamiento periódico iniciado. Ctrl+C para detener.");
                    if (segundos == 0) detener.WaitOne();
                    else detener.WaitOne(TimeSpan.FromSeconds(segundos));
                }
                Console.WriteLine("Procesamiento detenido.");
                return 0;
            }
            if (Environment.UserInteractive)
            {
                Console.WriteLine("Usar --una-vez o --consola para desarrollo; sin argumentos se ejecuta desde el administrador de servicios de Windows.");
                return 1;
            }
            ServiceBase.Run(new FacturacionService());
            return 0;
        }
    }
}
