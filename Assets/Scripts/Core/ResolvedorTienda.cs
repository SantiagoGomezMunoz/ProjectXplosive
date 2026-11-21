using System.Collections.Generic;
using UnityEngine;

public class EfectoAplicado
{
    public ObjetoColocado a;
    public ObjetoColocado b;
    public ReglaInteraccion regla;
}

public class ResultadoTienda
{
    public List<EfectoAplicado> efectos = new();
    public Dictionary<ObjetoColocado, float> modificadorValor = new();
    public int peligroTotal;

    public int PrecioFinal(ObjetoColocado o)
    {
        modificadorValor.TryGetValue(o, out float mod);
        return Mathf.Max(0, Mathf.RoundToInt(o.data.precioBase * (1f + mod)));
    }
}

public static class ResolvedorTienda
{
    public static ResultadoTienda Resolver(GridTienda grid, TablaInteracciones tabla)
    {
        var resultado = new ResultadoTienda();
        var paresVistos = new HashSet<(Vector2Int, Vector2Int)>();

        foreach (ObjetoColocado a in grid.Todos)
        {
            resultado.peligroTotal += a.data.peligro;

            foreach (Vector2Int offset in a.data.aura)
            {
                if (!grid.TryObtener(a.celda + offset, out ObjetoColocado b) || b == a) continue;

                // Cada pareja cuenta una sola vez, aunque ambas auras se alcancen
                var clave = Ordenar(a.celda, b.celda);
                if (!paresVistos.Add(clave)) continue;

                foreach (Etiqueta ea in a.data.etiquetas)
                foreach (Etiqueta eb in b.data.etiquetas)
                {
                    if (!tabla.TryObtener(ea, eb, out ReglaInteraccion regla)) continue;

                    resultado.efectos.Add(new EfectoAplicado { a = a, b = b, regla = regla });
                    Sumar(resultado, a, regla.modificadorValor);
                    Sumar(resultado, b, regla.modificadorValor);
                    resultado.peligroTotal += regla.deltaPeligro;
                }
            }
        }

        resultado.peligroTotal = Mathf.Max(0, resultado.peligroTotal);
        return resultado;
    }

    private static void Sumar(ResultadoTienda r, ObjetoColocado o, float mod)
    {
        r.modificadorValor.TryGetValue(o, out float actual);
        r.modificadorValor[o] = actual + mod;
    }

    private static (Vector2Int, Vector2Int) Ordenar(Vector2Int p, Vector2Int q) =>
        (p.x < q.x || (p.x == q.x && p.y <= q.y)) ? (p, q) : (q, p);
}