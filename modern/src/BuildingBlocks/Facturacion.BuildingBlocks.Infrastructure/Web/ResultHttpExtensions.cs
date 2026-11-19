using Facturacion.BuildingBlocks.Resultados;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Facturacion.BuildingBlocks.Infrastructure.Web;

/// <summary>
/// Traduce un <see cref="Result"/> del caso de uso a <see cref="ActionResult"/> o <see cref="IResult"/>.
///
/// Esto elimina los bloques try/catch de cada acción de controller que vimos en las Clases 5 y 6,
/// y evita que los controllers tomen decisiones de negocio.
/// </summary>
public static class ResultHttpExtensions
{
    public static ActionResult ToProblemActionResult(this ControllerBase controller, Error error)
    {
        var statusCode = ObtenerStatusCode(error.Tipo);
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = error.Mensaje,
            Detail = error.Mensaje,
            Extensions = { ["codigo"] = error.Codigo }
        };

        return new ObjectResult(problem) { StatusCode = statusCode };
    }

    public static ActionResult<T> ToActionResult<T>(this Result<T> result, ControllerBase controller)
    {
        return result.EsExito
            ? controller.Ok(result.Valor)
            : controller.ToProblemActionResult(result.Error);
    }

    public static ActionResult ToNoContentResult(this Result result, ControllerBase controller)
    {
        return result.EsExito
            ? controller.NoContent()
            : controller.ToProblemActionResult(result.Error);
    }

    public static int ObtenerStatusCode(TipoError tipo) => tipo switch
    {
        TipoError.Validacion => StatusCodes.Status400BadRequest,
        TipoError.NoEncontrado => StatusCodes.Status404NotFound,
        TipoError.Conflicto => StatusCodes.Status409Conflict,
        TipoError.NoAutorizado => StatusCodes.Status401Unauthorized,
        TipoError.LimiteExcedido => StatusCodes.Status429TooManyRequests,
        _ => StatusCodes.Status400BadRequest
    };
}

