using System.Collections.Generic;

public class ResultadoCierre
{
    public int dia;
    public int alquiler;
    public bool alquilerPagado;
    public int cuota;
    public bool cuotaPagada;
    public int dineroPorEmbargo;
    public int reputacionPerdida;
    public bool partidaTerminada;
    public bool campanaCompletada;
    // Posiciones de la lista de valores recibida que quedan embargadas
    public List<int> indicesEmbargados = new();
}

public static class Economia
{
    public const int ReputacionPorImpago = 1;

    // Cobra el alquiler y la cuota del día, aplica embargos y avanza al día siguiente.
    // valoresEmbargables: valor de cada objeto que se puede embargar (quien llama decide cuál usar).
    public static ResultadoCierre CerrarDia(
        EstadoPartida estado, ConfiguracionPartida config, IReadOnlyList<int> valoresEmbargables)
    {
        var r = new ResultadoCierre { dia = estado.dia };

        if (estado.terminada)
        {
            r.partidaTerminada = true;
            return r;
        }

        var disponibles = new List<int>();
        for (int i = 0; i < valoresEmbargables.Count; i++) disponibles.Add(i);

        // 1. Alquiler
        r.alquiler = config.Alquiler(estado.dia);
        if (estado.dinero >= r.alquiler)
        {
            estado.dinero -= r.alquiler;
            r.alquilerPagado = true;
        }
        else
        {
            estado.reputacion -= ReputacionPorImpago;
            r.reputacionPerdida += ReputacionPorImpago;

            // Penalización: se embarga el objeto de menor valor y no se acredita dinero
            int menor = IndiceDeMenorValor(valoresEmbargables, disponibles);
            if (menor < 0) return Quiebra(estado, r);

            r.indicesEmbargados.Add(menor);
            disponibles.Remove(menor);
        }

        // 2. Cuota
        r.cuota = config.CuotaDelDia(estado.dia);
        if (r.cuota > 0)
        {
            if (estado.dinero >= r.cuota)
            {
                PagarCuota(estado, r);
            }
            else
            {
                // Se embargan los objetos de mayor valor hasta cubrir lo que falta
                int faltante = r.cuota - estado.dinero;
                var orden = new List<int>(disponibles);
                orden.Sort((a, b) => valoresEmbargables[b].CompareTo(valoresEmbargables[a]));

                var elegidos = new List<int>();
                int suma = 0;
                foreach (int i in orden)
                {
                    if (suma >= faltante) break;
                    elegidos.Add(i);
                    suma += valoresEmbargables[i];
                }

                if (suma < faltante) return Quiebra(estado, r);

                r.indicesEmbargados.AddRange(elegidos);
                r.dineroPorEmbargo = suma;
                estado.dinero += suma;
                PagarCuota(estado, r);
            }
        }

        // 3. ¿Fin de la campaña?
        r.campanaCompletada = config.EsUltimoDia(estado.dia) && estado.deudaRestante <= 0;
        if (r.campanaCompletada)
        {
            estado.terminada = true;
            estado.victoria = true;
        }

        return r;
    }

    private static void PagarCuota(EstadoPartida estado, ResultadoCierre r)
    {
        estado.dinero -= r.cuota;
        estado.deudaRestante -= r.cuota;
        estado.cuotasPagadas++;
        r.cuotaPagada = true;
    }

    private static ResultadoCierre Quiebra(EstadoPartida estado, ResultadoCierre r)
    {
        estado.terminada = true;
        estado.victoria = false;
        r.partidaTerminada = true;
        r.indicesEmbargados.Clear();
        r.dineroPorEmbargo = 0;
        return r;
    }

    private static int IndiceDeMenorValor(IReadOnlyList<int> valores, List<int> disponibles)
    {
        int mejor = -1;
        foreach (int i in disponibles)
            if (mejor < 0 || valores[i] < valores[mejor]) mejor = i;
        return mejor;
    }

    // Pasa al día siguiente (no hace nada si la partida terminó)
    public static void AvanzarDia(EstadoPartida estado)
    {
        if (!estado.terminada) estado.dia++;
    }
}