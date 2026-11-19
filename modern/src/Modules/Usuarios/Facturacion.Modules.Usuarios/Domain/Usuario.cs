using Facturacion.BuildingBlocks.Dominio;
using Facturacion.BuildingBlocks.Resultados;
using Facturacion.BuildingBlocks.Seguridad;

namespace Facturacion.Modules.Usuarios.Domain;

public static class ErroresUsuario
{
    public static readonly Error NombreObligatorio =
        Error.Validacion("Usuarios.NombreObligatorio", "El nombre de usuario es obligatorio.");

    public static readonly Error NombreMuyLargo =
        Error.Validacion("Usuarios.NombreMuyLargo", "El nombre de usuario no puede superar los 50 caracteres.");

    public static readonly Error PasswordInvalida =
        Error.Validacion("Usuarios.PasswordInvalida", "La contraseña debe tener al menos 12 caracteres y hasta 72 bytes UTF-8.");

    public static readonly Error RolInvalido =
        Error.Validacion("Usuarios.RolInvalido", "El rol debe ser ADMIN u OPERADOR.");

    public static readonly Error CredencialesInvalidas =
        Error.NoAutorizado("Usuarios.CredencialesInvalidas", "Usuario o contraseña inválidos.");

    public static readonly Error IntentosExcedidos =
        Error.LimiteExcedido("Usuarios.IntentosExcedidos", "No se pudo iniciar sesión. Esperá 15 minutos antes de volver a intentar.");

    public static Error NombreDuplicado(string nombre) =>
        Error.Conflicto("Usuarios.NombreDuplicado", $"Ya existe un usuario con el nombre '{nombre}'.");

    public static Error NoEncontrado(int id) =>
        Error.NoEncontrado("Usuarios.NoEncontrado", $"No se encontró el usuario con identificador {id}.");
}

/// <summary>
/// Entidad de dominio rica para Usuario.
/// Normaliza el nombre de usuario en minúsculas igual que `AccountController.Login` en el legacy.
/// </summary>
public sealed class Usuario : Entidad
{
    private Usuario() { }

    private Usuario(string nombreUsuario, string passwordHash, string rol)
    {
        NombreUsuario = nombreUsuario;
        PasswordHash = passwordHash;
        Rol = rol;
        Activo = true;
    }

    public string NombreUsuario { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string Rol { get; private set; } = Roles.Operador;
    public bool Activo { get; private set; }

    public static string? NormalizarNombre(string? nombre)
    {
        var texto = Validar.TextoRequerido(nombre, 50, ErroresUsuario.NombreObligatorio, ErroresUsuario.NombreMuyLargo);
        return texto.EsExito ? texto.Valor.ToLowerInvariant() : null;
    }

    public static Result<Usuario> Crear(string? nombreUsuario, string? passwordHash, string? rol)
    {
        var nombreValido = Validar.TextoRequerido(
            nombreUsuario, 50, ErroresUsuario.NombreObligatorio, ErroresUsuario.NombreMuyLargo);
        if (nombreValido.EsFallo)
            return nombreValido.Error;

        if (string.IsNullOrWhiteSpace(passwordHash))
            return ErroresUsuario.PasswordInvalida;

        var rolNormalizado = Roles.Normalizar(rol);
        if (rolNormalizado is null)
            return ErroresUsuario.RolInvalido;

        return new Usuario(nombreValido.Valor.ToLowerInvariant(), passwordHash.Trim(), rolNormalizado);
    }

    public Result CambiarRol(string? nuevoRol)
    {
        var rolNormalizado = Roles.Normalizar(nuevoRol);
        if (rolNormalizado is null)
            return ErroresUsuario.RolInvalido;

        Rol = rolNormalizado;
        return Result.Exito();
    }

    public Result CambiarPasswordHash(string? nuevoHash)
    {
        if (string.IsNullOrWhiteSpace(nuevoHash))
            return ErroresUsuario.PasswordInvalida;

        PasswordHash = nuevoHash.Trim();
        return Result.Exito();
    }

    public void Desactivar() => Activo = false;

    public void Activar() => Activo = true;
}

