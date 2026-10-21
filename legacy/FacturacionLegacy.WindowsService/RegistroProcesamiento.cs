using System;
using System.Configuration;
using System.IO;
using Serilog;
using Serilog.Core;
using Serilog.Formatting.Json;

namespace FacturacionLegacy.WindowsService
{
    internal static class RegistroProcesamiento
    {
        public static Logger Crear()
        {
            string directorio = Environment.ExpandEnvironmentVariables(ConfigurationManager.AppSettings["LogDirectory"] ?? "logs");
            if (!Path.IsPathRooted(directorio)) directorio = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, directorio);
            Directory.CreateDirectory(directorio);
            // Sin mensajes de excepción ni entidades: solo propiedades técnicas y agregados.
            return new LoggerConfiguration()
                .WriteTo.File(new JsonFormatter(), Path.Combine(directorio, "pendientes-.json"),
                    rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7)
                // Conserva la línea consumida por ValidarModulo2.ps1.
                .WriteTo.Logger(registro => registro
                    .Filter.ByIncludingOnly(e => e.Properties.ContainsKey("CantidadFacturas"))
                    .WriteTo.File(Path.Combine(directorio, "resumen-.log"),
                        rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7,
                        outputTemplate: "{Timestamp:O} Facturas pendientes: {CantidadFacturas}{NewLine}"))
                .CreateLogger();
        }
    }
}
