using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class ReputacionTests
{
    private static ConfiguracionPartida Config() =>
        ScriptableObject.CreateInstance<ConfiguracionPartida>();

    [Test]
    public void TramoPara_AsignaCadaReputacionASuTramo()
    {
        ConfiguracionPartida c = Config();

        Assert.AreEqual("Baja", c.TramoPara(-5).nombre);
        Assert.AreEqual("Baja", c.TramoPara(-1).nombre);
        Assert.AreEqual("Media", c.TramoPara(0).nombre);
        Assert.AreEqual("Media", c.TramoPara(5).nombre);
        Assert.AreEqual("Alta", c.TramoPara(6).nombre);
        Assert.AreEqual("Alta", c.TramoPara(20).nombre);
    }

    [Test]
    public void TramoPara_ConReputacionMuyPorDebajoDelPrimerTramo_UsaElMasBajo()
    {
        Assert.AreEqual("Baja", Config().TramoPara(-20000).nombre);
    }

    [Test]
    public void ClientesPorTarde_DependeDelTramo()
    {
        ConfiguracionPartida c = Config();

        Assert.AreEqual(4, c.ClientesPorTarde(-1));
        Assert.AreEqual(6, c.ClientesPorTarde(0));
        Assert.AreEqual(8, c.ClientesPorTarde(6));
    }

    [Test]
    public void LotesExtra_SoloConReputacionAlta()
    {
        ConfiguracionPartida c = Config();

        Assert.AreEqual(0, c.LotesExtra(-1));
        Assert.AreEqual(0, c.LotesExtra(3));
        Assert.AreEqual(1, c.LotesExtra(6));
    }

    [Test]
    public void PistasPeores_SoloConReputacionBaja()
    {
        ConfiguracionPartida c = Config();

        Assert.IsTrue(c.PistasPeores(-1));
        Assert.IsFalse(c.PistasPeores(0));
        Assert.IsFalse(c.PistasPeores(10));
    }

    [Test]
    public void ElOrdenDeLosTramosNoImporta()
    {
        ConfiguracionPartida c = Config();
        c.tramosReputacion.Reverse();

        Assert.AreEqual("Baja", c.TramoPara(-1).nombre);
        Assert.AreEqual("Media", c.TramoPara(2).nombre);
        Assert.AreEqual("Alta", c.TramoPara(9).nombre);
    }

    [Test]
    public void SinTramos_UsaValoresPorDefecto()
    {
        ConfiguracionPartida c = Config();
        c.tramosReputacion = new List<TramoReputacion>();

        Assert.IsNull(c.TramoPara(0));
        Assert.AreEqual(6, c.ClientesPorTarde(0));
        Assert.AreEqual(0, c.LotesExtra(0));
        Assert.IsFalse(c.PistasPeores(0));
    }
}