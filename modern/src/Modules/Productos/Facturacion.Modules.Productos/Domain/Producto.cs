using Facturacion.BuildingBlocks.Dominio;
using Facturacion.BuildingBlocks.Resultados;

namespace Facturacion.Modules.Productos.Domain;

/// <summary>
/// Entidad de dominio rica para Producto.
///
/// Encapsula las reglas que en el legacy estaban repartidas entre DataAnnotations
/// (`[Range(typeof(decimal), "0", "9999999999")]`) y `ProductosController`
/// (`decimal.Round(producto.PrecioUnitario, 2, MidpointRounding.AwayFromZero)`).
/// </summary>
public sealed class Producto : Entidad
{
    public const decimal PrecioMaximo = 9_999_999_999m;

    private Producto() { }

    private Producto(string codigo, string nombre, string? descripcion, decimal precioUnitario, int stock)
    {
        Codigo = codigo;
        Nombre = nombre;
        Descripcion = descripcion;
        PrecioUnitario = precioUnitario;
        Stock = stock;
        Activo = true;
    }

    public string Codigo { get; private set; } = string.Empty;
    public string Nombre { get; private set; } = string.Empty;
    public string? Descripcion { get; private set; }
    public decimal PrecioUnitario { get; private set; }
    public int Stock { get; private set; }
    public bool Activo { get; private set; }

    public static Result<Producto> Crear(
        string? codigo,
        string? nombre,
        string? descripcion,
        decimal precioUnitario,
        int stock)
    {
        var datos = ValidarDatos(codigo, nombre, descripcion, precioUnitario, stock);
        if (datos.EsFallo)
            return datos.Error;

        var (cod, nom, desc, precio, stk) = datos.Valor;
        return new Producto(cod.Valor, nom, desc, precio, stk);
    }

    public Result Actualizar(
        string? codigo,
        string? nombre,
        string? descripcion,
        decimal precioUnitario,
        int stock)
    {
        var datos = ValidarDatos(codigo, nombre, descripcion, precioUnitario, stock);
        if (datos.EsFallo)
            return datos.Error;

        var (cod, nom, desc, precio, stk) = datos.Valor;
        Codigo = cod.Valor;
        Nombre = nom;
        Descripcion = desc;
        PrecioUnitario = precio;
        Stock = stk;
        return Result.Exito();
    }

    public void Desactivar() => Activo = false;

    public void Activar() => Activo = true;

    private static Result<(CodigoProducto Codigo, string Nombre, string? Descripcion, decimal Precio, int Stock)> ValidarDatos(
        string? codigo,
        string? nombre,
        string? descripcion,
        decimal precioUnitario,
        int stock)
    {
        var codigoValido = CodigoProducto.Crear(codigo);
        if (codigoValido.EsFallo)
            return codigoValido.Error;

        var nombreValido = Validar.TextoRequerido(
            nombre, 100, ErroresProducto.NombreObligatorio, ErroresProducto.NombreMuyLargo);
        if (nombreValido.EsFallo)
            return nombreValido.Error;

        var descripcionValida = Validar.TextoOpcional(
            descripcion, 500, ErroresProducto.DescripcionMuyLarga);
        if (descripcionValida.EsFallo)
            return descripcionValida.Error;

        if (precioUnitario < 0m || precioUnitario > PrecioMaximo)
            return ErroresProducto.PrecioFueraDeRango;

        if (stock < 0)
            return ErroresProducto.StockNegativo;

        return (codigoValido.Valor, nombreValido.Valor, descripcionValida.Valor, Dinero.Redondear(precioUnitario), stock);
    }
}

