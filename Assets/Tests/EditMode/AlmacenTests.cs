using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class AlmacenTests
{
    private static ObjetoMalditoData Objeto(string id)
    {
        var d = ScriptableObject.CreateInstance<ObjetoMalditoData>();
        d.name = id;
        return d;
    }

    [Test]
    public void Agregar_CreaLaEntradaConSuCantidad()
    {
        var estado = new EstadoPartida();

        estado.AgregarAlAlmacen("fuego");

        Assert.AreEqual(1, estado.almacen.Count);
        Assert.AreEqual(1, estado.CantidadEnAlmacen("fuego"));
    }

    [Test]
    public void Agregar_ElMismoId_SumaALaMismaEntrada()
    {
        var estado = new EstadoPartida();

        estado.AgregarAlAlmacen("fuego");
        estado.AgregarAlAlmacen("fuego", 2);

        Assert.AreEqual(1, estado.almacen.Count);
        Assert.AreEqual(3, estado.CantidadEnAlmacen("fuego"));
    }

    [Test]
    public void Agregar_CantidadCeroONegativa_NoHaceNada()
    {
        var estado = new EstadoPartida();

        estado.AgregarAlAlmacen("fuego", 0);
        estado.AgregarAlAlmacen("fuego", -2);

        Assert.AreEqual(0, estado.almacen.Count);
    }

    [Test]
    public void Quitar_RestaUno_YEliminaLaEntradaAlLlegarACero()
    {
        var estado = new EstadoPartida();
        estado.AgregarAlAlmacen("fuego", 2);

        Assert.IsTrue(estado.QuitarDelAlmacen("fuego"));
        Assert.AreEqual(1, estado.CantidadEnAlmacen("fuego"));

        Assert.IsTrue(estado.QuitarDelAlmacen("fuego"));
        Assert.AreEqual(0, estado.almacen.Count);
    }

    [Test]
    public void Quitar_IdInexistente_DevuelveFalso()
    {
        var estado = new EstadoPartida();

        Assert.IsFalse(estado.QuitarDelAlmacen("nada"));
    }

    [Test]
    public void Catalogo_BuscaPorId()
    {
        var catalogo = ScriptableObject.CreateInstance<CatalogoObjetos>();
        ObjetoMalditoData fuego = Objeto("fuego");
        catalogo.objetos = new List<ObjetoMalditoData> { fuego, Objeto("frio") };

        Assert.AreSame(fuego, catalogo.Buscar("fuego"));
        Assert.IsNull(catalogo.Buscar("desconocido"));
    }

    [Test]
    public void EstadoNuevo_CopiaElAlmacenInicial()
    {
        var config = ScriptableObject.CreateInstance<ConfiguracionPartida>();
        config.almacenInicial = new List<ObjetoInicial>
        {
            new ObjetoInicial { objeto = Objeto("fuego"), cantidad = 2 },
            new ObjetoInicial { objeto = Objeto("frio"), cantidad = 1 }
        };

        EstadoPartida estado = EstadoPartida.Nueva(config);

        Assert.AreEqual(2, estado.CantidadEnAlmacen("fuego"));
        Assert.AreEqual(1, estado.CantidadEnAlmacen("frio"));
    }
}