using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class GestorNoche : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private GestorDia gestorDia;
    [SerializeField] private ColocadorObjetos colocador;
    [SerializeField] private EstadoTienda tienda;
    [SerializeField] private Transform jugador;

    [Header("Ritmo de eventos")]
    [SerializeField] private float intervaloPeligroBajo = 12f;
    [SerializeField] private float intervaloPeligroAlto = 4f;
    [SerializeField] private int peligroMaximoReferencia = 25;
    [SerializeField] private float tiempoAviso = 3f;
    [SerializeField] private int maxSimultaneos = 2;

    [Header("Consecuencias")]
    [SerializeField] private float radioCalmar = 1.6f;
    [SerializeField] private int sinCalmarParaPenalizar = 3;

    private class Inquieto
    {
        public ObjetoColocado objeto;
        public float restante;
    }

    private readonly List<Inquieto> inquietos = new();
    private bool enNoche;
    private float temporizadorEvento;
    private float temporizadorParpadeo;
    private int calmados;
    private int sinCalmar;
    private GUIStyle estilo;

    private void OnEnable() => gestorDia.FaseCambiada += AlCambiarFase;

    private void OnDisable()
    {
        if (gestorDia != null) gestorDia.FaseCambiada -= AlCambiarFase;
    }

    private void AlCambiarFase(FaseDia fase)
    {
        if (fase == FaseDia.Noche)
        {
            enNoche = true;
            calmados = 0;
            sinCalmar = 0;
            temporizadorEvento = Mathf.Min(IntervaloActual(), 5f); // el primer aviso llega pronto
            temporizadorParpadeo = Random.Range(2f, 4f);
            return;
        }

        if (enNoche)
        {
            enNoche = false;
            TerminarNoche();
        }
    }

    private void Update()
    {
        if (!enNoche) return;

        // Parpadeo de la luz: más frecuente cuanto más peligro
        if (colocador.PeligroActual > 0)
        {
            temporizadorParpadeo -= Time.deltaTime;
            if (temporizadorParpadeo <= 0f)
            {
                gestorDia.Parpadear();
                temporizadorParpadeo = Random.Range(0.7f, 1.3f) * Mathf.Lerp(8f, 2.5f, Nivel());
            }
        }

        // Nuevos avisos
        temporizadorEvento -= Time.deltaTime;
        if (temporizadorEvento <= 0f)
        {
            temporizadorEvento = IntervaloActual();
            IniciarInquietud();
        }

        ActualizarInquietos();
    }

    private float Nivel() =>
        Mathf.InverseLerp(0f, peligroMaximoReferencia, colocador.PeligroActual);

    private float IntervaloActual() =>
        Mathf.Lerp(intervaloPeligroBajo, intervaloPeligroAlto, Nivel());

    private bool EstaInquieto(ObjetoColocado o)
    {
        foreach (Inquieto i in inquietos)
            if (i.objeto == o) return true;
        return false;
    }

    private void IniciarInquietud()
    {
        if (inquietos.Count >= maxSimultaneos) return;

        // Los objetos más peligrosos entran más veces en el sorteo
        var sorteo = new List<ObjetoColocado>();
        foreach (ObjetoColocado o in colocador.Grid.Todos)
        {
            if (o.data.peligro < 1 || EstaInquieto(o)) continue;
            for (int k = 0; k < o.data.peligro; k++) sorteo.Add(o);
        }
        if (sorteo.Count == 0) return;

        ObjetoColocado elegido = sorteo[Random.Range(0, sorteo.Count)];
        inquietos.Add(new Inquieto { objeto = elegido, restante = tiempoAviso });
        Mensaje($"{elegido.data.nombre} se inquieta...");
    }

    private void ActualizarInquietos()
    {
        Keyboard teclado = Keyboard.current;
        if (teclado != null && teclado.eKey.wasPressedThisFrame) IntentarCalmar();

        for (int i = inquietos.Count - 1; i >= 0; i--)
        {
            Inquieto inq = inquietos[i];
            inq.restante -= Time.deltaTime;
            Vibrar(inq);

            if (inq.restante <= 0f)
            {
                inquietos.RemoveAt(i);
                Descontrolar(inq.objeto);
            }
        }
    }

    private void IntentarCalmar()
    {
        if (jugador == null) return;

        Inquieto mejor = null;
        float mejorDistancia = radioCalmar;

        foreach (Inquieto inq in inquietos)
        {
            float d = Vector2.Distance(jugador.position, colocador.CeldaACentro(inq.objeto.celda));
            if (d <= mejorDistancia)
            {
                mejor = inq;
                mejorDistancia = d;
            }
        }

        if (mejor == null) return;

        Restaurar(mejor.objeto);
        inquietos.Remove(mejor);
        calmados++;
        Mensaje($"Calmaste {mejor.objeto.data.nombre}.");
    }

    private void Vibrar(Inquieto inq)
    {
        GameObject vista = inq.objeto.vista;
        if (vista == null) return;

        float avance = 1f - inq.restante / tiempoAviso;
        float intensidad = 0.04f + 0.04f * avance;
        Vector3 centro = colocador.CeldaACentro(inq.objeto.celda);
        vista.transform.position = centro + (Vector3)(Random.insideUnitCircle * intensidad);

        var sr = vista.GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.color = Color.Lerp(Color.white, new Color(1f, 0.35f, 0.35f), Mathf.PingPong(Time.time * 6f, 1f));
    }

    private void Restaurar(ObjetoColocado o)
    {
        if (o.vista == null) return;
        o.vista.transform.position = colocador.CeldaACentro(o.celda);
        var sr = o.vista.GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = Color.white;
    }

    private void Descontrolar(ObjetoColocado o)
    {
        sinCalmar++;
        Restaurar(o);

        var libres = new List<Vector2Int>();
        foreach (Vector2Int d in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
        {
            Vector2Int c = o.celda + d;
            if (colocador.Grid.EstaLibre(c)) libres.Add(c);
        }

        if (libres.Count == 0)
        {
            Mensaje($"{o.data.nombre} se agita, pero no tiene a dónde ir.");
            return;
        }

        colocador.Mover(o, libres[Random.Range(0, libres.Count)]);
        Mensaje($"{o.data.nombre} se movió sola...");
    }

    private void TerminarNoche()
    {
        foreach (Inquieto i in inquietos) Restaurar(i.objeto);
        inquietos.Clear();

        if (tienda != null) tienda.RegistrarNoche(calmados, sinCalmar);

        Mensaje($"La noche terminó: {calmados} calmados, {sinCalmar} sin calmar.");
        if (tienda != null && sinCalmar >= sinCalmarParaPenalizar)
            tienda.CambiarReputacion(-1, "Demasiados objetos se descontrolaron.");
    }
    
    private void Mensaje(string texto)
    {
        if (tienda != null) tienda.Mensaje(texto);
        else Debug.Log(texto);
    }

    private void OnGUI()
    {
        if (!enNoche || inquietos.Count == 0) return;

        if (estilo == null)
            estilo = new GUIStyle(GUI.skin.box) { fontSize = 18, alignment = TextAnchor.MiddleCenter };

        var sb = new System.Text.StringBuilder();
        foreach (Inquieto inq in inquietos)
            sb.AppendLine($"¡{inq.objeto.data.nombre} se inquieta! {Mathf.CeilToInt(inq.restante)} s - acércate y pulsa E");

        float ancho = 560f;
        float alto = inquietos.Count * 26f + 12f;
        GUI.Box(new Rect(Screen.width - ancho - 10f, 60f, ancho, alto), sb.ToString().TrimEnd(), estilo);
    }
}