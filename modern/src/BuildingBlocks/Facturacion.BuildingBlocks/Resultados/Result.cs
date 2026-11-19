namespace Facturacion.BuildingBlocks.Resultados;

/// <summary>
/// Resultado de una operación que puede fallar por una regla de negocio.
///
/// En el legacy los errores viajaban como ArgumentException + try/catch en el
/// controller (FacturasController.Create) o como ModelState.AddModelError.
/// Acá el caso de uso devuelve un Result y el controller sólo lo traduce a HTTP.
/// </summary>
public class Result
{
    private readonly Error? error;

    protected Result(bool esExito, Error? error)
    {
        if (esExito && error is not null)
            throw new ArgumentException("Un resultado exitoso no puede tener error.", nameof(error));
        if (!esExito && error is null)
            throw new ArgumentNullException(nameof(error), "Un resultado fallido necesita un error.");

        EsExito = esExito;
        this.error = error;
    }

    public bool EsExito { get; }

    public bool EsFallo => !EsExito;

    public Error Error => error ?? throw new InvalidOperationException("Un resultado exitoso no tiene error.");

    public static Result Exito() => new(true, null);

    public static Result Fallo(Error error) => new(false, error);

    public static Result<T> Exito<T>(T valor) => Result<T>.Exitoso(valor);

    public static Result<T> Fallo<T>(Error error) => Result<T>.Fallido(error);

    public static implicit operator Result(Error error) => Fallo(error);
}

public sealed class Result<T> : Result
{
    private readonly T? valor;

    private Result(T valor) : base(true, null) => this.valor = valor;

    private Result(Error error) : base(false, error) { }

    public T Valor => EsExito
        ? valor!
        : throw new InvalidOperationException($"No hay valor: la operación falló ({Error.Codigo}).");

    internal static Result<T> Exitoso(T valor) => new(valor);

    internal static Result<T> Fallido(Error error) => new(error);

    public static implicit operator Result<T>(T valor) => new(valor);

    public static implicit operator Result<T>(Error error) => new(error);
}

