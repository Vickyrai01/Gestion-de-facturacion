namespace Facturacion.BuildingBlocks.Aplicacion;

/// <summary>
/// La infraestructura la lanza cuando la base rechaza una escritura por
/// concurrencia o por un índice único (por ejemplo, dos usuarios que crean el
/// mismo código de producto a la vez). El host la traduce a HTTP 409.
///
/// Reemplaza al catch de SqlException 2601/2627 que estaba en
/// ProductosController.Guardar del legacy.
/// </summary>
public sealed class ConflictoDePersistenciaException(string mensaje, Exception? causa = null)
    : Exception(mensaje, causa);

