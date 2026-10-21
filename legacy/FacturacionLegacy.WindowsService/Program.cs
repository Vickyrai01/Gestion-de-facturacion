using System;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;

namespace FacturacionLegacy.WindowsService
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            if (args.Length >= 1 && args[0] == "--productos-async")
                // Único puente síncrono: Main legacy. No copiar a un controller MVC.
                return MostrarProductosAsync(args).GetAwaiter().GetResult();
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
                Console.WriteLine("Usar --productos-async [id], --una-vez o --consola para desarrollo; sin argumentos se ejecuta desde el administrador de servicios de Windows.");
                return 1;
            }
            ServiceBase.Run(new FacturacionService());
            return 0;
        }
        private static async Task<int> MostrarProductosAsync(string[] args)
        {
            int id = 0;
            if (args.Length > 2 || (args.Length == 2 && (!int.TryParse(args[1], out id) || id <= 0)))
            {
                Console.Error.WriteLine("Uso: --productos-async [id positivo]");
                return 1;
            }
            using (var cancelar = new CancellationTokenSource())
            {
                ConsoleCancelEventHandler manejador = (sender, e) => { e.Cancel = true; cancelar.Cancel(); };
                Console.CancelKeyPress += manejador;
                try
                {
                    var consultas = new ConsultasProductosAsync();
                    if (args.Length == 2)
                    {
                        var producto = await consultas.ObtenerAsync(id, cancelar.Token);
                        if (producto == null) { Console.WriteLine("Producto inexistente."); return 1; }
                        Console.WriteLine("Producto {0}: {1}, precio {2:N2}", producto.Id, producto.Codigo, producto.PrecioUnitario);
                    }
                    else
                    {
                        var productos = await consultas.ListarAsync(cancelar.Token);
                        foreach (var producto in productos)
                            Console.WriteLine("Producto {0}: {1}, precio {2:N2}", producto.Id, producto.Codigo, producto.PrecioUnitario);
                        Console.WriteLine("Productos consultados: {0} (máximo 20).", productos.Count);
                    }
                    return 0;
                }
                catch (OperationCanceledException) { Console.WriteLine("Consulta cancelada."); return 2; }
                catch (Exception error) { Console.Error.WriteLine("Consulta fallida: " + error.GetType().Name); return 1; }
                finally { Console.CancelKeyPress -= manejador; }
            }
        }
    }
}
