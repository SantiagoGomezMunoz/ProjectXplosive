using System.Collections.Generic;
using UnityEngine;

public class ObjetoColocado
{
    public ObjetoMalditoData data;
    public Vector2Int celda;
    public GameObject vista;
}

public class GridTienda
{
    public readonly int ancho;
    public readonly int alto;
    private readonly Dictionary<Vector2Int, ObjetoColocado> celdas = new();
    public IEnumerable<ObjetoColocado> Todos => celdas.Values;
    public Vector2Int puerta = Vector2Int.zero;

    public GridTienda(int ancho, int alto)
    {
        this.ancho = ancho;
        this.alto = alto;
    }

    public bool EstaDentro(Vector2Int c) =>
        c.x >= 0 && c.y >= 0 && c.x < ancho && c.y < alto;

    public bool EsTransitable(Vector2Int c) => EstaDentro(c) && !celdas.ContainsKey(c);
    public bool EstaLibre(Vector2Int c) => EsTransitable(c) && c != puerta;
    

    public bool TryObtener(Vector2Int c, out ObjetoColocado objeto) =>
        celdas.TryGetValue(c, out objeto);

    public bool Colocar(ObjetoMalditoData data, Vector2Int c, out ObjetoColocado resultado)
    {
        resultado = null;
        if (!EstaLibre(c)) return false;
        resultado = new ObjetoColocado { data = data, celda = c };
        celdas[c] = resultado;
        return true;
    }

    public bool Quitar(Vector2Int c, out ObjetoColocado quitado)
    {
        if (!celdas.TryGetValue(c, out quitado)) return false;
        celdas.Remove(c);
        return true;
    }
    
}
