using Facturacion.BuildingBlocks.Resultados;

namespace Facturacion.Modules.Clientes.Domain;

/// <summary>
/// Catálogo de errores de negocio del módulo Clientes.
/// Conserva exactamente los mensajes del legacy para mantener compatibilidad funcional.
/// </summary>
public static class ErroresCliente
{
    public static readonly Error NombreObligatorio =
        Error.Validacion("Clientes.NombreObligatorio", "El nombre es obligatorio.");

    public static readonly Error NombreMuyLargo =
        Error.Validacion("Clientes.NombreMuyLargo", "El nombre no puede superar los 100 caracteres.");

    public static readonly Error ApellidoObligatorio =
        Error.Validacion("Clientes.ApellidoObligatorio", "El apellido es obligatorio.");

    public static readonly Error ApellidoMuyLargo =
        Error.Validacion("Clientes.ApellidoMuyLargo", "El apellido no puede superar los 100 caracteres.");

    public static readonly Error DocumentoObligatorio =
        Error.Validacion("Clientes.DocumentoObligatorio", "El documento es obligatorio.");

    public static readonly Error DocumentoMuyLargo =
        Error.Validacion("Clientes.DocumentoMuyLargo", "El documento no puede superar los 30 caracteres.");

    public static readonly Error EmailObligatorio =
        Error.Validacion("Clientes.EmailObligatorio", "El email es obligatorio.");

    public static readonly Error EmailMuyLargo =
        Error.Validacion("Clientes.EmailMuyLargo", "El email no puede superar los 254 caracteres.");

    public static readonly Error EmailInvalido =
        Error.Validacion("Clientes.EmailInvalido", "Ingresá un email válido.");

    public static Error NoEncontrado(int id) =>
        Error.NoEncontrado("Clientes.NoEncontrado", $"No se encontró el cliente con identificador {id}.");

    public static Error DocumentoDuplicado(string documento) =>
        Error.Conflicto("Clientes.DocumentoDuplicado", $"Ya existe un cliente con el documento '{documento}'.");
}

