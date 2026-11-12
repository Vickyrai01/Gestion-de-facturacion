namespace Facturacion.Domain.Entities;

public sealed class Producto
{
    private Producto()
    {
    }

    private Producto(string codigo, string nombre, string? descripcion, decimal preciounitario, int stock)
    {
        Id = 0;
        Codigo = ValidarTexto(codigo, nameof(codigo), 30);
        Nombre = ValidarTexto(nombre, nameof(nombre), 100);
        Descripcion = ValidarDescripcion(descripcion);
        Preciounitario = ValidarPrecio(preciounitario);
        Stock = ValidarStock(stock);
        Activo = true;
    }

    public int Id { get; private set; }
    public string Codigo { get; private set; } = null!;
    public string Nombre { get; private set; } = null!;
    public string? Descripcion { get; private set; }
    public decimal Preciounitario { get; private set; }
    public int Stock { get; private set; }
    public bool Activo { get; private set; }

    public static Producto Crear(
        string codigo,
        string nombre,
        decimal preciounitario,
        int stock = 0,
        string? descripcion = null)
        => new(codigo, nombre, descripcion, preciounitario, stock);

    public void ActualizarDatos(string codigo, string nombre, string? descripcion)
    {
        Codigo = ValidarTexto(codigo, nameof(codigo), 30);
        Nombre = ValidarTexto(nombre, nameof(nombre), 100);
        Descripcion = ValidarDescripcion(descripcion);
    }

    public void CambiarPrecio(decimal preciounitario)
        => Preciounitario = ValidarPrecio(preciounitario);

    public void Reponer(int cantidad)
    {
        if (cantidad <= 0)
            throw new ArgumentOutOfRangeException(nameof(cantidad), "La cantidad debe ser mayor que cero.");

        Stock = checked(Stock + cantidad);
    }

    public void ReducirStock(int cantidad)
    {
        if (cantidad <= 0)
            throw new ArgumentOutOfRangeException(nameof(cantidad), "La cantidad debe ser mayor que cero.");
        if (cantidad > Stock)
            throw new InvalidOperationException("No hay stock suficiente para la cantidad solicitada.");

        Stock -= cantidad;
    }

    public void Activar() => Activo = true;

    public void Desactivar() => Activo = false;

    private static decimal ValidarPrecio(decimal precio)
    {
        if (precio < 0)
            throw new ArgumentOutOfRangeException(nameof(precio), "El precio no puede ser negativo.");

        return decimal.Round(precio, 2, MidpointRounding.AwayFromZero);
    }

    private static int ValidarStock(int stock)
    {
        if (stock < 0)
            throw new ArgumentOutOfRangeException(nameof(stock), "El stock no puede ser negativo.");

        return stock;
    }

    private static string? ValidarDescripcion(string? descripcion)
    {
        if (string.IsNullOrWhiteSpace(descripcion))
            return null;

        descripcion = descripcion.Trim();
        if (descripcion.Length > 500)
            throw new ArgumentException("La descripción no puede superar 500 caracteres.", nameof(descripcion));

        return descripcion;
    }

    private static string ValidarTexto(string valor, string nombre, int longitudMaxima)
    {
        if (string.IsNullOrWhiteSpace(valor))
            throw new ArgumentException("El valor es obligatorio.", nombre);

        valor = valor.Trim();
        if (valor.Length > longitudMaxima)
            throw new ArgumentException($"El valor no puede superar {longitudMaxima} caracteres.", nombre);

        return valor;
    }
}
