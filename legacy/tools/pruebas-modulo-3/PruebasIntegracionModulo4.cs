using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using FacturacionLegacy.Datos;
using FacturacionLegacy.Datos.Entidades;
using FacturacionLegacy.Datos.Productos;
using FacturacionLegacy.WindowsService;
using Serilog;
using Serilog.Core;
using Serilog.Events;

// Se ejecuta con el arnés existente; no necesita SQL Server ni otro proyecto.
internal static class PruebasIntegracionModulo4
{
    public static void Ejecutar(Action<bool, string> comprobar)
    {
        var productos = Enumerable.Range(1, 25).Select(i => new Producto
        {
            Id = i, Codigo = (26 - i).ToString("D2"), Nombre = "Demo", PrecioUnitario = i, Activo = i % 2 == 0
        }).ToList();
        using (var db = new FacturacionDbContext())
        {
            db.Productos = new ProductosMemoria(productos);
            IConsultaProductos consultas = new ConsultaProductosEf6(db);
            var lista = consultas.ListarAsync(CancellationToken.None).GetAwaiter().GetResult();
            comprobar(lista.Select(p => p.Id).SequenceEqual(productos.OrderBy(p => p.Codigo).ThenBy(p => p.Id).Take(20).Select(p => p.Id)), "Async integrado conserva orden y límite de 20");
            comprobar(lista.Any(p => !p.Activo), "Async incluye inactivos como la demo original");
            comprobar(consultas.ListarActivos().All(p => p.Activo) && consultas.ListarActivos().Count == 12, "Catálogo síncrono del Módulo 3 conserva filtro de activos");
            var detalle = consultas.ObtenerAsync(1, CancellationToken.None).GetAwaiter().GetResult();
            comprobar(detalle.Id == 1 && detalle.Codigo == "25" && !detalle.Activo && detalle.PrecioUnitario == 1m, "Detalle async reutiliza ProductoConsultaDto sin filtrar inactivos");
            comprobar(consultas.ObtenerAsync(999, CancellationToken.None).GetAwaiter().GetResult() == null, "Detalle async inexistente devuelve null");
            comprobar(consultas.Obtener(new[] { 1, 2 }).Count == 2, "Consulta síncrona de precios conserva productos solicitados");
            comprobar(consultas.ListarAsync(new CancellationToken(true)).IsCanceled, "Listado acepta cancelación antes de consultar");
            comprobar(consultas.ObtenerAsync(1, new CancellationToken(true)).IsCanceled, "Detalle acepta cancelación antes de consultar");
            bool invalido = false;
            try { consultas.ObtenerAsync(0, CancellationToken.None).GetAwaiter().GetResult(); }
            catch (ArgumentOutOfRangeException) { invalido = true; }
            comprobar(invalido, "Detalle async rechaza identificador no positivo");
        }

        var eventos = new EventosMemoria();
        using (var log = new LoggerConfiguration().WriteTo.Sink(eventos).CreateLogger())
        {
            int consultas = 0;
            var proceso = new ResumenPendientes(() => { consultas++; return 3; }, log);
            comprobar(proceso.EjecutarUnaVez() && consultas == 1, "Resumen ejecuta la operación inyectada una vez");
            comprobar(eventos.Valores.Count == 3 && eventos.Valores.Any(e => e.Properties.ContainsKey("CantidadFacturas") && (int)((ScalarValue)e.Properties["CantidadFacturas"]).Value == 3), "Resumen conserva cantidad numérica y tres eventos estructurados");
            proceso.Dispose();
            comprobar(!proceso.EjecutarUnaVez() && consultas == 1, "Detener impide nuevas consultas");
            eventos.Valores.Clear();
            using (var fallido = new ResumenPendientes(() => { throw new InvalidOperationException("dato sensible simulado"); }, log))
            {
                comprobar(!fallido.EjecutarUnaVez(), "Resumen devuelve false ante error controlado");
                var error = eventos.Valores.Single(e => e.Level == LogEventLevel.Error);
                comprobar((string)((ScalarValue)error.Properties["TipoError"]).Value == "InvalidOperationException" && error.Exception == null && !error.RenderMessage().Contains("dato sensible"), "Error estructurado registra solo tipo técnico");
            }
        }
    }

    private sealed class EventosMemoria : ILogEventSink
    {
        public readonly List<LogEvent> Valores = new List<LogEvent>();
        public void Emit(LogEvent evento) { Valores.Add(evento); }
    }

    private sealed class ProductosMemoria : DbSet<Producto>, IQueryable<Producto>, IDbAsyncEnumerable<Producto>
    {
        private readonly IQueryable<Producto> datos;
        public ProductosMemoria(IEnumerable<Producto> productos) { datos = new ConsultaAsync<Producto>(productos); }
        public override System.Data.Entity.Infrastructure.DbQuery<Producto> AsNoTracking() { return this; }
        Type IQueryable.ElementType { get { return datos.ElementType; } }
        Expression IQueryable.Expression { get { return datos.Expression; } }
        IQueryProvider IQueryable.Provider { get { return datos.Provider; } }
        IEnumerator<Producto> IEnumerable<Producto>.GetEnumerator() { return datos.GetEnumerator(); }
        IEnumerator IEnumerable.GetEnumerator() { return datos.GetEnumerator(); }
        IDbAsyncEnumerator<Producto> IDbAsyncEnumerable<Producto>.GetAsyncEnumerator() { return new EnumeradorAsync<Producto>(datos.GetEnumerator()); }
        IDbAsyncEnumerator IDbAsyncEnumerable.GetAsyncEnumerator() { return new EnumeradorAsync<Producto>(datos.GetEnumerator()); }
    }

    private sealed class ConsultaAsync<T> : EnumerableQuery<T>, IDbAsyncEnumerable<T>, IQueryable<T>
    {
        public ConsultaAsync(IEnumerable<T> valores) : base(valores) { }
        public ConsultaAsync(Expression expresion) : base(expresion) { }
        IQueryProvider IQueryable.Provider { get { return new ProveedorAsync(this); } }
        public IDbAsyncEnumerator<T> GetAsyncEnumerator() { return new EnumeradorAsync<T>(this.AsEnumerable().GetEnumerator()); }
        IDbAsyncEnumerator IDbAsyncEnumerable.GetAsyncEnumerator() { return GetAsyncEnumerator(); }
    }

    private sealed class ProveedorAsync : IDbAsyncQueryProvider
    {
        private readonly IQueryProvider interno;
        public ProveedorAsync(IQueryProvider interno) { this.interno = interno; }
        public IQueryable CreateQuery(Expression expresion)
        {
            var tipo = typeof(ConsultaAsync<>).MakeGenericType(expresion.Type.GetGenericArguments()[0]);
            return (IQueryable)Activator.CreateInstance(tipo, new object[] { expresion });
        }
        public IQueryable<T> CreateQuery<T>(Expression expresion) { return new ConsultaAsync<T>(expresion); }
        public object Execute(Expression expresion) { return interno.Execute(expresion); }
        public T Execute<T>(Expression expresion) { return interno.Execute<T>(expresion); }
        public Task<object> ExecuteAsync(Expression expresion, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(Execute(expresion));
        }
        public Task<T> ExecuteAsync<T>(Expression expresion, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(Execute<T>(expresion));
        }
    }

    private sealed class EnumeradorAsync<T> : IDbAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> interno;
        public EnumeradorAsync(IEnumerator<T> interno) { this.interno = interno; }
        public T Current { get { return interno.Current; } }
        object IDbAsyncEnumerator.Current { get { return Current; } }
        public Task<bool> MoveNextAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(interno.MoveNext());
        }
        public void Dispose() { interno.Dispose(); }
    }
}
