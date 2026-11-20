using UnityEngine;
using UnityEngine.InputSystem;

public class ColocadorObjetos : MonoBehaviour
{
    [SerializeField] private Camera camara;
    [SerializeField] private ObjetoMalditoData[] catalogo;
    [SerializeField] private Vector2Int tamanoGrid = new(12, 8);
    [SerializeField] private Vector2 origen = Vector2.zero;
    [SerializeField] private Sprite spriteCelda;
    [SerializeField] private TablaInteracciones tabla;

    private readonly System.Collections.Generic.List<SpriteRenderer> marcasAura = new();
    private Transform contenedor;
    private GridTienda grid;
    private SpriteRenderer fantasma;
    private int indiceActual;

    private void Awake()
    {
        grid = new GridTienda(tamanoGrid.x, tamanoGrid.y);
        if (camara == null) camara = Camera.main;

        fantasma = new GameObject("Fantasma").AddComponent<SpriteRenderer>();
        fantasma.sortingOrder = 100;
        contenedor = new GameObject("Objetos colocados").transform;
        ActualizarFantasma();
    }

    private void Update()
    {
        Mouse mouse = Mouse.current;
        Keyboard teclado = Keyboard.current;
        if (mouse == null || teclado == null || catalogo.Length == 0) return;

        for (int i = 0; i < catalogo.Length && i < 9; i++)
        {
            if (teclado[(Key)((int)Key.Digit1 + i)].wasPressedThisFrame)
            {
                indiceActual = i;
                ActualizarFantasma();
            }
        }

        Vector2 pantalla = mouse.position.ReadValue();
        Vector3 mundo = camara.ScreenToWorldPoint(
            new Vector3(pantalla.x, pantalla.y, Mathf.Abs(camara.transform.position.z)));
        Vector2Int celda = Vector2Int.FloorToInt((Vector2)mundo - origen);

        bool valida = grid.EstaLibre(celda);
        MostrarAura(catalogo[indiceActual], celda);
        fantasma.transform.position = CeldaACentro(celda);
        fantasma.color = valida
            ? new Color(0.6f, 1f, 0.6f, 0.7f)
            : new Color(1f, 0.4f, 0.4f, 0.7f);

        if (mouse.leftButton.wasPressedThisFrame && valida)
        {
            ObjetoMalditoData data = catalogo[indiceActual];
            if (grid.Colocar(data, celda, out ObjetoColocado colocado))
            {
                var go = new GameObject(data.nombre);
                go.AddComponent<SpriteRenderer>().sprite = data.sprite;
                go.transform.position = CeldaACentro(celda);
                go.transform.SetParent(contenedor);
                colocado.vista = go;
                Recalcular();
            }
        }

        if (mouse.rightButton.wasPressedThisFrame && grid.Quitar(celda, out ObjetoColocado quitado))
        {
            Destroy(quitado.vista);
            Recalcular();
        }
    }

    private void ActualizarFantasma() =>
        fantasma.sprite = catalogo.Length > 0 ? catalogo[indiceActual].sprite : null;

    private Vector3 CeldaACentro(Vector2Int c) =>
        new Vector3(origen.x + c.x + 0.5f, origen.y + c.y + 0.5f, 0f);

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 1f, 1f, 0.25f);
        for (int x = 0; x <= tamanoGrid.x; x++)
            Gizmos.DrawLine(new Vector3(origen.x + x, origen.y), new Vector3(origen.x + x, origen.y + tamanoGrid.y));
        for (int y = 0; y <= tamanoGrid.y; y++)
            Gizmos.DrawLine(new Vector3(origen.x, origen.y + y), new Vector3(origen.x + tamanoGrid.x, origen.y + y));
    }

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
                ? new Color(1f, 0.85f, 0.2f, 0.45f)   // amarillo: aquí habrá interacción
                : new Color(1f, 1f, 1f, 0.15f);       // blanco tenue: solo alcance
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

    private void Recalcular()
    {
        if (tabla == null) return;
        ResultadoTienda r = ResolvedorTienda.Resolver(grid, tabla);

        Debug.Log($"--- Peligro total: {r.peligroTotal} | Interacciones: {r.efectos.Count} ---");
        foreach (EfectoAplicado e in r.efectos)
            Debug.Log($"{e.a.data.nombre} + {e.b.data.nombre}: {e.regla.mensaje}");
        foreach (ObjetoColocado o in grid.Todos)
            Debug.Log($"{o.data.nombre} en {o.celda}: precio final {r.PrecioFinal(o)}");
    }
}