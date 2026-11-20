using UnityEngine;
using UnityEngine.InputSystem;

public class ColocadorObjetos : MonoBehaviour
{
    [SerializeField] private Camera camara;
    [SerializeField] private ObjetoMalditoData[] catalogo;
    [SerializeField] private Vector2Int tamanoGrid = new(12, 8);
    [SerializeField] private Vector2 origen = Vector2.zero;

    private GridTienda grid;
    private SpriteRenderer fantasma;
    private int indiceActual;

    private void Awake()
    {
        grid = new GridTienda(tamanoGrid.x, tamanoGrid.y);
        if (camara == null) camara = Camera.main;

        fantasma = new GameObject("Fantasma").AddComponent<SpriteRenderer>();
        fantasma.sortingOrder = 100;
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
                colocado.vista = go;
            }
        }

        if (mouse.rightButton.wasPressedThisFrame && grid.Quitar(celda, out ObjetoColocado quitado))
        {
            Destroy(quitado.vista);
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
}