using System.Collections.Generic;
using UnityEngine;

public enum EstadoCliente { Entrando, Paseando, Mirando, Saliendo }

public class Cliente : MonoBehaviour
{
    public PerfilCliente Perfil { get; private set; }
    public ObjetoColocado Objetivo { get; private set; }
    public EstadoCliente Estado { get; private set; }
    public System.Action<Cliente> Termino;

    private ColocadorObjetos colocador;
    private EstadoTienda tienda;
    private List<Vector2Int> ruta;
    private int indiceRuta;
    private Vector2Int celdaActual;
    private float espera;
    private int paseosRestantes;

    public void Iniciar(PerfilCliente perfil, ColocadorObjetos colocador, EstadoTienda tienda, Material material)
    {
        Perfil = perfil;
        this.colocador = colocador;
        this.tienda = tienda;

        var sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = perfil.sprite;
        sr.color = perfil.tinte;
        sr.sortingOrder = 10;
        if (material != null) sr.sharedMaterial = material;

        celdaActual = colocador.Grid.puerta;
        transform.position = colocador.CeldaACentro(celdaActual);
        var voltear = gameObject.AddComponent<VoltearSegunMovimiento>();
        voltear.Configurar(perfil.spriteMiraIzquierda);
        ElegirObjetivo();
    }

    private void ElegirObjetivo()
    {
        ResultadoTienda estado = colocador.Evaluar();
        var candidatos = new List<ObjetoColocado>();

        foreach (ObjetoColocado o in colocador.Grid.Todos)
        {
            if (o.data.peligro < Perfil.peligroMinimoObjeto) continue;
            if (Perfil.soloSinInteracciones && TieneInteracciones(o, estado)) continue;
            candidatos.Add(o);
        }

        if (candidatos.Count == 0)
        {
            IniciarPaseo();
            return;
        }

        Objetivo = candidatos[Random.Range(0, candidatos.Count)];
        ruta = Pathfinding.BuscarRutaHastaAdyacente(colocador.Grid, celdaActual, Objetivo.celda);
        if (ruta == null)
        {
            IniciarPaseo();
            return;
        }

        indiceRuta = 0;
        Estado = EstadoCliente.Entrando;
    }

    private static bool TieneInteracciones(ObjetoColocado o, ResultadoTienda estado)
    {
        foreach (EfectoAplicado e in estado.efectos)
            if (e.a == o || e.b == o) return true;
        return false;
    }

    private void IniciarPaseo()
    {
        Estado = EstadoCliente.Paseando;
        paseosRestantes = Random.Range(2, 4); // 2 o 3 puntos
        ruta = null;
        indiceRuta = 0;
        espera = 0f;
    }

    private void Update()
    {
        switch (Estado)
        {
            case EstadoCliente.Entrando:
                if (Avanzar()) AlLlegar();
                break;

            case EstadoCliente.Paseando:
                if (Avanzar())
                {
                    espera -= Time.deltaTime; // pausa al llegar a cada punto
                    if (espera <= 0f) SiguientePaseo();
                }
                break;

            case EstadoCliente.Mirando:
                espera -= Time.deltaTime;
                if (espera <= 0f) Comprar();
                break;

            case EstadoCliente.Saliendo:
                if (Avanzar()) Terminar();
                break;
        }
    }

    private void SiguientePaseo()
    {
        if (paseosRestantes <= 0)
        {
            Decir($"{Perfil.nombre} no encontró nada de su gusto y se va.");
            Salir();
            return;
        }

        paseosRestantes--;
        GridTienda grid = colocador.Grid;

        for (int intento = 0; intento < 20; intento++)
        {
            var c = new Vector2Int(Random.Range(0, grid.ancho), Random.Range(0, grid.alto));
            if (c == celdaActual || !grid.EsTransitable(c)) continue;

            List<Vector2Int> r = Pathfinding.BuscarRuta(grid, celdaActual, c);
            if (r == null) continue;

            ruta = r;
            indiceRuta = 0;
            espera = 0.6f;
            return;
        }

        // No hay a dónde pasear: se va en el siguiente ciclo
        paseosRestantes = 0;
        ruta = null;
        indiceRuta = 0;
        espera = 0f;
    }

    private void AlLlegar()
    {
        ResultadoTienda estado = colocador.Evaluar();

        if (Perfil.umbralMiedo > 0)
            Debug.Log($"[{Perfil.nombre}] peligro={estado.peligroTotal}, umbral={Perfil.umbralMiedo}");

        // Miedo: la tienda está demasiado maldita
        if (Perfil.umbralMiedo > 0 && estado.peligroTotal > Perfil.umbralMiedo)
        {
            if (tienda != null) tienda.RegistrarSusto(Perfil.nombre);
            Salir();
            return;
        }

        // Otro cliente pudo llevarse el objeto antes
        if (!EsObjetivoVigente())
        {
            Decir($"{Perfil.nombre} llegó tarde: alguien se llevó {Objetivo.data.nombre}.");
            Salir();
            return;
        }

        Estado = EstadoCliente.Mirando;
        espera = 1.5f;
    }

    private void Comprar()
    {
        if (!EsObjetivoVigente())
        {
            Salir();
            return;
        }

        ResultadoTienda estado = colocador.Evaluar();
        int precio = Mathf.RoundToInt(estado.PrecioFinal(Objetivo) * Perfil.multiplicadorPrecio);

        colocador.Vender(Objetivo);
        if (tienda != null) tienda.RegistrarVenta(precio, Perfil.nombre, Objetivo.data.nombre);
        Salir();
    }

    private bool EsObjetivoVigente() =>
        Objetivo != null
        && colocador.Grid.TryObtener(Objetivo.celda, out ObjetoColocado actual)
        && actual == Objetivo;

    public void Salir()
    {
        if (Estado == EstadoCliente.Saliendo) return;

        ruta = Pathfinding.BuscarRuta(colocador.Grid, celdaActual, colocador.Grid.puerta);
        indiceRuta = 0;
        Estado = EstadoCliente.Saliendo;
        if (ruta == null) Terminar();
    }

    private void Terminar()
    {
        Termino?.Invoke(this);
        Destroy(gameObject);
    }

    private void Decir(string texto)
    {
        if (tienda != null) tienda.Mensaje(texto);
        else Debug.Log(texto);
    }

    // Devuelve true cuando llegó al final de la ruta
    private bool Avanzar()
    {
        if (ruta == null || indiceRuta >= ruta.Count) return true;

        Vector3 destino = colocador.CeldaACentro(ruta[indiceRuta]);
        transform.position = Vector3.MoveTowards(
            transform.position, destino, Perfil.velocidad * Time.deltaTime);

        if ((transform.position - destino).sqrMagnitude < 0.0001f)
        {
            celdaActual = ruta[indiceRuta];
            indiceRuta++;
        }
        return indiceRuta >= ruta.Count;
    }
}