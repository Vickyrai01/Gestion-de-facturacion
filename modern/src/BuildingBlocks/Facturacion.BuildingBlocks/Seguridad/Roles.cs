namespace Facturacion.BuildingBlocks.Seguridad;

/// <summary>
/// Roles del sistema (mismos valores que RolesUsuario del legacy y que la tabla USUARIOS).
/// Son parte del "shared kernel": todos los módulos los conocen.
/// </summary>
public static class Roles
{
    public const string Admin = "ADMIN";
    public const string Operador = "OPERADOR";

    public static readonly IReadOnlyList<string> Todos = [Admin, Operador];

    public static string? Normalizar(string? rol)
    {
        var normalizado = rol?.Trim().ToUpperInvariant();
        return normalizado is not null && Todos.Contains(normalizado) ? normalizado : null;
    }
}

/// <summary>
/// Políticas de autorización por operación (decisión de la clase 6).
/// Los controllers declaran QUÉ política exigen; el host define CÓMO se cumple.
/// </summary>
public static class Politicas
{
    /// <summary>ADMIN u OPERADOR: consultar y crear facturas.</summary>
    public const string Operar = nameof(Operar);

    /// <summary>Sólo ADMIN: ABM de catálogo, emitir/anular y reportes.</summary>
    public const string Administrar = nameof(Administrar);
}

