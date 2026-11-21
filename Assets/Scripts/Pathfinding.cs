using System;
using System.Collections.Generic;
using UnityEngine;

public static class Pathfinding
{
    private static readonly Vector2Int[] Vecinos =
        { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

    public static List<Vector2Int> BuscarRuta(GridTienda grid, Vector2Int inicio, Vector2Int destino) =>
        Buscar(grid, inicio, c => c == destino);

    public static List<Vector2Int> BuscarRutaHastaAdyacente(GridTienda grid, Vector2Int inicio, Vector2Int objetivo) =>
        Buscar(grid, inicio, c => Mathf.Abs(c.x - objetivo.x) + Mathf.Abs(c.y - objetivo.y) == 1);

    private static List<Vector2Int> Buscar(GridTienda grid, Vector2Int inicio, Func<Vector2Int, bool> esMeta)
    {
        var anterior = new Dictionary<Vector2Int, Vector2Int>();
        var cola = new Queue<Vector2Int>();
        cola.Enqueue(inicio);
        anterior[inicio] = inicio;

        while (cola.Count > 0)
        {
            Vector2Int actual = cola.Dequeue();
            if (esMeta(actual)) return Reconstruir(anterior, inicio, actual);

            foreach (Vector2Int d in Vecinos)
            {
                Vector2Int siguiente = actual + d;
                if (anterior.ContainsKey(siguiente) || !grid.EsTransitable(siguiente)) continue;
                anterior[siguiente] = actual;
                cola.Enqueue(siguiente);
            }
        }
        return null; // no hay camino
    }

    private static List<Vector2Int> Reconstruir(
        Dictionary<Vector2Int, Vector2Int> anterior, Vector2Int inicio, Vector2Int fin)
    {
        var ruta = new List<Vector2Int>();
        Vector2Int c = fin;
        while (c != inicio) { ruta.Add(c); c = anterior[c]; }
        ruta.Reverse();
        return ruta;
    }
}