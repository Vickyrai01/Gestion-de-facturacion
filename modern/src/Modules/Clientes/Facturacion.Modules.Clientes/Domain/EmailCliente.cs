using Facturacion.BuildingBlocks.Dominio;
using Facturacion.BuildingBlocks.Resultados;

namespace Facturacion.Modules.Clientes.Domain;

/// <summary>
/// Value Object para Email de cliente.
/// En el legacy era un string con [EmailAddress] y [StringLength(254)].
/// Al modelarlo como Value Object, ningún Cliente puede existir en memoria con un email inválido.
/// </summary>
public sealed record EmailCliente
{
    public const int LongitudMaxima = 254;

    public string Valor { get; }

    private EmailCliente(string valor) => Valor = valor;

    public static Result<EmailCliente> Crear(string? email)
    {
        var texto = Validar.TextoRequerido(
            email,
            LongitudMaxima,
            ErroresCliente.EmailObligatorio,
            ErroresCliente.EmailMuyLargo);

        if (texto.EsFallo)
            return texto.Error;

        var valor = texto.Valor;
        var indiceArroba = valor.IndexOf('@');
        if (indiceArroba <= 0
            || indiceArroba != valor.LastIndexOf('@')
            || indiceArroba == valor.Length - 1
            || !valor[(indiceArroba + 1)..].Contains('.'))
        {
            return ErroresCliente.EmailInvalido;
        }

        return new EmailCliente(valor);
    }

    public override string ToString() => Valor;
}

