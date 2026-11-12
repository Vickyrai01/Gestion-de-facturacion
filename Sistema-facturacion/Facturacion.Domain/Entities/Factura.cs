namespace Facturacion.Domain.Entities;

public sealed class Factura
{
    public const string EstadoPendiente = "PENDIENTE";
    public const string EstadoEmitida = "EMITIDA";
    public const string EstadoPagada = "PAGADA";
    public const string EstadoAnulada = "ANULADA";

    private readonly List<Detallesfactura> detallesfacturas = [];

    private Factura()
    {
    }

    private Factura(string numero, int clienteid, DateTime fechaemision, decimal impuestos)
    {
        Id = 0;
        Numero = ValidarTexto(numero, nameof(numero), 30);
        if (clienteid <= 0)
            throw new ArgumentOutOfRangeException(nameof(clienteid), "El cliente debe ser válido.");

        Clienteid = clienteid;
        Fechaemision = fechaemision;
        Estado = EstadoPendiente;
        Impuestos = ValidarImporte(impuestos, nameof(impuestos));
        RecalcularTotales();
    }

    public int Id { get; private set; }
    public string Numero { get; private set; } = null!;
    public int Clienteid { get; private set; }
    public DateTime Fechaemision { get; private set; }
    public string Estado { get; private set; } = null!;
    public decimal Subtotal { get; private set; }
    public decimal Impuestos { get; private set; }
    public decimal Total { get; private set; }
    public IReadOnlyCollection<Detallesfactura> Detallesfacturas => detallesfacturas.AsReadOnly();

    public static Factura Crear(string numero, int clienteid, DateTime fechaemision, decimal impuestos = 0)
        => new(numero, clienteid, fechaemision, impuestos);

    public void AgregarDetalle(Producto producto, int cantidad)
    {
        ArgumentNullException.ThrowIfNull(producto);
        VerificarFacturaModificable();
        if (!producto.Activo)
            throw new InvalidOperationException("No se puede facturar un producto inactivo.");
        if (detallesfacturas.Any(detalle => detalle.Productoid == producto.Id))
            throw new InvalidOperationException("El producto ya existe en la factura.");

        producto.ReducirStock(cantidad);
        var detalle = Detallesfactura.Crear(producto.Id, cantidad, producto.Preciounitario);
        detalle.AsignarFactura(Id);
        detallesfacturas.Add(detalle);
        RecalcularTotales();
    }

    public void CambiarImpuestos(decimal impuestos)
    {
        VerificarFacturaModificable();
        Impuestos = ValidarImporte(impuestos, nameof(impuestos));
        RecalcularTotales();
    }

    public void Emitir()
    {
        VerificarFacturaModificable();
        if (detallesfacturas.Count == 0)
            throw new InvalidOperationException("No se puede emitir una factura sin detalles.");

        Estado = EstadoEmitida;
    }

    public void MarcarComoPagada()
    {
        if (Estado != EstadoEmitida)
            throw new InvalidOperationException("Solo se puede pagar una factura emitida.");

        Estado = EstadoPagada;
    }

    public void Anular()
    {
        if (Estado == EstadoPagada)
            throw new InvalidOperationException("No se puede anular una factura pagada.");
        if (Estado == EstadoAnulada)
            return;

        Estado = EstadoAnulada;
    }

    private void VerificarFacturaModificable()
    {
        if (Estado != EstadoPendiente)
            throw new InvalidOperationException("Solo se puede modificar una factura pendiente.");
    }

    private void RecalcularTotales()
    {
        Subtotal = decimal.Round(detallesfacturas.Sum(detalle => detalle.Subtotal), 2, MidpointRounding.AwayFromZero);
        Total = decimal.Round(Subtotal + Impuestos, 2, MidpointRounding.AwayFromZero);
    }

    private static decimal ValidarImporte(decimal importe, string nombre)
    {
        if (importe < 0)
            throw new ArgumentOutOfRangeException(nombre, "El importe no puede ser negativo.");

        return decimal.Round(importe, 2, MidpointRounding.AwayFromZero);
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
