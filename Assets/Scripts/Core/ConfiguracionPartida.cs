using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ObjetoInicial
{
    public ObjetoMalditoData objeto;
    [Min(1)] public int cantidad = 1;
}

[Serializable]
public class ConfigActo
{
    public string nombre;
    [Min(1)] public int dias = 15;
    public int alquilerDiario = 20;
    public int cuotaFinal; // se cobra el último día del acto
}

[CreateAssetMenu(menuName = "Tienda/Configuracion de partida", fileName = "ConfiguracionPartida")]
public class ConfiguracionPartida : ScriptableObject
{
    public ModoJuego modo = ModoJuego.Campana;
    public int dineroInicial = 100;
    [Header("Almacén")]
    public List<ObjetoInicial> almacenInicial = new();
    [Min(0)] public int suministroDiarioProvisional = 2;
    public List<ConfigActo> actos = new();

    [Header("Solo modo infinito")]
    public int alquilerInicialInfinito = 20;
    [Min(1)] public int diasPorSubidaInfinito = 10;
    public int subidaAlquilerInfinito = 5;

    public int TotalDias
    {
        get
        {
            if (modo == ModoJuego.Infinito) return int.MaxValue;
            int total = 0;
            foreach (ConfigActo a in actos) total += a.dias;
            return total;
        }
    }

    public int DeudaTotal
    {
        get
        {
            if (modo == ModoJuego.Infinito) return 0;
            int total = 0;
            foreach (ConfigActo a in actos) total += a.cuotaFinal;
            return total;
        }
    }

    // Índice del acto (desde 0) al que pertenece el día (desde 1); -1 si no existe
    public int IndiceActo(int dia)
    {
        if (modo == ModoJuego.Infinito || dia < 1) return -1;

        int acumulado = 0;
        for (int i = 0; i < actos.Count; i++)
        {
            acumulado += actos[i].dias;
            if (dia <= acumulado) return i;
        }
        return -1;
    }

    public bool EsUltimoDiaDeActo(int dia)
    {
        int indice = IndiceActo(dia);
        if (indice < 0) return false;

        int fin = 0;
        for (int i = 0; i <= indice; i++) fin += actos[i].dias;
        return dia == fin;
    }

    public int Alquiler(int dia)
    {
        if (dia < 1) return 0;

        if (modo == ModoJuego.Infinito)
            return alquilerInicialInfinito
                   + ((dia - 1) / Mathf.Max(1, diasPorSubidaInfinito)) * subidaAlquilerInfinito;

        int indice = IndiceActo(dia);
        return indice < 0 ? 0 : actos[indice].alquilerDiario;
    }

    public int CuotaDelDia(int dia)
    {
        if (!EsUltimoDiaDeActo(dia)) return 0;
        return actos[IndiceActo(dia)].cuotaFinal;
    }

    public bool EsUltimoDia(int dia) => modo == ModoJuego.Campana && dia == TotalDias;
}