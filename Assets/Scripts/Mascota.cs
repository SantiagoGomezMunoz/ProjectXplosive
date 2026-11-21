using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class Mascota : MonoBehaviour
{
    private enum Humor { Tranquila, Alerta, Asustada }

    [Header("Referencias")]
    [SerializeField] private ColocadorObjetos colocador;

    [Header("Sprites (solo el primero es obligatorio)")]
    [SerializeField] private Sprite spriteTranquila;
    [SerializeField] private Sprite spriteAlerta;
    [SerializeField] private Sprite spriteAsustada;

    [Header("Comportamiento")]
    [SerializeField] private float velocidad = 1.2f;
    [SerializeField] private int peligroAlerta = 6;
    [SerializeField] private int peligroAsustada = 15;
    [SerializeField] private int radioPaseo = 3;
    [SerializeField] private bool spriteMiraIzquierda = true;

    private SpriteRenderer render;
    private Humor humor = Humor.Tranquila;
    private Vector3 posicionBase;
    private Vector2Int celdaActual;
    private List<Vector2Int> ruta;
    private int indiceRuta;
    private float pausa;

    private void Start()
    {
        render = GetComponent<SpriteRenderer>();
        celdaActual = ElegirCeldaInicial();
        posicionBase = colocador.CeldaACentro(celdaActual);
        transform.position = posicionBase;
        pausa = Random.Range(1f, 3f);
    }

    private Vector2Int ElegirCeldaInicial()
    {
        GridTienda grid = colocador.Grid;
        for (int i = 0; i < 30; i++)
        {
            var c = new Vector2Int(Random.Range(0, grid.ancho), Random.Range(0, grid.alto));
            if (grid.EstaLibre(c)) return c;
        }
        return grid.puerta;
    }

    private void Update()
    {
        ActualizarHumor();

        bool caminando = false;

        if (humor != Humor.Asustada)
        {
            if (ruta != null && indiceRuta < ruta.Count)
            {
                caminando = true;
                Vector3 destino = colocador.CeldaACentro(ruta[indiceRuta]);
                Vector3 antes = posicionBase;
                posicionBase = Vector3.MoveTowards(posicionBase, destino, velocidad * Time.deltaTime);

                float dx = posicionBase.x - antes.x;
                if (Mathf.Abs(dx) > 0.0001f)
                    render.flipX = spriteMiraIzquierda ? dx > 0f : dx < 0f;

                if ((posicionBase - destino).sqrMagnitude < 0.0001f)
                {
                    celdaActual = ruta[indiceRuta];
                    indiceRuta++;
                }
            }
            else
            {
                pausa -= Time.deltaTime;
                if (pausa <= 0f) ElegirDestino();
            }
        }

        Animar(caminando);
    }

    private void ActualizarHumor()
    {
        int peligro = colocador.PeligroActual;
        humor = peligro >= peligroAsustada ? Humor.Asustada
              : peligro >= peligroAlerta ? Humor.Alerta
              : Humor.Tranquila;
    }

    private void ElegirDestino()
    {
        GridTienda grid = colocador.Grid;
        pausa = humor == Humor.Alerta ? Random.Range(4f, 8f) : Random.Range(2f, 5f);

        for (int i = 0; i < 15; i++)
        {
            var c = celdaActual + new Vector2Int(
                Random.Range(-radioPaseo, radioPaseo + 1),
                Random.Range(-radioPaseo, radioPaseo + 1));
            if (c == celdaActual || !grid.EstaLibre(c)) continue;

            List<Vector2Int> r = Pathfinding.BuscarRuta(grid, celdaActual, c);
            if (r == null) continue;

            ruta = r;
            indiceRuta = 0;
            return;
        }
    }

    private void Animar(bool caminando)
    {
        Sprite sprite = humor switch
        {
            Humor.Asustada => spriteAsustada != null ? spriteAsustada : spriteTranquila,
            Humor.Alerta => spriteAlerta != null ? spriteAlerta : spriteTranquila,
            _ => spriteTranquila
        };
        if (sprite != null) render.sprite = sprite;

        float escala = humor == Humor.Asustada ? 1.2f : humor == Humor.Alerta ? 1.12f : 1f;
        transform.localScale = new Vector3(escala, escala, 1f);

        Vector3 desplazamiento = Vector3.zero;
        if (humor == Humor.Asustada)
            desplazamiento = (Vector3)(Random.insideUnitCircle * 0.03f);
        else if (caminando)
            desplazamiento = new Vector3(0f, Mathf.Abs(Mathf.Sin(Time.time * 10f)) * 0.04f, 0f);

        transform.position = posicionBase + desplazamiento;
    }
}