namespace Facturacion.BuildingBlocks.Aplicacion;

/// <summary>
/// Abstracción del reloj. El legacy llamaba DateTime.UtcNow dentro de
/// FacturacionOperaciones.Crear, lo que impedía testear fechas.
/// </summary>
public interface IReloj
{
    DateTime AhoraUtc { get; }
}

