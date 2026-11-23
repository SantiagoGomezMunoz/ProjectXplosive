using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class SubastaTests
{
    private static ObjetoMalditoData Objeto(int precio, int peligro)
    {
        var d = ScriptableObject.CreateInstance<ObjetoMalditoData>();
        d.nombre = "Objeto";
        d.precioBase = precio;
        d.peligro = peligro;
        d.etiquetas = new List<Etiqueta> { Etiqueta.Fuego };
        return d;
    }

    private static PerfilCliente Rival(float multiplicador, int peligroMinimo = 0, int umbralMiedo = 0)
    {
        var p = ScriptableObject.CreateInstance<PerfilCliente>();
        p.nombre = "Rival";
        p.multiplicadorPrecio = multiplicador;
        p.peligroMinimoObjeto = peligroMinimo;
        p.umbralMiedo = umbralMiedo;
        return p;
    }

    private static Lote LoteDe(ObjetoMalditoData objeto, int salida) =>
        new Lote { objeto = objeto, precioSalida = salida };

    [Test]
    public void SinRivales_ElJugadorSeLlevaElLoteAlPrecioDeSalida()
    {
        var s = new SubastaLote(LoteDe(Objeto(30, 1), 18), new List<PerfilCliente>(), 5, 0);

        s.Avanzar(true, 100);

        Assert.AreEqual(EstadoSubasta.GanaJugador, s.Estado);
        Assert.AreEqual(18, s.PrecioFinal);
    }

    [Test]
    public void SinRivales_SiElJugadorPasa_NoSeAdjudica()
    {
        var s = new SubastaLote(LoteDe(Objeto(30, 1), 18), new List<PerfilCliente>(), 5, 0);

        s.Avanzar(false, 100);

        Assert.AreEqual(EstadoSubasta.SinAdjudicar, s.Estado);
        Assert.AreEqual(0, s.PrecioFinal);
    }

    [Test]
    public void SinDineroParaLaSalida_NoSeAdjudica()
    {
        var s = new SubastaLote(LoteDe(Objeto(30, 1), 18), new List<PerfilCliente>(), 5, 0);

        s.Avanzar(true, 10);

        Assert.AreEqual(EstadoSubasta.SinAdjudicar, s.Estado);
    }

    [Test]
    public void ConDineroSoloParaLaSalida_GanaALaSalida()
    {
        var s = new SubastaLote(LoteDe(Objeto(30, 1), 18), new List<PerfilCliente>(), 5, 0);

        s.Avanzar(true, 20); // alcanza para 18, no para 23

        Assert.AreEqual(EstadoSubasta.GanaJugador, s.Estado);
        Assert.AreEqual(18, s.PrecioFinal);
    }

    [Test]
    public void UnRival_ElJugadorGanaPagandoLoUltimoQueAceptoElRival()
    {
        PerfilCliente rival = Rival(1.3f); // máximo: 39
        var s = new SubastaLote(LoteDe(Objeto(30, 1), 18), new List<PerfilCliente> { rival }, 5, 0);

        int seguridad = 50;
        while (s.Estado == EstadoSubasta.EnCurso && seguridad-- > 0) s.Avanzar(true, 1000);

        Assert.AreEqual(EstadoSubasta.GanaJugador, s.Estado);
        Assert.AreEqual(38, s.PrecioFinal); // 18, 23, 28, 33, 38; a 43 el rival se retira
    }

    [Test]
    public void SiElJugadorSeRetira_GanaElRivalAlUltimoPrecio()
    {
        PerfilCliente rival = Rival(1.3f);
        var s = new SubastaLote(LoteDe(Objeto(30, 1), 18), new List<PerfilCliente> { rival }, 5, 0);

        s.Avanzar(true, 1000);  // 23
        s.Avanzar(true, 1000);  // 28
        s.Avanzar(false, 1000); // el jugador se retira

        Assert.AreEqual(EstadoSubasta.GanaRival, s.Estado);
        Assert.AreSame(rival, s.GanadorRival);
        Assert.AreEqual(28, s.PrecioFinal);
    }

    [Test]
    public void DosRivales_GanaElDeMayorMaximo_PagaLoQueAceptoElOtro()
    {
        PerfilCliente alto = Rival(1.3f);  // 39
        PerfilCliente bajo = Rival(0.8f);  // 24
        var s = new SubastaLote(LoteDe(Objeto(30, 1), 18), new List<PerfilCliente> { alto, bajo }, 5, 0);

        s.Avanzar(false, 1000); // 23: siguen los dos
        s.Avanzar(false, 1000); // 28: el bajo se retira

        Assert.AreEqual(EstadoSubasta.GanaRival, s.Estado);
        Assert.AreSame(alto, s.GanadorRival);
        Assert.AreEqual(23, s.PrecioFinal);
    }

    [Test]
    public void UnRival_ConPeligroMinimoMayorQueElDelObjeto_NoParticipa()
    {
        PerfilCliente rival = Rival(1.3f, peligroMinimo: 2);
        var s = new SubastaLote(LoteDe(Objeto(30, 1), 18), new List<PerfilCliente> { rival }, 5, 0);

        Assert.AreEqual(0, s.RivalesActivos.Count);
    }

    [Test]
    public void UnRival_Miedoso_SoloParticipaSiElPeligroNoSuperaSuUmbral()
    {
        PerfilCliente miedoso = Rival(1f, umbralMiedo: 10);
        Lote lote = LoteDe(Objeto(30, 1), 18);

        var tranquilo = new SubastaLote(lote, new List<PerfilCliente> { miedoso }, 5, 10);
        var asustado = new SubastaLote(lote, new List<PerfilCliente> { miedoso }, 5, 12);

        Assert.AreEqual(1, tranquilo.RivalesActivos.Count);
        Assert.AreEqual(0, asustado.RivalesActivos.Count);
    }

    [Test]
    public void UnRival_ConMaximoPorDebajoDelPrecioDeSalida_NoParticipa()
    {
        PerfilCliente rival = Rival(0.5f); // máximo: 15 < 18
        var s = new SubastaLote(LoteDe(Objeto(30, 1), 18), new List<PerfilCliente> { rival }, 5, 0);

        Assert.AreEqual(0, s.RivalesActivos.Count);
    }

    [Test]
    public void ResolverRestante_TerminaLaSubastaEntreLosRivales()
    {
        var s = new SubastaLote(
            LoteDe(Objeto(30, 1), 18),
            new List<PerfilCliente> { Rival(1.3f), Rival(0.8f) },
            5, 0);

        s.Avanzar(false, 0);
        s.ResolverRestante();

        Assert.AreNotEqual(EstadoSubasta.EnCurso, s.Estado);
        Assert.AreEqual(EstadoSubasta.GanaRival, s.Estado);
    }
}