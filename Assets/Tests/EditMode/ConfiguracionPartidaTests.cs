using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class ConfiguracionPartidaTests
{
    private static ConfiguracionPartida CrearCampana()
    {
        var c = ScriptableObject.CreateInstance<ConfiguracionPartida>();
        c.modo = ModoJuego.Campana;
        c.dineroInicial = 100;
        c.actos = new List<ConfigActo>
        {
            new ConfigActo { nombre = "Acto 1", dias = 15, alquilerDiario = 20, cuotaFinal = 1200 },
            new ConfigActo { nombre = "Acto 2", dias = 15, alquilerDiario = 30, cuotaFinal = 1500 },
            new ConfigActo { nombre = "Acto 3", dias = 15, alquilerDiario = 40, cuotaFinal = 1800 },
            new ConfigActo { nombre = "Acto 4", dias = 15, alquilerDiario = 50, cuotaFinal = 1500 }
        };
        return c;
    }

    private static ConfiguracionPartida CrearInfinito()
    {
        var c = ScriptableObject.CreateInstance<ConfiguracionPartida>();
        c.modo = ModoJuego.Infinito;
        c.dineroInicial = 100;
        c.alquilerInicialInfinito = 20;
        c.diasPorSubidaInfinito = 10;
        c.subidaAlquilerInfinito = 5;
        return c;
    }

    [Test]
    public void TotalDias_SumaLosActos()
    {
        Assert.AreEqual(60, CrearCampana().TotalDias);
    }

    [Test]
    public void DeudaTotal_SumaLasCuotas()
    {
        Assert.AreEqual(6000, CrearCampana().DeudaTotal);
    }

    [Test]
    public void IndiceActo_AsignaCadaDiaASuActo()
    {
        ConfiguracionPartida c = CrearCampana();

        Assert.AreEqual(0, c.IndiceActo(1));
        Assert.AreEqual(0, c.IndiceActo(15));
        Assert.AreEqual(1, c.IndiceActo(16));
        Assert.AreEqual(1, c.IndiceActo(30));
        Assert.AreEqual(2, c.IndiceActo(31));
        Assert.AreEqual(2, c.IndiceActo(45));
        Assert.AreEqual(3, c.IndiceActo(46));
        Assert.AreEqual(3, c.IndiceActo(60));
        Assert.AreEqual(-1, c.IndiceActo(61));
        Assert.AreEqual(-1, c.IndiceActo(0));
    }

    [Test]
    public void Alquiler_SubePorActo()
    {
        ConfiguracionPartida c = CrearCampana();

        Assert.AreEqual(20, c.Alquiler(1));
        Assert.AreEqual(20, c.Alquiler(15));
        Assert.AreEqual(30, c.Alquiler(16));
        Assert.AreEqual(40, c.Alquiler(31));
        Assert.AreEqual(50, c.Alquiler(46));
        Assert.AreEqual(0, c.Alquiler(61));
    }

    [Test]
    public void CuotaDelDia_SoloVenceElUltimoDiaDelActo()
    {
        ConfiguracionPartida c = CrearCampana();

        Assert.AreEqual(0, c.CuotaDelDia(14));
        Assert.AreEqual(1200, c.CuotaDelDia(15));
        Assert.AreEqual(0, c.CuotaDelDia(16));
        Assert.AreEqual(1500, c.CuotaDelDia(30));
        Assert.AreEqual(1800, c.CuotaDelDia(45));
        Assert.AreEqual(1500, c.CuotaDelDia(60));
    }

    [Test]
    public void EsUltimoDia_SoloElDia60()
    {
        ConfiguracionPartida c = CrearCampana();

        Assert.IsFalse(c.EsUltimoDia(59));
        Assert.IsTrue(c.EsUltimoDia(60));
        Assert.IsFalse(c.EsUltimoDia(61));
    }

    [Test]
    public void Infinito_ElAlquilerSubeCadaDiezDias()
    {
        ConfiguracionPartida c = CrearInfinito();

        Assert.AreEqual(20, c.Alquiler(1));
        Assert.AreEqual(20, c.Alquiler(10));
        Assert.AreEqual(25, c.Alquiler(11));
        Assert.AreEqual(30, c.Alquiler(21));
    }

    [Test]
    public void Infinito_NoTieneDeudaNiCuotasNiActos()
    {
        ConfiguracionPartida c = CrearInfinito();

        Assert.AreEqual(0, c.DeudaTotal);
        Assert.AreEqual(0, c.CuotaDelDia(10));
        Assert.AreEqual(-1, c.IndiceActo(5));
        Assert.IsFalse(c.EsUltimoDia(60));
    }

    [Test]
    public void EstadoNuevo_Campana_ArrancaConLaDeuda()
    {
        EstadoPartida e = EstadoPartida.Nueva(CrearCampana());

        Assert.AreEqual(1, e.dia);
        Assert.AreEqual(100, e.dinero);
        Assert.AreEqual(6000, e.deudaRestante);
        Assert.AreEqual(0, e.cuotasPagadas);
        Assert.IsFalse(e.terminada);
    }

    [Test]
    public void EstadoNuevo_Infinito_ArrancaSinDeuda()
    {
        EstadoPartida e = EstadoPartida.Nueva(CrearInfinito());

        Assert.AreEqual(ModoJuego.Infinito, e.modo);
        Assert.AreEqual(0, e.deudaRestante);
    }
}