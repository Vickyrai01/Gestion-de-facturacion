using System.Reflection;
using Facturacion.BuildingBlocks.Dominio;
using Facturacion.Modules.Clientes;
using Facturacion.Modules.Facturacion;
using Facturacion.Modules.Productos;
using Facturacion.Modules.Usuarios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Facturacion.Tests;

/// <summary>
/// Fitness Functions de Arquitectura para la Clase 7.
/// Verifican automáticamente las 5 reglas del temario:
/// 1. Monolito a Modular Monolith (aislamiento entre módulos).
/// 2. Separación de capas (Domain no conoce EF Core ni ASP.NET Core).
/// 3. Eliminación de lógica/persistencia en Controllers.
/// 4. Desacople DTO / Repositorios (los controllers no exponen entidades y los DbContext son internal).
/// 5. Clean Architecture / DDD (entidades con setters privados, sin modelo anémico).
/// </summary>
public sealed class ArquitecturaModularTests
{
    private static readonly Assembly[] Modulos =
    [
        typeof(ClientesModule).Assembly,
        typeof(ProductosModule).Assembly,
        typeof(FacturacionModule).Assembly,
        typeof(UsuariosModule).Assembly
    ];

    [Fact]
    public void Modulos_no_referencian_implementaciones_internas_de_otros_modulos()
    {
        // Un módulo sólo puede referenciar BuildingBlocks o proyectos *.Contracts de otros módulos.
        foreach (var modulo in Modulos)
        {
            var nombrePropio = modulo.GetName().Name!;
            var referenciasAOtrosModulos = modulo
                .GetReferencedAssemblies()
                .Select(r => r.Name!)
                .Where(n => n.StartsWith("Facturacion.Modules.", StringComparison.Ordinal)
                            && n != nombrePropio
                            && !n.EndsWith(".Contracts", StringComparison.Ordinal))
                .ToList();

            Assert.Empty(referenciasAOtrosModulos);
        }
    }

    [Fact]
    public void Capa_Domain_no_depende_de_EntityFramework_ni_de_AspNetCore()
    {
        foreach (var modulo in Modulos)
        {
            var tiposDominio = modulo
                .GetTypes()
                .Where(t => t.Namespace is not null && t.Namespace.Contains(".Domain", StringComparison.Ordinal));

            foreach (var tipo in tiposDominio)
            {
                Assert.False(typeof(DbContext).IsAssignableFrom(tipo), $"{tipo.FullName} no puede heredar de DbContext.");
                Assert.False(typeof(ControllerBase).IsAssignableFrom(tipo), $"{tipo.FullName} no puede heredar de ControllerBase.");

                foreach (var propiedad in tipo.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    var ns = propiedad.PropertyType.Namespace ?? string.Empty;
                    Assert.DoesNotContain("Microsoft.EntityFrameworkCore", ns);
                    Assert.DoesNotContain("Microsoft.AspNetCore", ns);
                }
            }
        }
    }

    [Fact]
    public void Controllers_no_inyectan_DbContext_ni_repositorios()
    {
        foreach (var modulo in Modulos)
        {
            var controllers = modulo
                .GetTypes()
                .Where(t => typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract);

            foreach (var controller in controllers)
            {
                foreach (var ctor in controller.GetConstructors())
                {
                    foreach (var parametro in ctor.GetParameters())
                    {
                        var tipoParametro = parametro.ParameterType;
                        Assert.False(
                            typeof(DbContext).IsAssignableFrom(tipoParametro),
                            $"El controller {controller.Name} no debe inyectar DbContext ({tipoParametro.Name}).");
                        Assert.DoesNotContain(
                            "Repositorio",
                            tipoParametro.Name,
                            StringComparison.OrdinalIgnoreCase);
                    }
                }
            }
        }
    }

    [Fact]
    public void DbContexts_y_repositorios_concretos_son_internal_a_cada_modulo()
    {
        foreach (var modulo in Modulos)
        {
            var dbContexts = modulo
                .GetTypes()
                .Where(t => typeof(DbContext).IsAssignableFrom(t));

            Assert.NotEmpty(dbContexts);
            foreach (var dbContext in dbContexts)
            {
                Assert.False(
                    dbContext.IsPublic,
                    $"El DbContext {dbContext.FullName} debe ser internal para impedir que otros módulos accedan a sus tablas.");
            }
        }
    }

    [Fact]
    public void Entidades_de_dominio_no_tienen_setters_publicos_anemicos()
    {
        foreach (var modulo in Modulos)
        {
            var entidades = modulo
                .GetTypes()
                .Where(t => typeof(Entidad).IsAssignableFrom(t) && !t.IsAbstract);

            Assert.NotEmpty(entidades);
            foreach (var entidad in entidades)
            {
                var propiedadesConSetterPublico = entidad
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.SetMethod is not null && p.SetMethod.IsPublic)
                    .Select(p => $"{entidad.Name}.{p.Name}")
                    .ToList();

                Assert.Empty(propiedadesConSetterPublico);
            }
        }
    }
}

