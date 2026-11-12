namespace Facturacion.Domain.Entities;

public sealed class Usuario
{
    private Usuario()
    {
    }

    private Usuario(string nombreusuario, string passwordhash, string rol)
    {
        Id = 0;
        Nombreusuario = ValidarTexto(nombreusuario, nameof(nombreusuario), 50);
        Passwordhash = ValidarTexto(passwordhash, nameof(passwordhash), 100);
        Rol = ValidarTexto(rol, nameof(rol), 10);
        Activo = true;
    }

    public int Id { get; private set; }
    public string Nombreusuario { get; private set; } = null!;
    public string Passwordhash { get; private set; } = null!;
    public string Rol { get; private set; } = null!;
    public bool Activo { get; private set; }

    public static Usuario Crear(string nombreusuario, string passwordhash, string rol)
        => new(nombreusuario, passwordhash, rol);

    public void CambiarPassword(string passwordhash)
        => Passwordhash = ValidarTexto(passwordhash, nameof(passwordhash), 100);

    public void CambiarRol(string rol)
        => Rol = ValidarTexto(rol, nameof(rol), 10);

    public void Activar() => Activo = true;

    public void Desactivar() => Activo = false;

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
