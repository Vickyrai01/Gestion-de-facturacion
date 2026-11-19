using Facturacion.BuildingBlocks.Dominio;
using Facturacion.BuildingBlocks.Resultados;

namespace Facturacion.Modules.Clientes.Domain;

/// <summary>
/// Entidad de dominio rica para Cliente.
///
/// Diferencias clave frente al legacy (FacturacionLegacy.Datos.Entidades.Cliente)
/// y frente al modelo scaffolded de las Clases 4-6:
/// 1. No tiene setters públicos ni DataAnnotations acoplados a MVC.
/// 2. No tiene propiedad de navegación `ICollection&lt;Factura&gt; Facturas` (eso rompía
///    la frontera entre el módulo Clientes y el módulo Facturación).
/// 3. Garantiza sus invariantes en `Crear` y `Actualizar`.
/// </summary>
public sealed class Cliente : Entidad
{
    private Cliente() { }

    private Cliente(string nombre, string apellido, string documento, string email)
    {
        Nombre = nombre;
        Apellido = apellido;
        Documento = documento;
        Email = email;
        Activo = true;
    }

    public string Nombre { get; private set; } = string.Empty;
    public string Apellido { get; private set; } = string.Empty;
    public string Documento { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public bool Activo { get; private set; }

    public static Result<Cliente> Crear(string? nombre, string? apellido, string? documento, string? email)
    {
        var datos = ValidarDatos(nombre, apellido, documento, email);
        if (datos.EsFallo)
            return datos.Error;

        var (n, a, d, e) = datos.Valor;
        return new Cliente(n, a, d, e.Valor);
    }

    public Result Actualizar(string? nombre, string? apellido, string? documento, string? email)
    {
        var datos = ValidarDatos(nombre, apellido, documento, email);
        if (datos.EsFallo)
            return datos.Error;

        var (n, a, d, e) = datos.Valor;
        Nombre = n;
        Apellido = a;
        Documento = d;
        Email = e.Valor;
        return Result.Exito();
    }

    /// <summary>
    /// Baja lógica idéntica a `ClientesController.Desactivar` del legacy.
    /// </summary>
    public void Desactivar() => Activo = false;

    public void Activar() => Activo = true;

    private static Result<(string Nombre, string Apellido, string Documento, EmailCliente Email)> ValidarDatos(
        string? nombre,
        string? apellido,
        string? documento,
        string? email)
    {
        var nombreValido = Validar.TextoRequerido(
            nombre, 100, ErroresCliente.NombreObligatorio, ErroresCliente.NombreMuyLargo);
        if (nombreValido.EsFallo)
            return nombreValido.Error;

        var apellidoValido = Validar.TextoRequerido(
            apellido, 100, ErroresCliente.ApellidoObligatorio, ErroresCliente.ApellidoMuyLargo);
        if (apellidoValido.EsFallo)
            return apellidoValido.Error;

        var documentoValido = Validar.TextoRequerido(
            documento, 30, ErroresCliente.DocumentoObligatorio, ErroresCliente.DocumentoMuyLargo);
        if (documentoValido.EsFallo)
            return documentoValido.Error;

        var emailValido = EmailCliente.Crear(email);
        if (emailValido.EsFallo)
            return emailValido.Error;

        return (nombreValido.Valor, apellidoValido.Valor, documentoValido.Valor, emailValido.Valor);
    }
}

