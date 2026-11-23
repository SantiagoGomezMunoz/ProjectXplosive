using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

public class ColocadorObjetos : MonoBehaviour
{
    private struct EtiquetaFlotante
    {
        public Vector3 posicion;
        public string texto;
        public bool bueno;
        public bool arriba;
        public int nivel;
        public Vector2Int celda;
    }

    [SerializeField] private Camera camara;
    [SerializeField] private CatalogoObjetos catalogo;
    [SerializeField] private Partida partida;
    [SerializeField] private Vector2Int tamanoGrid = new(12, 8);
    [SerializeField] private Vector2 origen = Vector2.zero;
    [SerializeField] private Sprite spriteCelda;
    [SerializeField] private TablaInteracciones tabla;
    [SerializeField] private GestorDia gestor;
    [SerializeField] private Vector2Int puerta = Vector2Int.zero;
    [SerializeField] private Material materialSprites;

    private readonly List<SpriteRenderer> marcasAura = new();
    private readonly List<EtiquetaFlotante> flotantes = new();
    private Transform contenedor;
    private GridTienda grid;
    private GameObject lineasCuadricula;
    private SpriteRenderer fantasma;
    private int indiceActual;
    private string textoPrevia = "";
    private Vector2Int ultimaCelda = new(int.MinValue, int.MinValue);
    private int ultimoIndice = -1;
    private bool sucio = true;
    private bool mostrarLeyenda = true;
    private GUIStyle estiloPanel;
    private GUIStyle estiloVerde;
    private GUIStyle estiloRojo;
    private GUIStyle estiloLeyenda;

    public GridTienda Grid => grid;
    public int PeligroActual { get; private set; }

    private void Awake()
    {
        grid = new GridTienda(tamanoGrid.x, tamanoGrid.y);
        grid.puerta = puerta;
        CrearCuadricula();

        var marcaPuerta = new GameObject("Puerta").AddComponent<SpriteRenderer>();
        marcaPuerta.sprite = spriteCelda;
        marcaPuerta.color = new Color(0.55f, 0.35f, 0.15f, 0.8f);
        marcaPuerta.sortingOrder = -5;
        if (materialSprites != null) marcaPuerta.sharedMaterial = materialSprites;
        marcaPuerta.transform.position = CeldaACentro(puerta);

        if (camara == null) camara = Camera.main;

        fantasma = new GameObject("Fantasma").AddComponent<SpriteRenderer>();
        fantasma.sortingOrder = 100;
        contenedor = new GameObject("Objetos colocados").transform;
    }

    private void Update()
    {
        Mouse mouse = Mouse.current;
        Keyboard teclado = Keyboard.current;
        if (mouse == null || teclado == null) return;

        if (teclado.hKey.wasPressedThisFrame) mostrarLeyenda = !mostrarLeyenda;

        bool puedeColocar = gestor == null || gestor.FaseActual == FaseDia.Manana;
        lineasCuadricula.SetActive(puedeColocar);

        if (!puedeColocar)
        {
            fantasma.gameObject.SetActive(false);
            OcultarAura();
            textoPrevia = "";
            flotantes.Clear();
            sucio = true;
            return;
        }

        List<EntradaAlmacen> almacen = partida.Estado.almacen;
        if (indiceActual >= almacen.Count) indiceActual = Mathf.Max(0, almacen.Count - 1);

        for (int i = 0; i < almacen.Count && i < 9; i++)
        {
            if (teclado[(Key)((int)Key.Digit1 + i)].wasPressedThisFrame) indiceActual = i;
        }

        ObjetoMalditoData elegido = almacen.Count > 0 ? catalogo.Buscar(almacen[indiceActual].id) : null;
        fantasma.sprite = elegido != null ? elegido.sprite : null;

        Vector2 pantalla = mouse.position.ReadValue();
        Vector3 mundo = camara.ScreenToWorldPoint(
            new Vector3(pantalla.x, pantalla.y, Mathf.Abs(camara.transform.position.z)));
        Vector2Int celda = Vector2Int.FloorToInt((Vector2)mundo - origen);

        grid.TryObtener(celda, out ObjetoColocado bajoCursor);
        bool valida = elegido != null && grid.EstaLibre(celda);

        // Sobre un objeto colocado se muestra su alcance; si no, el del objeto elegido
        ObjetoMalditoData datosAlcance = bajoCursor != null ? bajoCursor.data : elegido;
        if (datosAlcance != null) MostrarAura(datosAlcance, celda);
        else OcultarAura();

        fantasma.gameObject.SetActive(elegido != null && bajoCursor == null);
        fantasma.transform.position = CeldaACentro(celda);
        fantasma.color = valida
            ? new Color(0.6f, 1f, 0.6f, 0.7f)
            : new Color(1f, 0.4f, 0.4f, 0.7f);

        if (sucio || celda != ultimaCelda || indiceActual != ultimoIndice)
        {
            ActualizarInformacion(celda, valida, bajoCursor, elegido);
            ultimaCelda = celda;
            ultimoIndice = indiceActual;
            sucio = false;
        }

        if (mouse.leftButton.wasPressedThisFrame && valida) Colocar(elegido, celda);

        if (mouse.rightButton.wasPressedThisFrame && grid.Quitar(celda, out ObjetoColocado quitado))
        {
            Destroy(quitado.vista);
            partida.Estado.AgregarAlAlmacen(quitado.data.Id);
            Recalcular();
        }
    }

    private void Colocar(ObjetoMalditoData data, Vector2Int celda)
    {
        if (!partida.Estado.QuitarDelAlmacen(data.Id)) return;

        if (!grid.Colocar(data, celda, out ObjetoColocado colocado))
        {
            partida.Estado.AgregarAlAlmacen(data.Id);
            return;
        }

        var go = new GameObject(data.nombre);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = data.sprite;
        if (materialSprites != null) sr.sharedMaterial = materialSprites;
        go.transform.position = CeldaACentro(celda);
        go.transform.SetParent(contenedor);
        colocado.vista = go;
        Recalcular();
    }

    public Vector3 CeldaACentro(Vector2Int c) =>
        new Vector3(origen.x + c.x + 0.5f, origen.y + c.y + 0.5f, 0f);

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 1f, 1f, 0.25f);
        for (int x = 0; x <= tamanoGrid.x; x++)
            Gizmos.DrawLine(new Vector3(origen.x + x, origen.y), new Vector3(origen.x + x, origen.y + tamanoGrid.y));
        for (int y = 0; y <= tamanoGrid.y; y++)
            Gizmos.DrawLine(new Vector3(origen.x, origen.y + y), new Vector3(origen.x + tamanoGrid.x, origen.y + y));
    }

    // ---------- Alcance ----------

    private void MostrarAura(ObjetoMalditoData data, Vector2Int celda)
    {
        int n = data.aura.Count;
        while (marcasAura.Count < n) marcasAura.Add(CrearMarca());

        for (int i = 0; i < marcasAura.Count; i++)
        {
            SpriteRenderer marca = marcasAura[i];
            if (i >= n) { marca.gameObject.SetActive(false); continue; }

            Vector2Int c = celda + data.aura[i];
            bool dentro = grid.EstaDentro(c);
            marca.gameObject.SetActive(dentro);
            if (!dentro) continue;

            marca.transform.position = CeldaACentro(c);
            bool hayObjeto = grid.TryObtener(c, out _);
            marca.color = hayObjeto
                ? new Color(1f, 0.85f, 0.2f, 0.45f)   // amarillo: hay un objeto dentro
                : new Color(1f, 1f, 1f, 0.15f);       // tenue: solo alcance
        }
    }

    private SpriteRenderer CrearMarca()
    {
        var marca = new GameObject("MarcaAura").AddComponent<SpriteRenderer>();
        marca.sprite = spriteCelda;
        marca.sortingOrder = 50;
        marca.transform.SetParent(transform);
        return marca;
    }

    private void OcultarAura()
    {
        foreach (SpriteRenderer m in marcasAura) m.gameObject.SetActive(false);
    }

    // ---------- Cálculo y datos ----------

    private void Recalcular()
    {
        sucio = true;
        if (tabla == null) return;
        ResultadoTienda r = ResolvedorTienda.Resolver(grid, tabla);
        PeligroActual = r.peligroTotal;

        Debug.Log($"--- Peligro total: {r.peligroTotal} | Interacciones: {r.efectos.Count} ---");
        foreach (EfectoAplicado e in r.efectos)
            Debug.Log($"{e.a.data.nombre} + {e.b.data.nombre}: {e.regla.mensaje}");
        foreach (ObjetoColocado o in grid.Todos)
            Debug.Log($"{o.data.nombre} en {o.celda}: precio final {r.PrecioFinal(o)}");
    }

    public ResultadoTienda Evaluar() => ResolvedorTienda.Resolver(grid, tabla);

    public void Vender(ObjetoColocado objeto)
    {
        if (grid.Quitar(objeto.celda, out ObjetoColocado quitado) && quitado.vista != null)
            Destroy(quitado.vista);
        Recalcular();
    }

    public bool Mover(ObjetoColocado objeto, Vector2Int nuevaCelda)
    {
        if (!grid.Mover(objeto, nuevaCelda)) return false;
        if (objeto.vista != null) objeto.vista.transform.position = CeldaACentro(nuevaCelda);
        Recalcular();
        return true;
    }

    // ---------- Información en pantalla ----------

    private void ActualizarInformacion(Vector2Int celda, bool valida, ObjetoColocado bajoCursor, ObjetoMalditoData elegido)
    {
        flotantes.Clear();
        textoPrevia = "";
        if (tabla == null) return;

        if (bajoCursor != null)
        {
            textoPrevia = ConstruirFicha(bajoCursor);
        }
        else if (celda == puerta)
        {
            textoPrevia = "Puerta: por aquí entran los clientes.\nNo se puede colocar nada aquí.";
        }
        else if (elegido == null)
        {
            if (grid.EstaDentro(celda)) textoPrevia = "El almacén está vacío: no hay objetos para colocar.";
        }
        else if (valida)
        {
            textoPrevia = CalcularPrevia(elegido, celda);
        }
    }

    private string ConstruirFicha(ObjetoColocado objeto)
    {
        ResultadoTienda r = Evaluar();
        ObjetoMalditoData d = objeto.data;
        var sb = new StringBuilder();

        sb.AppendLine(d.nombre);
        sb.AppendLine($"Etiquetas: {NombresEtiquetas(d)}   Peligro propio: {d.peligro}");
        if (!string.IsNullOrEmpty(d.descripcion)) sb.AppendLine($"\"{d.descripcion}\"");
        sb.AppendLine($"Precio base: {d.precioBase}");

        bool hayEfectos = false;
        foreach (EfectoAplicado e in r.efectos)
        {
            ObjetoColocado otro = e.a == objeto ? e.b : (e.b == objeto ? e.a : null);
            if (otro == null) continue;

            hayEfectos = true;
            sb.AppendLine($"  con {otro.data.nombre}: {Describir(e.regla, ", ")}");
            AgregarFlotante(otro, e.regla);
        }

        if (!hayEfectos) sb.AppendLine("  Sin efectos con los objetos cercanos.");
        sb.AppendLine($"Precio final: {r.PrecioFinal(objeto)}");
        sb.Append("Clic derecho: guardar en el almacén");
        return sb.ToString();
    }

    private string CalcularPrevia(ObjetoMalditoData data, Vector2Int celda)
    {
        ResultadoTienda antes = ResolvedorTienda.Resolver(grid, tabla);

        grid.Colocar(data, celda, out ObjetoColocado temporal);
        ResultadoTienda despues = ResolvedorTienda.Resolver(grid, tabla);

        var sb = new StringBuilder();
        sb.AppendLine(data.nombre);
        sb.AppendLine($"Etiquetas: {NombresEtiquetas(data)}   Peligro propio: {data.peligro}");
        if (!string.IsNullOrEmpty(data.descripcion)) sb.AppendLine($"\"{data.descripcion}\"");
        sb.AppendLine($"Precio: {data.precioBase} -> {despues.PrecioFinal(temporal)}");
        sb.AppendLine($"Peligro de la tienda: {antes.peligroTotal} -> {despues.peligroTotal}");

        bool hayEfectos = false;
        foreach (EfectoAplicado e in despues.efectos)
        {
            ObjetoColocado otro = e.a == temporal ? e.b : (e.b == temporal ? e.a : null);
            if (otro == null) continue;

            hayEfectos = true;
            sb.AppendLine($"- {otro.data.nombre}: {Describir(e.regla, ", ")}");
            sb.AppendLine($"    {e.regla.mensaje}");
            AgregarFlotante(otro, e.regla);
        }

        if (!hayEfectos) sb.AppendLine("Sin efectos con los objetos cercanos.");
        sb.Append("Clic izquierdo: colocar");

        grid.Quitar(celda, out _);
        return sb.ToString();
    }

    // El valor va arriba de la casilla y el peligro abajo, fuera del dibujo del objeto
    private void AgregarFlotante(ObjetoColocado objetivo, ReglaInteraccion regla)
    {
        int porcentaje = Mathf.RoundToInt(regla.modificadorValor * 100f);
        if (porcentaje != 0)
            AgregarEtiqueta(objetivo.celda, $"{(porcentaje > 0 ? "+" : "")}{porcentaje} % valor", porcentaje > 0, true);

        if (regla.deltaPeligro != 0)
            AgregarEtiqueta(objetivo.celda, $"{(regla.deltaPeligro > 0 ? "+" : "")}{regla.deltaPeligro} peligro", regla.deltaPeligro < 0, false);
    }

    private void AgregarEtiqueta(Vector2Int celda, string texto, bool bueno, bool arriba)
    {
        int nivel = 0;
        foreach (EtiquetaFlotante f in flotantes)
            if (f.celda == celda && f.arriba == arriba) nivel++;

        flotantes.Add(new EtiquetaFlotante
        {
            celda = celda,
            arriba = arriba,
            nivel = nivel,
            texto = texto,
            bueno = bueno,
            posicion = CeldaACentro(celda) + Vector3.up * (arriba ? 0.5f : -0.5f)
        });
    }

    private static string Describir(ReglaInteraccion r, string separador)
    {
        var partes = new List<string>();
        int porcentaje = Mathf.RoundToInt(r.modificadorValor * 100f);
        if (porcentaje != 0) partes.Add($"{(porcentaje > 0 ? "+" : "")}{porcentaje} % valor");
        if (r.deltaPeligro != 0) partes.Add($"{(r.deltaPeligro > 0 ? "+" : "")}{r.deltaPeligro} peligro");
        return partes.Count > 0 ? string.Join(separador, partes) : "sin efecto";
    }

    private static string NombresEtiquetas(ObjetoMalditoData d)
    {
        var nombres = new List<string>();
        foreach (Etiqueta e in d.etiquetas) nombres.Add(NombreEtiqueta(e));
        return nombres.Count > 0 ? string.Join(", ", nombres) : "ninguna";
    }

    private static string NombreEtiqueta(Etiqueta e) => e switch
    {
        Etiqueta.Frio => "Frío",
        Etiqueta.Sueno => "Sueño",
        _ => e.ToString()
    };

    // ---------- Cuadrícula visible ----------

    private void CrearCuadricula()
    {
        lineasCuadricula = new GameObject("Cuadricula");
        var color = new Color(1f, 1f, 1f, 0.18f);
        float grosor = 1f / 32f; // un píxel

        for (int x = 0; x <= tamanoGrid.x; x++)
            CrearLinea(new Vector3(origen.x + x, origen.y + tamanoGrid.y / 2f, 0f),
                       new Vector3(grosor, tamanoGrid.y + grosor, 1f), color);

        for (int y = 0; y <= tamanoGrid.y; y++)
            CrearLinea(new Vector3(origen.x + tamanoGrid.x / 2f, origen.y + y, 0f),
                       new Vector3(tamanoGrid.x + grosor, grosor, 1f), color);
    }

    private void CrearLinea(Vector3 posicion, Vector3 escala, Color color)
    {
        var sr = new GameObject("Linea").AddComponent<SpriteRenderer>();
        sr.sprite = spriteCelda;
        sr.color = color;
        sr.sortingOrder = -4;
        sr.transform.SetParent(lineasCuadricula.transform);
        sr.transform.position = posicion;
        sr.transform.localScale = escala;
    }

    // ---------- Interfaz ----------

    private void PrepararEstilos()
    {
        if (estiloPanel != null) return;

        estiloPanel = new GUIStyle(GUI.skin.box)
        {
            fontSize = 16,
            alignment = TextAnchor.UpperLeft,
            wordWrap = true,
            padding = new RectOffset(10, 10, 8, 8)
        };

        estiloVerde = new GUIStyle(GUI.skin.box)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            padding = new RectOffset(6, 6, 2, 2)
        };
        estiloVerde.normal.textColor = new Color(0.55f, 1f, 0.55f);

        estiloRojo = new GUIStyle(estiloVerde);
        estiloRojo.normal.textColor = new Color(1f, 0.5f, 0.5f);

        estiloLeyenda = new GUIStyle(GUI.skin.box)
        {
            fontSize = 14,
            alignment = TextAnchor.UpperLeft,
            wordWrap = true,
            padding = new RectOffset(10, 10, 8, 8)
        };
    }

    private void OnGUI()
    {
        UIEscala.Aplicar();
        PrepararEstilos();

        if (!string.IsNullOrEmpty(textoPrevia))
        {
            var contenido = new GUIContent(textoPrevia);
            float alto = estiloPanel.CalcHeight(contenido, 420f);
            GUI.Box(new Rect(10, 10, 420f, alto), contenido, estiloPanel);
        }

        foreach (EtiquetaFlotante f in flotantes)
        {
            Vector3 p = camara.WorldToScreenPoint(f.posicion);
            float x = p.x / UIEscala.Factor;
            float y = (Screen.height - p.y) / UIEscala.Factor;

            GUIStyle estilo = f.bueno ? estiloVerde : estiloRojo;
            Vector2 tam = estilo.CalcSize(new GUIContent(f.texto));
            float desplazamiento = f.nivel * (tam.y + 2f);
            float arribaY = f.arriba ? y - tam.y - desplazamiento : y + desplazamiento;

            GUI.Box(new Rect(x - tam.x / 2f, arribaY, tam.x, tam.y), f.texto, estilo);
        }

        if (gestor == null || gestor.FaseActual == FaseDia.Manana)
        {
            DibujarAlmacen();
            DibujarLeyenda();
        }
    }

    private void DibujarAlmacen()
    {
        List<EntradaAlmacen> almacen = partida.Estado.almacen;
        var sb = new StringBuilder("ALMACÉN\n");

        if (almacen.Count == 0) sb.Append("(vacío)");

        for (int i = 0; i < almacen.Count && i < 9; i++)
        {
            ObjetoMalditoData d = catalogo.Buscar(almacen[i].id);
            string nombre = d != null ? d.nombre : almacen[i].id;
            sb.AppendLine($"{(i == indiceActual ? ">" : " ")} {i + 1}. {nombre} x{almacen[i].cantidad}");
        }

        string texto = sb.ToString().TrimEnd();
        float ancho = 300f;
        float alto = estiloLeyenda.CalcHeight(new GUIContent(texto), ancho);
        GUI.Box(new Rect(UIEscala.Ancho - ancho - 10f, 60f, ancho, alto), texto, estiloLeyenda);
    }

    private void DibujarLeyenda()
    {
        if (!mostrarLeyenda)
        {
            GUI.Box(new Rect(UIEscala.Ancho - 130f, UIEscala.Alto - 40f, 120f, 30f), "H: ayuda", estiloLeyenda);
            return;
        }

        string texto =
            "WASD: moverte    Teclas 1 a 9: elegir objeto del almacén\n" +
            "Clic izquierdo: colocar    Clic derecho: guardar en el almacén\n" +
            "Casillas tenues: alcance del objeto. Amarillo: hay otro objeto dentro, habrá efecto.\n" +
            "Cifras sobre los objetos: valor (arriba) y peligro (abajo).\n" +
            "Peligro alto: los clientes miedosos se van y de noche los objetos se inquietan.\n" +
            "H: ocultar ayuda";

        float ancho = 430f;
        float alto = estiloLeyenda.CalcHeight(new GUIContent(texto), ancho);
        GUI.Box(new Rect(UIEscala.Ancho - ancho - 10f, UIEscala.Alto - alto - 10f, ancho, alto), texto, estiloLeyenda);
    }
}