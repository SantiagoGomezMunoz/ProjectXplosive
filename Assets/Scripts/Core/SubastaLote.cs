using System;
using System.Collections.Generic;

public enum EstadoSubasta { EnCurso, GanaJugador, GanaRival, SinAdjudicar }

// Subasta ascendente de un lote: el precio sube un incremento por ronda y cada
// participante acepta el nuevo precio o se retira. Gana quien queda solo y paga el
// último precio que aceptó su competidor.
public class SubastaLote
{
    private readonly List<PerfilCliente> rivalesActivos = new();
    private readonly int peligroTienda;

    public Lote Lote { get; }
    public int PrecioActual { get; private set; }
    public int Incremento { get; }
    public int Ronda { get; private set; }
    public bool JugadorActivo { get; private set; } = true;
    public EstadoSubasta Estado { get; private set; } = EstadoSubasta.EnCurso;
    public PerfilCliente GanadorRival { get; private set; }
    public int PrecioFinal { get; private set; }
    public IReadOnlyList<PerfilCliente> RivalesActivos => rivalesActivos;
    public int PrecioSiguiente => PrecioActual + Incremento;

    public SubastaLote(Lote lote, IEnumerable<PerfilCliente> rivales, int incremento, int peligroTienda)
    {
        Lote = lote;
        Incremento = Math.Max(1, incremento);
        PrecioActual = lote.precioSalida;
        this.peligroTienda = peligroTienda;

        foreach (PerfilCliente r in rivales)
            if (r != null && Quiere(r, PrecioActual)) rivalesActivos.Add(r);
    }

    public static int MaximoDe(PerfilCliente rival, ObjetoMalditoData objeto) =>
        (int)Math.Round(objeto.precioBase * (double)rival.multiplicadorPrecio);

    private bool Quiere(PerfilCliente rival, int precio)
    {
        ObjetoMalditoData objeto = Lote.objeto;

        if (objeto.peligro < rival.peligroMinimoObjeto) return false;
        if (rival.umbralMiedo > 0 && peligroTienda > rival.umbralMiedo) return false;

        return precio <= MaximoDe(rival, objeto);
    }

    // Una ronda: el jugador sigue (acepta el precio siguiente) o se retira.
    public void Avanzar(bool jugadorSigue, int dineroJugador)
    {
        if (Estado != EstadoSubasta.EnCurso) return;
        Ronda++;

        int nuevo = PrecioSiguiente;
        bool jugadorQuiere = JugadorActivo && jugadorSigue && dineroJugador >= PrecioActual;
        bool jugadorCompite = jugadorQuiere && dineroJugador >= nuevo;

        var quedan = new List<PerfilCliente>();
        foreach (PerfilCliente r in rivalesActivos)
            if (Quiere(r, nuevo)) quedan.Add(r);

        int supervivientes = quedan.Count + (jugadorCompite ? 1 : 0);

        if (supervivientes >= 2)
        {
            PrecioActual = nuevo;
            rivalesActivos.Clear();
            rivalesActivos.AddRange(quedan);
            JugadorActivo = jugadorCompite;
            return;
        }

        if (supervivientes == 1)
        {
            Terminar(jugadorCompite ? EstadoSubasta.GanaJugador : EstadoSubasta.GanaRival);
            GanadorRival = jugadorCompite ? null : quedan[0];
            return;
        }

        // Nadie acepta el nuevo precio: si el jugador quería y podía pagar el actual, se lo lleva
        Terminar(jugadorQuiere ? EstadoSubasta.GanaJugador : EstadoSubasta.SinAdjudicar);
    }

    // Termina la subasta entre los rivales cuando el jugador ya no participa
    public void ResolverRestante()
    {
        int seguridad = 10000;
        while (Estado == EstadoSubasta.EnCurso && seguridad-- > 0) Avanzar(false, 0);
    }

    private void Terminar(EstadoSubasta estado)
    {
        Estado = estado;
        PrecioFinal = estado == EstadoSubasta.SinAdjudicar ? 0 : PrecioActual;
    }
}