namespace Facturacion.Api.Seguridad;
public static class Roles
{
    public const string Admin = "ADMIN";
    public const string Operador = "OPERADOR";

    public static readonly IReadOnlyList<string> Todos = [Admin, Operador];

    public static bool EsValido(string? rol)
        => rol is not null && Todos.Contains(rol.Trim().ToUpperInvariant());
}


public static class Politicas
{

    public const string Consultar = nameof(Consultar);

    public const string GestionarCatalogo = nameof(GestionarCatalogo);

    public const string AdministrarUsuarios = nameof(AdministrarUsuarios);
}
