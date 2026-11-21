using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class ResolvedorTiendaTests
{
    private TablaInteracciones tabla;

    [SetUp]
    public void Preparar()
    {
        tabla = ScriptableObject.CreateInstance<TablaInteracciones>();
        tabla.reglas.Add(new ReglaInteraccion
        {
            etiquetaA = Etiqueta.Fuego, etiquetaB = Etiqueta.Frio,
            modificadorValor = -0.2f, deltaPeligro = -1
        });
        tabla.reglas.Add(new ReglaInteraccion
        {
            etiquetaA = Etiqueta.Fuego, etiquetaB = Etiqueta.Hambre,
            modificadorValor = 0.4f, deltaPeligro = 2
        });
    }

    private static ObjetoMalditoData Crear(Etiqueta etiqueta, int precio, int peligro)
    {
        var d = ScriptableObject.CreateInstance<ObjetoMalditoData>();
        d.nombre = etiqueta.ToString();
        d.precioBase = precio;
        d.peligro = peligro;
        d.etiquetas = new List<Etiqueta> { etiqueta };
        return d;
    }

    [Test]
    public void FuegoYFrio_AdyacentesSeNeutralizan()
    {
        var grid = new GridTienda(5, 5);
        grid.Colocar(Crear(Etiqueta.Fuego, 10, 1), new Vector2Int(1, 1), out ObjetoColocado fuego);
        grid.Colocar(Crear(Etiqueta.Frio, 20, 0), new Vector2Int(2, 1), out _);

        ResultadoTienda r = ResolvedorTienda.Resolver(grid, tabla);

        Assert.AreEqual(1, r.efectos.Count);
        Assert.AreEqual(8, r.PrecioFinal(fuego));   // 10 * 0.8
        Assert.AreEqual(0, r.peligroTotal);          // 1 + 0 - 1
    }

    [Test]
    public void ObjetosFueraDeAlcance_NoInteractuan()
    {
        var grid = new GridTienda(6, 3);
        grid.Colocar(Crear(Etiqueta.Fuego, 10, 1), new Vector2Int(0, 1), out _);
        grid.Colocar(Crear(Etiqueta.Frio, 20, 0), new Vector2Int(4, 1), out _);

        ResultadoTienda r = ResolvedorTienda.Resolver(grid, tabla);

        Assert.AreEqual(0, r.efectos.Count);
    }

    [Test]
    public void Pareja_CuentaUnaSolaVez_AunqueAmbasAurasSeAlcancen()
    {
        var grid = new GridTienda(4, 4);
        grid.Colocar(Crear(Etiqueta.Fuego, 10, 1), new Vector2Int(1, 1), out _);
        grid.Colocar(Crear(Etiqueta.Frio, 20, 0), new Vector2Int(2, 1), out _);

        ResultadoTienda r = ResolvedorTienda.Resolver(grid, tabla);

        Assert.AreEqual(1, r.efectos.Count);
    }

    [Test]
    public void ValorSeAcumula_ConVariosVecinos()
    {
        var grid = new GridTienda(5, 5);
        grid.Colocar(Crear(Etiqueta.Hambre, 30, 2), new Vector2Int(2, 2), out ObjetoColocado hambre);
        grid.Colocar(Crear(Etiqueta.Fuego, 10, 1), new Vector2Int(1, 2), out _);
        grid.Colocar(Crear(Etiqueta.Fuego, 10, 1), new Vector2Int(3, 2), out _);

        ResultadoTienda r = ResolvedorTienda.Resolver(grid, tabla);

        Assert.AreEqual(54, r.PrecioFinal(hambre));  // 30 * (1 + 0.4 + 0.4)
        Assert.AreEqual(8, r.peligroTotal);          // 2 + 1 + 1 + 2 + 2
    }

    [Test]
    public void LaPuerta_NoSePuedeOcupar()
    {
        var grid = new GridTienda(4, 4) { puerta = new Vector2Int(0, 0) };

        Assert.IsFalse(grid.EstaLibre(new Vector2Int(0, 0)));
        Assert.IsTrue(grid.EsTransitable(new Vector2Int(0, 0)));
    }
}