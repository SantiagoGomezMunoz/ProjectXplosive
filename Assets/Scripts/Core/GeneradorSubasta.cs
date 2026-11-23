using System;
using System.Collections.Generic;

public static class GeneradorSubasta
{
    // 0 o 1 sin calmar: claras; 2 o 3: parciales; 4 o más: confusas.
    // Con reputación baja, cada nivel baja un escalón.
    public static CalidadPistas CalcularCalidad(int objetosSinCalmar, bool pistasPeores)
    {
        int nivel = objetosSinCalmar <= 1 ? 0 : (objetosSinCalmar <= 3 ? 1 : 2);
        if (pistasPeores) nivel = Math.Min(2, nivel + 1);
        return (CalidadPistas)nivel;
    }

    public static List<Lote> GenerarLotes(
        IReadOnlyList<ObjetoMalditoData> catalogo,
        int cantidad,
        int lotesExtra,
        CalidadPistas calidad,
        float factorPrecioSalida,
        Random rng)
    {
        var lotes = new List<Lote>();
        if (catalogo == null || catalogo.Count == 0) return lotes;

        // Lotes normales: distintos mientras el catálogo alcance
        var mezcla = new List<ObjetoMalditoData>(catalogo);
        Mezclar(mezcla, rng);
        for (int i = 0; i < cantidad; i++)
            lotes.Add(CrearLote(mezcla[i % mezcla.Count], false, calidad, factorPrecioSalida, rng));

        // Lote extra: el objeto más valioso entre 3 candidatos al azar
        for (int k = 0; k < lotesExtra; k++)
        {
            var candidatos = new List<ObjetoMalditoData>(catalogo);
            Mezclar(candidatos, rng);
            if (candidatos.Count > 3) candidatos.RemoveRange(3, candidatos.Count - 3);

            ObjetoMalditoData mejor = candidatos[0];
            foreach (ObjetoMalditoData c in candidatos)
                if (c.precioBase > mejor.precioBase) mejor = c;

            lotes.Add(CrearLote(mejor, true, calidad, factorPrecioSalida, rng));
        }

        return lotes;
    }

    public static List<Pista> GenerarPistas(ObjetoMalditoData objeto, CalidadPistas calidad, Random rng)
    {
        var pistas = new List<Pista>();

        switch (calidad)
        {
            case CalidadPistas.Claras:
                pistas.Add(PistaEtiqueta(objeto));
                pistas.Add(PistaPeligro(objeto));
                break;

            case CalidadPistas.Parciales:
                pistas.Add(rng.Next(2) == 0 ? PistaEtiqueta(objeto) : PistaPeligro(objeto));
                break;

            default:
                pistas.Add(rng.Next(2) == 0
                    ? PistaEtiquetaAmbigua(objeto, rng)
                    : PistaPeligroAmbigua(objeto, rng));
                break;
        }

        return pistas;
    }

    private static Lote CrearLote(
        ObjetoMalditoData objeto, bool extra, CalidadPistas calidad, float factor, Random rng)
    {
        return new Lote
        {
            objeto = objeto,
            esExtra = extra,
            precioSalida = Math.Max(1, (int)Math.Round(objeto.precioBase * (double)factor)),
            pistas = GenerarPistas(objeto, calidad, rng)
        };
    }

    private static Pista PistaEtiqueta(ObjetoMalditoData o) =>
        new Pista { tipo = TipoPista.Etiqueta, etiquetas = new List<Etiqueta>(o.etiquetas) };

    private static Pista PistaPeligro(ObjetoMalditoData o) =>
        new Pista { tipo = TipoPista.Peligro, peligroMin = o.peligro, peligroMax = o.peligro };

    // Dos etiquetas posibles: una verdadera y una falsa, en orden aleatorio
    private static Pista PistaEtiquetaAmbigua(ObjetoMalditoData o, Random rng)
    {
        var falsas = new List<Etiqueta>();
        foreach (Etiqueta e in Enum.GetValues(typeof(Etiqueta)))
            if (!o.etiquetas.Contains(e)) falsas.Add(e);

        var etiquetas = new List<Etiqueta>();
        if (o.etiquetas.Count > 0) etiquetas.Add(o.etiquetas[rng.Next(o.etiquetas.Count)]);
        if (falsas.Count > 0) etiquetas.Add(falsas[rng.Next(falsas.Count)]);
        Mezclar(etiquetas, rng);

        return new Pista { tipo = TipoPista.Etiqueta, ambigua = true, etiquetas = etiquetas };
    }

    // Rango de tres valores que siempre contiene el peligro verdadero
    private static Pista PistaPeligroAmbigua(ObjetoMalditoData o, Random rng)
    {
        int minimo = Math.Max(0, o.peligro - rng.Next(3));
        int maximo = minimo + 2;
        if (maximo > 5)
        {
            maximo = 5;
            minimo = 3;
        }

        return new Pista { tipo = TipoPista.Peligro, ambigua = true, peligroMin = minimo, peligroMax = maximo };
    }

    private static void Mezclar<T>(IList<T> lista, Random rng)
    {
        for (int i = lista.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (lista[i], lista[j]) = (lista[j], lista[i]);
        }
    }
}