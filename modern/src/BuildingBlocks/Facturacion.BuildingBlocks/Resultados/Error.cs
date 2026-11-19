namespace Facturacion.BuildingBlocks.Resultados;

/// <summary>
/// Clasifica el error para que la capa HTTP elija el código de estado
/// SIN que el dominio o la aplicación conozcan HTTP.
/// </summary>
public enum TipoError
{
    Validacion,
    NoEncontrado,
    Conflicto,
    NoAutorizado,
    LimiteExcedido
}

/// <summary>
/// Error esperado del negocio (no es una excepción). El código es estable y
/// sirve para tests y para clientes de la API; el mensaje es para personas.
/// </summary>
public sealed record Error(string Codigo, string Mensaje, TipoError Tipo)
{
    public static Error Validacion(string codigo, string mensaje) => new(codigo, mensaje, TipoError.Validacion);

    public static Error NoEncontrado(string codigo, string mensaje) => new(codigo, mensaje, TipoError.NoEncontrado);

    public static Error Conflicto(string codigo, string mensaje) => new(codigo, mensaje, TipoError.Conflicto);

    public static Error NoAutorizado(string codigo, string mensaje) => new(codigo, mensaje, TipoError.NoAutorizado);

    public static Error LimiteExcedido(string codigo, string mensaje) => new(codigo, mensaje, TipoError.LimiteExcedido);
}

