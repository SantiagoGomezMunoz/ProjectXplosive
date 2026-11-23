using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class GeneradorLotesTests
{
    private static ObjetoMalditoData Objeto(string id, int precio)
    {
        var d = ScriptableObject.CreateInstance<ObjetoMalditoData>();
        d.name = id;
        d.nombre = id;
        d.precioBase = precio;
        d.peligro = 1;
        d.etiquetas = new List<Etiqueta> { Etiqueta.Fuego };
        return d;
    }

    private static List<ObjetoMalditoData> Catalogo(params int[] precios)
    {
        var lista = new List<ObjetoMalditoData>();
        for (int i = 0; i < precios.Length; i++) lista.Add(Objeto("obj" + i, precios[i]));
        return lista;
    }

    [Test]
    public void GeneraLosLotesPedidos_YElExtraVaAlFinal()
    {
        List<Lote> lotes = GeneradorSubasta.GenerarLotes(
            Catalogo(10, 20, 30, 40, 50), 3, 1, CalidadPistas.Claras, 0.6f, new System.Random(1));

        Assert.AreEqual(4, lotes.Count);
        Assert.IsFalse(lotes[0].esExtra);
        Assert.IsTrue(lotes[3].esExtra);
        Assert.AreEqual(2, lotes[0].pistas.Count); // pistas claras
    }

    [Test]
    public void LosLotesNormales_SonDistintosSiElCatalogoAlcanza()
    {
        List<Lote> lotes = GeneradorSubasta.GenerarLotes(
            Catalogo(10, 20, 30, 40, 50), 3, 0, CalidadPistas.Claras, 0.6f, new System.Random(7));

        var vistos = new HashSet<ObjetoMalditoData>();
        foreach (Lote l in lotes) vistos.Add(l.objeto);

        Assert.AreEqual(3, vistos.Count);
    }

    [Test]
    public void ElPrecioDeSalida_EsUnSesentaPorCientoDelPrecioBase()
    {
        List<Lote> a = GeneradorSubasta.GenerarLotes(
            Catalogo(30), 1, 0, CalidadPistas.Claras, 0.6f, new System.Random(1));
        List<Lote> b = GeneradorSubasta.GenerarLotes(
            Catalogo(10), 1, 0, CalidadPistas.Claras, 0.6f, new System.Random(1));

        Assert.AreEqual(18, a[0].precioSalida);
        Assert.AreEqual(6, b[0].precioSalida);
    }

    [Test]
    public void ElLoteExtra_EsElMasValiosoEntreLosCandidatos()
    {
        // Con 3 objetos, los 3 son candidatos: el extra es siempre el de mayor precio
        List<Lote> lotes = GeneradorSubasta.GenerarLotes(
            Catalogo(10, 50, 30), 0, 1, CalidadPistas.Claras, 0.6f, new System.Random(3));

        Assert.AreEqual(1, lotes.Count);
        Assert.AreEqual(50, lotes[0].objeto.precioBase);
        Assert.IsTrue(lotes[0].esExtra);
    }

    [Test]
    public void SinObjetosEnElCatalogo_NoHayLotes()
    {
        List<Lote> lotes = GeneradorSubasta.GenerarLotes(
            new List<ObjetoMalditoData>(), 3, 1, CalidadPistas.Claras, 0.6f, new System.Random(1));

        Assert.AreEqual(0, lotes.Count);
    }
}