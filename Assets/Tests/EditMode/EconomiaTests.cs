using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class EconomiaTests
{
    private static ConfiguracionPartida Campana()
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

    private static ConfiguracionPartida Infinito()
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
    public void Alquiler_ConDinero_SeCobraYAvanzaElDia()
    {
        ConfiguracionPartida config = Campana();
        EstadoPartida estado = EstadoPartida.Nueva(config);

        ResultadoCierre r = Economia.CerrarDia(estado, config, new List<int>());

        Assert.IsTrue(r.alquilerPagado);
        Assert.AreEqual(80, estado.dinero);
        Assert.AreEqual(1, estado.dia);
        Assert.AreEqual(0, estado.reputacion);
    }

    [Test]
    public void Alquiler_SinDinero_EmbargaElObjetoDeMenorValorYBajaLaReputacion()
    {
        ConfiguracionPartida config = Campana();
        EstadoPartida estado = EstadoPartida.Nueva(config);
        estado.dinero = 10;

        ResultadoCierre r = Economia.CerrarDia(estado, config, new List<int> { 50, 10, 30 });

        Assert.IsFalse(r.alquilerPagado);
        CollectionAssert.AreEqual(new List<int> { 1 }, r.indicesEmbargados);
        Assert.AreEqual(-1, estado.reputacion);
        Assert.AreEqual(10, estado.dinero);
        Assert.AreEqual(1, estado.dia);
        Assert.IsFalse(estado.terminada);
    }

    [Test]
    public void Alquiler_SinDineroNiObjetos_EsQuiebra()
    {
        ConfiguracionPartida config = Campana();
        EstadoPartida estado = EstadoPartida.Nueva(config);
        estado.dinero = 10;

        ResultadoCierre r = Economia.CerrarDia(estado, config, new List<int>());

        Assert.IsTrue(r.partidaTerminada);
        Assert.IsTrue(estado.terminada);
        Assert.IsFalse(estado.victoria);
        Assert.AreEqual(1, estado.dia);
    }

    [Test]
    public void Cuota_ConDineroSuficiente_ReduceLaDeuda()
    {
        ConfiguracionPartida config = Campana();
        EstadoPartida estado = EstadoPartida.Nueva(config);
        estado.dia = 15;
        estado.dinero = 3000;

        ResultadoCierre r = Economia.CerrarDia(estado, config, new List<int>());

        Assert.IsTrue(r.cuotaPagada);
        Assert.AreEqual(1780, estado.dinero);        // 3000 - 20 - 1200
        Assert.AreEqual(4800, estado.deudaRestante);
        Assert.AreEqual(1, estado.cuotasPagadas);
        Assert.AreEqual(15, estado.dia);
    }

    [Test]
    public void Cuota_SinDinero_EmbargaElObjetoMasValiosoHastaCubrir()
    {
        ConfiguracionPartida config = Campana();
        EstadoPartida estado = EstadoPartida.Nueva(config);
        estado.dia = 15;
        estado.dinero = 1000;

        ResultadoCierre r = Economia.CerrarDia(estado, config, new List<int> { 100, 500, 300 });

        Assert.IsTrue(r.cuotaPagada);
        CollectionAssert.AreEqual(new List<int> { 1 }, r.indicesEmbargados);
        Assert.AreEqual(500, r.dineroPorEmbargo);
        Assert.AreEqual(280, estado.dinero);         // 1000 - 20 + 500 - 1200
        Assert.AreEqual(4800, estado.deudaRestante);
    }

    [Test]
    public void Cuota_SinDinero_EmbargaVariosObjetosSiHaceFalta()
    {
        ConfiguracionPartida config = Campana();
        EstadoPartida estado = EstadoPartida.Nueva(config);
        estado.dia = 15;
        estado.dinero = 1000;

        ResultadoCierre r = Economia.CerrarDia(estado, config, new List<int> { 100, 120, 90 });

        Assert.IsTrue(r.cuotaPagada);
        CollectionAssert.AreEquivalent(new List<int> { 0, 1 }, r.indicesEmbargados);
        Assert.AreEqual(220, r.dineroPorEmbargo);
        Assert.AreEqual(0, estado.dinero);           // 1000 - 20 + 220 - 1200
    }

    [Test]
    public void Cuota_QueNoSePuedeCubrir_TerminaLaPartida()
    {
        ConfiguracionPartida config = Campana();
        EstadoPartida estado = EstadoPartida.Nueva(config);
        estado.dia = 15;
        estado.dinero = 1000;

        ResultadoCierre r = Economia.CerrarDia(estado, config, new List<int> { 100, 50 });

        Assert.IsTrue(r.partidaTerminada);
        Assert.IsFalse(r.cuotaPagada);
        Assert.IsTrue(estado.terminada);
        Assert.IsFalse(estado.victoria);
        Assert.AreEqual(0, r.indicesEmbargados.Count);
        Assert.AreEqual(15, estado.dia);
    }

    [Test]
    public void Dia60_ConLaUltimaCuotaPagada_CompletaLaCampana()
    {
        ConfiguracionPartida config = Campana();
        EstadoPartida estado = EstadoPartida.Nueva(config);
        estado.dia = 60;
        estado.dinero = 10000;
        estado.deudaRestante = 1500;
        estado.cuotasPagadas = 3;

        ResultadoCierre r = Economia.CerrarDia(estado, config, new List<int>());

        Assert.IsTrue(r.campanaCompletada);
        Assert.IsTrue(estado.terminada);
        Assert.IsTrue(estado.victoria);
        Assert.AreEqual(0, estado.deudaRestante);
        Assert.AreEqual(60, estado.dia);
    }

    [Test]
    public void Infinito_SoloCobraAlquiler()
    {
        ConfiguracionPartida config = Infinito();
        EstadoPartida estado = EstadoPartida.Nueva(config);
        estado.dia = 15;
        estado.dinero = 500;

        ResultadoCierre r = Economia.CerrarDia(estado, config, new List<int>());

        Assert.AreEqual(25, r.alquiler);
        Assert.AreEqual(0, r.cuota);
        Assert.AreEqual(475, estado.dinero);
        Assert.AreEqual(15, estado.dia);
    }

    [Test]
    public void PartidaTerminada_NoCambiaNada()
    {
        ConfiguracionPartida config = Campana();
        EstadoPartida estado = EstadoPartida.Nueva(config);
        estado.terminada = true;

        ResultadoCierre r = Economia.CerrarDia(estado, config, new List<int>());

        Assert.IsTrue(r.partidaTerminada);
        Assert.AreEqual(100, estado.dinero);
        Assert.AreEqual(1, estado.dia);
    }

    [Test]
    public void AvanzarDia_SumaUnDia()
    {
        EstadoPartida estado = EstadoPartida.Nueva(Campana());

        Economia.AvanzarDia(estado);

        Assert.AreEqual(2, estado.dia);
    }

    [Test]
    public void AvanzarDia_NoHaceNadaSiLaPartidaTermino()
    {
        EstadoPartida estado = EstadoPartida.Nueva(Campana());
        estado.terminada = true;

        Economia.AvanzarDia(estado);

        Assert.AreEqual(1, estado.dia);
    }
}