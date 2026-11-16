namespace Facturacion.Api.Seguridad;

/// <summary>
/// Roles heredados del sistema legacy. Se conservan los mismos valores que
/// guarda la columna USUARIOS.ROL para no tener que migrar permisos.
/// </summary>
public static class Roles
{
    public const string Admin = "ADMIN";
    public const string Operador = "OPERADOR";

    public static readonly IReadOnlyList<string> Todos = [Admin, Operador];

    public static bool EsValido(string? rol)
        => rol is not null && Todos.Contains(rol.Trim().ToUpperInvariant());
}

/// <summary>
/// Políticas de autorización. Los controllers piden una política (la operación)
/// y no un rol: si mañana cambia quién puede hacer qué, se cambia acá.
/// Reproduce la tabla de permisos de legacy/docs/modulo-2/autenticacion.md.
/// </summary>
public static class Politicas
{
    /// <summary>Consultar clientes y productos: ADMIN y OPERADOR.</summary>
    public const string Consultar = nameof(Consultar);

    /// <summary>Crear, editar y desactivar clientes y productos: solo ADMIN.</summary>
    public const string GestionarCatalogo = nameof(GestionarCatalogo);

    /// <summary>Administrar cuentas de usuario: solo ADMIN.</summary>
    public const string AdministrarUsuarios = nameof(AdministrarUsuarios);
}
