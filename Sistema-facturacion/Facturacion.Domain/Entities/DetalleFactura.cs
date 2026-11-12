namespace Facturacion.Domain.Entities;

public sealed class Detallesfactura
{
    private Detallesfactura()
    {
    }

    private Detallesfactura(int productoid, int cantidad, decimal preciounitario)
    {
        if (productoid <= 0)
            throw new ArgumentOutOfRangeException(nameof(productoid), "El producto debe ser válido.");

        Productoid = productoid;
        Preciounitario = ValidarPrecio(preciounitario);
        Cantidad = ValidarCantidad(cantidad);
        RecalcularSubtotal();
    }

    public int Id { get; private set; }
    public int Facturaid { get; private set; }
    public int Productoid { get; private set; }
    public int Cantidad { get; private set; }
    public decimal Preciounitario { get; private set; }
    public decimal Subtotal { get; private set; }

    public static Detallesfactura Crear(int productoid, int cantidad, decimal preciounitario)
        => new(productoid, cantidad, preciounitario);

    public void CambiarCantidad(int cantidad)
    {
        Cantidad = ValidarCantidad(cantidad);
        RecalcularSubtotal();
    }

    internal void AsignarFactura(int facturaid)
    {
        if (facturaid < 0)
            throw new ArgumentOutOfRangeException(nameof(facturaid), "El identificador de factura no puede ser negativo.");

        Facturaid = facturaid;
    }

    private void RecalcularSubtotal()
        => Subtotal = decimal.Round(Cantidad * Preciounitario, 2, MidpointRounding.AwayFromZero);

    private static int ValidarCantidad(int cantidad)
    {
        if (cantidad <= 0)
            throw new ArgumentOutOfRangeException(nameof(cantidad), "La cantidad debe ser mayor que cero.");

        return cantidad;
    }

    private static decimal ValidarPrecio(decimal precio)
    {
        if (precio < 0)
            throw new ArgumentOutOfRangeException(nameof(precio), "El precio no puede ser negativo.");

        return decimal.Round(precio, 2, MidpointRounding.AwayFromZero);
    }
}
