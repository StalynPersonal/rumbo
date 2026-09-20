using Microsoft.EntityFrameworkCore;

using Rumbo.Dominio.Entidades.Financiero;

namespace Rumbo.Infraestructura.Persistencia.Semilla;

/// <summary>
/// Catalogo de monedas que se crea junto con la base de datos.
/// </summary>
/// <remarks>
/// <para>
/// Se usa <c>HasData</c> porque las monedas son una tabla GLOBAL y fija: el mismo catalogo
/// sirve para todos los espacios y forma parte de la migracion, de modo que cualquier entorno
/// nuevo arranca con los mismos valores.
/// </para>
/// <para>
/// <b>Aqui NO se siembran tasas de cambio.</b> Inventar un tipo de cambio seria fabricar un
/// dato financiero: cualquier importe convertido con el saldria mal y el usuario no tendria
/// forma de saberlo. Las tasas se cargan explicitamente, y si falta la de una fecha el sistema
/// avisa en lugar de suponer una paridad de 1 a 1.
/// </para>
/// </remarks>
public static class SemillaMonedas
{
    /// <summary>Anade las monedas iniciales al modelo.</summary>
    /// <param name="constructor">Constructor del modelo de EF Core.</param>
    public static void Sembrar(ModelBuilder constructor) =>
        constructor.Entity<Moneda>().HasData(
            new Moneda
            {
                Codigo = "DOP",
                Nombre = "Peso dominicano",
                Simbolo = "RD$",
                Decimales = 2,
                Activa = true,
            },
            new Moneda
            {
                Codigo = "USD",
                Nombre = "Dolar estadounidense",
                Simbolo = "US$",
                Decimales = 2,
                Activa = true,
            },
            new Moneda
            {
                Codigo = "EUR",
                Nombre = "Euro",
                Simbolo = "EUR",
                Decimales = 2,
                Activa = true,
            },
            new Moneda
            {
                Codigo = "GBP",
                Nombre = "Libra esterlina",
                Simbolo = "GBP",
                Decimales = 2,
                Activa = true,
            });
}
