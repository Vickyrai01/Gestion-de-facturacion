using Facturacion.BuildingBlocks.Aplicacion;
using Facturacion.Modules.Clientes.Domain;
using Facturacion.Modules.Facturacion.Domain;
using Facturacion.Modules.Productos.Domain;
using Facturacion.Modules.Usuarios.Domain;
using Facturacion.Modules.Usuarios.Infrastructure;
using Xunit;

namespace Facturacion.Tests;

/// <summary>
/// Pruebas unitarias de Dominio (DDD) que se ejecutan en memoria sin SQL Server ni contenedor HTTP.
/// </summary>
public sealed class DominioYCasosDeUsoTests
{
    [Fact]
    public void Factura_calcula_subtotal_impuestos_y_total_con_redondeo_away_from_zero()
    {
        // Caso de demostración de la clase: 125.50 x 2 + 100.00 x 3 a tasa 0.21
        // Subtotal = 251.00 + 300.00 = 551.00; Impuestos = 115.71; Total = 666.71
        var precios = new Dictionary<int, PrecioProductoVigente>
        {
            [1] = new(1, 125.50m),
            [2] = new(2, 100.00m)
        };

        var lineas = new[]
        {
            new LineaFacturaSolicitud(1, 2),
            new LineaFacturaSolicitud(2, 3)
        };

        var resultado = Factura.Crear(
            Factura.FormatearNumero(1),
            clienteId: 10,
            fechaEmisionUtc: new DateTime(2026, 11, 18, 12, 0, 0, DateTimeKind.Utc),
            lineas: lineas,
            preciosVigentes: precios,
            tasaImpuestos: 0.21m);

        Assert.True(resultado.EsExito);
        var factura = resultado.Valor;
        Assert.Equal("FAC-0000000001", factura.Numero);
        Assert.Equal(EstadosFactura.Pendiente, factura.Estado);
        Assert.Equal(551.00m, factura.Subtotal);
        Assert.Equal(115.71m, factura.Impuestos);
        Assert.Equal(666.71m, factura.Total);
        Assert.Equal(2, factura.Detalles.Count);
    }

    [Fact]
    public void Factura_respeta_reglas_de_transicion_de_estado_Emitir_y_Anular()
    {
        var precios = new Dictionary<int, PrecioProductoVigente> { [1] = new(1, 100m) };
        var factura = Factura.Crear(
            "FAC-0000000002",
            1,
            DateTime.UtcNow,
            [new LineaFacturaSolicitud(1, 1)],
            precios,
            0.21m).Valor;

        // 1. PENDIENTE -> EMITIDA (éxito)
        Assert.True(factura.Emitir().EsExito);
        Assert.Equal(EstadosFactura.Emitida, factura.Estado);

        // 2. EMITIDA -> EMITIDA (conflicto: solo se puede emitir una factura pendiente)
        var emitirDeNuevo = factura.Emitir();
        Assert.True(emitirDeNuevo.EsFallo);
        Assert.Equal(ErroresFactura.SoloPendienteSePuedeEmitir.Codigo, emitirDeNuevo.Error.Codigo);

        // 3. EMITIDA -> ANULADA (éxito)
        Assert.True(factura.Anular().EsExito);
        Assert.Equal(EstadosFactura.Anulada, factura.Estado);

        // 4. ANULADA -> ANULADA (conflicto: la factura ya está anulada)
        var anularDeNuevo = factura.Anular();
        Assert.True(anularDeNuevo.EsFallo);
        Assert.Equal(ErroresFactura.YaEstaAnulada.Codigo, anularDeNuevo.Error.Codigo);
    }

    [Fact]
    public void Producto_normaliza_codigo_a_mayusculas_y_redondea_precio()
    {
        var resultado = Producto.Crear("  prod-099 ", " Teclado Mecánico ", " Switch táctil ", 1234.565m, 15);

        Assert.True(resultado.EsExito);
        Assert.Equal("PROD-099", resultado.Valor.Codigo);
        Assert.Equal("Teclado Mecánico", resultado.Valor.Nombre);
        Assert.Equal(1234.57m, resultado.Valor.PrecioUnitario);
    }

    [Fact]
    public void Cliente_rechaza_email_invalido_y_permite_baja_logica()
    {
        var invalido = Cliente.Crear("Ana", "García", "30123456", "correo-sin-arroba");
        Assert.True(invalido.EsFallo);
        Assert.Equal(ErroresCliente.EmailInvalido.Codigo, invalido.Error.Codigo);

        var valido = Cliente.Crear("Ana", "García", "30123456", "ana@example.com").Valor;
        Assert.True(valido.Activo);
        valido.Desactivar();
        Assert.False(valido.Activo);
    }

    [Fact]
    public void ControlIntentosLogin_bloquea_al_sexto_intento_y_desbloquea_a_los_15_minutos()
    {
        var reloj = new RelojFalso(new DateTime(2026, 11, 18, 15, 0, 0, DateTimeKind.Utc));
        var control = new ControlIntentosLoginEnMemoria(reloj);

        for (var i = 0; i < 5; i++)
            Assert.True(control.AdmitirIntento("admin"));

        // 6to intento dentro de los 15 minutos -> bloqueado
        Assert.False(control.AdmitirIntento("admin"));

        // Avanzamos 15 minutos exactos -> la ventana expira y vuelve a admitir
        reloj.Avanzar(TimeSpan.FromMinutes(15));
        Assert.True(control.AdmitirIntento("admin"));
    }

    private sealed class RelojFalso(DateTime inicialUtc) : IReloj
    {
        public DateTime AhoraUtc { get; private set; } = inicialUtc;

        public void Avanzar(TimeSpan duracion) => AhoraUtc = AhoraUtc.Add(duracion);
    }
}

