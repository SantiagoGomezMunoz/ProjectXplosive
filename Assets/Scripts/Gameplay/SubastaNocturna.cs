using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

public class SubastaNocturna : MonoBehaviour
{
    private enum Paso { Inactiva, Preparando, Ronda, Resultado }

    [Header("Referencias")]
    [SerializeField] private GestorDia gestorDia;
    [SerializeField] private Partida partida;
    [SerializeField] private CatalogoObjetos catalogo;
    [SerializeField] private EstadoTienda tienda;
    [SerializeField] private ColocadorObjetos colocador;
    [SerializeField] private PerfilCliente[] rivales;

    [Header("Tiempos (segundos)")]
    [SerializeField] private float segundosPrimeraRonda = 8f;
    [SerializeField] private float segundosPorRonda = 3f;
    [SerializeField] private float segundosResultado = 3.5f;

    private readonly System.Random rng = new();
    private List<Lote> lotes = new();
    private SubastaLote subasta;
    private Paso paso = Paso.Inactiva;
    private CalidadPistas calidad;
    private int indice;
    private float temporizador;
    private float duracionRonda;
    private string textoResultado = "";
    private bool revelado;

    private GUIStyle estiloCaja;
    private GUIStyle estiloTitulo;
    private GUIStyle estiloTexto;
    private GUIStyle estiloPequeno;

    private void OnEnable() => gestorDia.FaseCambiada += AlCambiarFase;

    private void OnDisable()
    {
        if (gestorDia != null) gestorDia.FaseCambiada -= AlCambiarFase;
    }

    private void AlCambiarFase(FaseDia fase)
    {
        // Los lotes se preparan en Update, cuando la vigilia ya registró su resultado
        paso = fase == FaseDia.Subasta ? Paso.Preparando : Paso.Inactiva;
    }

    private void Update()
    {
        switch (paso)
        {
            case Paso.Preparando: Preparar(); break;
            case Paso.Ronda: ActualizarRonda(); break;
            case Paso.Resultado: ActualizarResultado(); break;
        }
    }

    // ---------- Flujo ----------

    private void Preparar()
    {
        int reputacion = partida.Estado.reputacion;
        calidad = GeneradorSubasta.CalcularCalidad(
            tienda.DescontroladosNoche, partida.Config.PistasPeores(reputacion));

        lotes = GeneradorSubasta.GenerarLotes(
            catalogo.objetos,
            partida.Config.lotesPorNoche,
            partida.Config.LotesExtra(reputacion),
            calidad,
            partida.Config.factorPrecioSalida,
            rng);

        indice = -1;
        tienda.Mensaje($"Se abre la subasta: {lotes.Count} lotes. Pistas {NombreCalidad(calidad)}.");
        SiguienteLote();
    }

    private void SiguienteLote()
    {
        indice++;
        if (indice >= lotes.Count)
        {
            Terminar();
            return;
        }

        subasta = new SubastaLote(
            lotes[indice], rivales, partida.Config.incrementoPuja, colocador.PeligroActual);
        revelado = false;

        // Si no alcanza ni para el precio de salida, los rivales se la disputan sin el jugador
        if (partida.Estado.dinero < subasta.PrecioActual)
        {
            subasta.ResolverRestante();
            CerrarLote("No te alcanza el dinero para este lote. ");
            return;
        }

        duracionRonda = segundosPrimeraRonda;
        temporizador = duracionRonda;
        paso = Paso.Ronda;
    }

    private void ActualizarRonda()
    {
        Keyboard teclado = Keyboard.current;
        bool pujar = teclado != null && teclado.spaceKey.wasPressedThisFrame;
        bool pasar = teclado != null && teclado.pKey.wasPressedThisFrame;

        temporizador -= Time.deltaTime;
        if (temporizador <= 0f) pasar = true; // tiempo agotado = pasar

        if (!pujar && !pasar) return;

        subasta.Avanzar(pujar && !pasar, partida.Estado.dinero);

        if (subasta.Estado == EstadoSubasta.EnCurso && !subasta.JugadorActivo)
            subasta.ResolverRestante();

        if (subasta.Estado != EstadoSubasta.EnCurso)
        {
            CerrarLote("");
            return;
        }

        duracionRonda = segundosPorRonda;
        temporizador = duracionRonda;
    }

    private void CerrarLote(string prefijo)
    {
        ObjetoMalditoData objeto = subasta.Lote.objeto;

        switch (subasta.Estado)
        {
            case EstadoSubasta.GanaJugador:
                tienda.RegistrarCompra(subasta.PrecioFinal);
                partida.Estado.AgregarAlAlmacen(objeto.Id);
                revelado = true;
                textoResultado =
                    $"¡Ganaste! Era {objeto.nombre} ({NombresEtiquetas(objeto)}; peligro {objeto.peligro}). " +
                    $"Pagaste {subasta.PrecioFinal} y va al almacén.";
                break;

            case EstadoSubasta.GanaRival:
                textoResultado = $"{prefijo}{subasta.GanadorRival.nombre} se lo llevó por {subasta.PrecioFinal}.";
                break;

            default:
                textoResultado = $"{prefijo}Nadie quiso el lote.";
                break;
        }

        tienda.Mensaje(textoResultado);
        temporizador = segundosResultado;
        paso = Paso.Resultado;
    }

    private void ActualizarResultado()
    {
        Keyboard teclado = Keyboard.current;
        temporizador -= Time.deltaTime;

        bool saltar = teclado != null && teclado.spaceKey.wasPressedThisFrame;
        if (temporizador <= 0f || saltar) SiguienteLote();
    }

    private void Terminar()
    {
        paso = Paso.Inactiva;
        gestorDia.FinalizarFase();
    }

    // ---------- Textos ----------

    private static string NombreCalidad(CalidadPistas c) => c switch
    {
        CalidadPistas.Claras => "claras",
        CalidadPistas.Parciales => "parciales",
        _ => "confusas"
    };

    private static string NombreEtiqueta(Etiqueta e) => e switch
    {
        Etiqueta.Frio => "Frío",
        Etiqueta.Sueno => "Sueño",
        _ => e.ToString()
    };

    private static string NombresEtiquetas(ObjetoMalditoData o)
    {
        var nombres = new List<string>();
        foreach (Etiqueta e in o.etiquetas) nombres.Add(NombreEtiqueta(e));
        return nombres.Count > 0 ? string.Join(", ", nombres) : "sin etiquetas";
    }

    private static string TextoPista(Pista p)
    {
        if (p.tipo == TipoPista.Etiqueta)
        {
            var nombres = new List<string>();
            foreach (Etiqueta e in p.etiquetas) nombres.Add(NombreEtiqueta(e));

            return p.ambigua
                ? $"Etiqueta: {string.Join(" o ", nombres)} (no estás seguro)"
                : $"Etiqueta: {string.Join(", ", nombres)}";
        }

        return p.ambigua
            ? $"Peligro: entre {p.peligroMin} y {p.peligroMax}"
            : $"Peligro: {p.peligroMin}";
    }

    // ---------- Interfaz ----------

    private void PrepararEstilos()
    {
        if (estiloCaja != null) return;

        estiloCaja = new GUIStyle(GUI.skin.box);

        estiloTitulo = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold };

        estiloTexto = new GUIStyle(GUI.skin.label) { fontSize = 17, wordWrap = true };

        estiloPequeno = new GUIStyle(GUI.skin.label) { fontSize = 14, wordWrap = true };
    }

    private void OnGUI()
    {
        if ((paso != Paso.Ronda && paso != Paso.Resultado) || subasta == null) return;

        UIEscala.Aplicar();
        PrepararEstilos();

        float ancho = 680f;
        float alto = 340f;
        var caja = new Rect((UIEscala.Ancho - ancho) / 2f, (UIEscala.Alto - alto) / 2f - 20f, ancho, alto);
        GUI.Box(caja, GUIContent.none, estiloCaja);

        Lote lote = subasta.Lote;
        string titulo = $"SUBASTA NOCTURNA - Lote {indice + 1} de {lotes.Count}";
        if (lote.esExtra) titulo += "  (lote extra de mayor calidad)";
        GUI.Label(new Rect(caja.x + 20f, caja.y + 12f, ancho - 40f, 28f), titulo, estiloTitulo);

        DibujarObjeto(new Rect(caja.x + 24f, caja.y + 56f, 120f, 120f), lote.objeto, !revelado);

        var pistas = new StringBuilder($"Pistas ({NombreCalidad(calidad)}):\n");
        foreach (Pista p in lote.pistas) pistas.AppendLine("- " + TextoPista(p));
        GUI.Label(new Rect(caja.x + 164f, caja.y + 52f, ancho - 184f, 130f), pistas.ToString().TrimEnd(), estiloTexto);

        GUI.Label(new Rect(caja.x + 20f, caja.y + 194f, ancho - 40f, 26f),
            $"Precio actual: {subasta.PrecioActual}    Siguiente: {subasta.PrecioSiguiente}    Tu dinero: {partida.Estado.dinero}",
            estiloTexto);

        string pujando = subasta.RivalesActivos.Count == 0
            ? "ninguno"
            : string.Join(", ", NombresRivales());
        GUI.Label(new Rect(caja.x + 20f, caja.y + 222f, ancho - 40f, 26f),
            $"Rivales pujando: {pujando}", estiloTexto);

        if (paso == Paso.Ronda)
        {
            float fraccion = duracionRonda > 0f ? Mathf.Clamp01(temporizador / duracionRonda) : 0f;
            DibujarBarra(new Rect(caja.x + 20f, caja.y + 258f, ancho - 40f, 10f), fraccion);

            string accion = partida.Estado.dinero >= subasta.PrecioSiguiente
                ? $"ESPACIO: pujar a {subasta.PrecioSiguiente}"
                : $"ESPACIO: mantener {subasta.PrecioActual}";
            GUI.Label(new Rect(caja.x + 20f, caja.y + 278f, ancho - 40f, 24f),
                $"{accion}      P: pasar      (si se acaba el tiempo, pasas)", estiloPequeno);
        }
        else
        {
            GUI.Label(new Rect(caja.x + 20f, caja.y + 252f, ancho - 40f, 56f), textoResultado, estiloTexto);
            GUI.Label(new Rect(caja.x + 20f, caja.y + 312f, ancho - 40f, 24f),
                "ESPACIO: siguiente lote", estiloPequeno);
        }
    }

    private IEnumerable<string> NombresRivales()
    {
        foreach (PerfilCliente r in subasta.RivalesActivos) yield return r.nombre;
    }

    private static void DibujarBarra(Rect rect, float fraccion)
    {
        Color anterior = GUI.color;

        GUI.color = new Color(0.2f, 0.2f, 0.25f, 1f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);

        GUI.color = new Color(0.95f, 0.8f, 0.25f, 1f);
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width * fraccion, rect.height), Texture2D.whiteTexture);

        GUI.color = anterior;
    }

    // Silueta negra mientras no se haya ganado el lote; con color al revelarlo
    private static void DibujarObjeto(Rect rect, ObjetoMalditoData objeto, bool silueta)
    {
        Color anterior = GUI.color;

        GUI.color = new Color(0.72f, 0.72f, 0.78f, 1f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);

        Sprite sprite = objeto.sprite;
        if (sprite != null)
        {
            Texture2D textura = sprite.texture;
            Rect t = sprite.textureRect;
            var uv = new Rect(t.x / textura.width, t.y / textura.height, t.width / textura.width, t.height / textura.height);

            GUI.color = silueta ? Color.black : Color.white;
            GUI.DrawTextureWithTexCoords(rect, textura, uv, true);
        }

        GUI.color = anterior;
    }
}