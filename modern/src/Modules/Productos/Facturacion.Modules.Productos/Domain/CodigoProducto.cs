using Facturacion.BuildingBlocks.Dominio;
using Facturacion.BuildingBlocks.Resultados;

namespace Facturacion.Modules.Productos.Domain;

/// <summary>
/// Value Object para el código de producto.
///
/// En el legacy la normalización `(producto.Codigo ?? "").Trim().ToUpperInvariant()`
/// estaba dentro de `ProductosController.ValidarCodigo` (lógica de negocio en el controller).
/// Al moverla a un Value Object de dominio, el código siempre nace normalizado.
/// </summary>
public sealed record CodigoProducto
{
    public const int LongitudMaxima = 30;

    public string Valor { get; }

    private CodigoProducto(string valor) => Valor = valor;

    public static Result<CodigoProducto> Crear(string? codigo)
    {
        var texto = Validar.TextoRequerido(
            codigo,
            LongitudMaxima,
            ErroresProducto.CodigoObligatorio,
            ErroresProducto.CodigoMuyLargo);

        return texto.EsFallo
            ? texto.Error
            : new CodigoProducto(texto.Valor.ToUpperInvariant());
    }

    public override string ToString() => Valor;
}

