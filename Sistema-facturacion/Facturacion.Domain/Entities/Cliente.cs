namespace Facturacion.Domain.Entities;

public sealed class Cliente
{
    private Cliente()
    {
    }

    private Cliente(string nombre, string apellido, string documento, string email)
    {
        Id = 0;
        Nombre = ValidarTexto(nombre, nameof(nombre), 100);
        Apellido = ValidarTexto(apellido, nameof(apellido), 100);
        Documento = ValidarTexto(documento, nameof(documento), 30);
        Email = ValidarEmail(email);
        Activo = true;
    }

    public int Id { get; private set; }
    public string Nombre { get; private set; } = null!;
    public string Apellido { get; private set; } = null!;
    public string Documento { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public bool Activo { get; private set; }

    public static Cliente Crear(string nombre, string apellido, string documento, string email)
        => new(nombre, apellido, documento, email);

    public void ActualizarDatos(string nombre, string apellido, string documento, string email)
    {
        Nombre = ValidarTexto(nombre, nameof(nombre), 100);
        Apellido = ValidarTexto(apellido, nameof(apellido), 100);
        Documento = ValidarTexto(documento, nameof(documento), 30);
        Email = ValidarEmail(email);
    }

    public void Activar() => Activo = true;

    public void Desactivar() => Activo = false;

    private static string ValidarEmail(string email)
    {
        email = ValidarTexto(email, nameof(email), 254);
        if (!email.Contains('@', StringComparison.Ordinal) || email.StartsWith('@') || email.EndsWith('@'))
            throw new ArgumentException("El email no tiene un formato válido.", nameof(email));

        return email;
    }

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
