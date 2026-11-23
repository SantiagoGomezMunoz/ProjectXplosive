using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class PistasTests
{
    private static ObjetoMalditoData Objeto(int peligro)
    {
        var d = ScriptableObject.CreateInstance<ObjetoMalditoData>();
        d.nombre = "Objeto";
        d.precioBase = 30;
        d.peligro = peligro;
        d.etiquetas = new List<Etiqueta> { Etiqueta.Fuego };
        return d;
    }

    [Test]
    public void Calidad_DependeDeLosObjetosSinCalmar_Y_BajaConReputacionBaja()
    {
        Assert.AreEqual(CalidadPistas.Claras, GeneradorSubasta.CalcularCalidad(0, false));
        Assert.AreEqual(CalidadPistas.Claras, GeneradorSubasta.CalcularCalidad(1, false));
        Assert.AreEqual(CalidadPistas.Parciales, GeneradorSubasta.CalcularCalidad(2, false));
        Assert.AreEqual(CalidadPistas.Parciales, GeneradorSubasta.CalcularCalidad(3, false));
        Assert.AreEqual(CalidadPistas.Confusas, GeneradorSubasta.CalcularCalidad(4, false));
        Assert.AreEqual(CalidadPistas.Confusas, GeneradorSubasta.CalcularCalidad(9, false));

        Assert.AreEqual(CalidadPistas.Parciales, GeneradorSubasta.CalcularCalidad(0, true));
        Assert.AreEqual(CalidadPistas.Confusas, GeneradorSubasta.CalcularCalidad(2, true));
        Assert.AreEqual(CalidadPistas.Confusas, GeneradorSubasta.CalcularCalidad(4, true));
    }

    [Test]
    public void Claras_DanLaEtiquetaYElPeligroExactos()
    {
        List<Pista> pistas = GeneradorSubasta.GenerarPistas(Objeto(2), CalidadPistas.Claras, new System.Random(1));

        Assert.AreEqual(2, pistas.Count);

        Pista etiqueta = pistas.Find(p => p.tipo == TipoPista.Etiqueta);
        Pista peligro = pistas.Find(p => p.tipo == TipoPista.Peligro);

        Assert.IsFalse(etiqueta.ambigua);
        CollectionAssert.AreEqual(new List<Etiqueta> { Etiqueta.Fuego }, etiqueta.etiquetas);
        Assert.IsFalse(peligro.ambigua);
        Assert.AreEqual(2, peligro.peligroMin);
        Assert.AreEqual(2, peligro.peligroMax);
    }

    [Test]
    public void Parciales_DanUnaSolaPistaExacta()
    {
        for (int semilla = 0; semilla < 20; semilla++)
        {
            List<Pista> pistas = GeneradorSubasta.GenerarPistas(
                Objeto(2), CalidadPistas.Parciales, new System.Random(semilla));

            Assert.AreEqual(1, pistas.Count);
            Assert.IsFalse(pistas[0].ambigua);
        }
    }

    [Test]
    public void Confusas_SonAmbiguasPeroSiempreContienenLaVerdad()
    {
        for (int semilla = 0; semilla < 50; semilla++)
        {
            for (int peligro = 0; peligro <= 5; peligro++)
            {
                List<Pista> pistas = GeneradorSubasta.GenerarPistas(
                    Objeto(peligro), CalidadPistas.Confusas, new System.Random(semilla));

                Assert.AreEqual(1, pistas.Count);
                Pista p = pistas[0];
                Assert.IsTrue(p.ambigua);

                if (p.tipo == TipoPista.Etiqueta)
                {
                    Assert.AreEqual(2, p.etiquetas.Count);
                    Assert.IsTrue(p.etiquetas.Contains(Etiqueta.Fuego));
                }
                else
                {
                    Assert.AreEqual(2, p.peligroMax - p.peligroMin);
                    Assert.GreaterOrEqual(p.peligroMin, 0);
                    Assert.LessOrEqual(p.peligroMax, 5);
                    Assert.LessOrEqual(p.peligroMin, peligro);
                    Assert.GreaterOrEqual(p.peligroMax, peligro);
                }
            }
        }
    }
}