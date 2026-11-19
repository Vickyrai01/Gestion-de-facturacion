using System.Globalization;
using Facturacion.BuildingBlocks.Resultados;

namespace Facturacion.BuildingBlocks.Dominio;

/// <summary>
/// Validaciones reutilizables. Reemplazan a los DataAnnotations que en el legacy
/// estaban sobre las entidades EF ([Required], [StringLength], [Range]) y que
/// sólo se ejecutaban si el controller miraba ModelState.
/// </summary>
public static class Validar
{
    /// <summary>Recorta espacios y exige un texto no vacío de hasta <paramref name="maximo"/> caracteres.</summary>
    public static Result<string> TextoRequerido(string? valor, int maximo, Error siFalta, Error siExcede)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return siFalta;

        var normalizado = valor.Trim();
        return normalizado.Length > maximo ? siExcede : normalizado;
    }

    /// <summary>Texto opcional: vacío se guarda como null.</summary>
    public static Result<string?> TextoOpcional(string? valor, int maximo, Error siExcede)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return Result.Exito<string?>(null);

        var normalizado = valor.Trim();
        return normalizado.Length > maximo ? siExcede : Result.Exito<string?>(normalizado);
    }
}

/// <summary>
/// Regla monetaria del sistema legacy: 2 decimales, redondeo "away from zero".
/// Centralizarla evita que cada capa redondee distinto.
/// </summary>
public static class Dinero
{
    public static decimal Redondear(decimal importe) =>
        decimal.Round(importe, 2, MidpointRounding.AwayFromZero);

    public static string Formatear(decimal importe) =>
        importe.ToString("N2", CultureInfo.InvariantCulture);
}

