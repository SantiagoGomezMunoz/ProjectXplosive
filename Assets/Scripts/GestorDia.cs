using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

public enum FaseDia { Manana, Tarde, Noche, Cierre }

public class GestorDia : MonoBehaviour
{
    [Header("Duración (segundos)")]
    [SerializeField] private float duracionManana = 60f;
    [SerializeField] private float duracionTarde = 90f;
    [SerializeField] private float duracionNoche = 60f;

    [Header("Ambiente")]
    [SerializeField] private Light2D luzGlobal;
    [SerializeField] private float velocidadTransicion = 2f;

    public FaseDia FaseActual { get; private set; } = FaseDia.Manana;
    public int Dia { get; private set; } = 1;
    public float TiempoRestante { get; private set; }
    public event Action<FaseDia> FaseCambiada;

    private Color colorObjetivo = Color.white;
    private float intensidadObjetivo = 1f;
    private GUIStyle estilo;

    private void Start() => IniciarFase(FaseDia.Manana);

    private void Update()
    {
        if (luzGlobal != null)
        {
            float t = velocidadTransicion * Time.deltaTime;
            luzGlobal.color = Color.Lerp(luzGlobal.color, colorObjetivo, t);
            luzGlobal.intensity = Mathf.Lerp(luzGlobal.intensity, intensidadObjetivo, t);
        }

        Keyboard teclado = Keyboard.current;

        if (FaseActual == FaseDia.Cierre)
        {
            if (teclado != null && teclado.spaceKey.wasPressedThisFrame)
            {
                Dia++;
                IniciarFase(FaseDia.Manana);
            }
            return;
        }

        TiempoRestante -= Time.deltaTime;
        bool saltar = teclado != null && teclado.nKey.wasPressedThisFrame; // solo para pruebas
        if (TiempoRestante <= 0f || saltar) IniciarFase(Siguiente(FaseActual));
    }

    private void IniciarFase(FaseDia fase)
    {
        FaseActual = fase;
        TiempoRestante = DuracionDe(fase);

        switch (fase)
        {
            case FaseDia.Manana: colorObjetivo = new Color(1f, 0.95f, 0.8f);  intensidadObjetivo = 1f;   break;
            case FaseDia.Tarde:  colorObjetivo = new Color(1f, 0.7f, 0.45f);  intensidadObjetivo = 0.9f; break;
            case FaseDia.Noche:  colorObjetivo = new Color(0.25f, 0.3f, 0.6f); intensidadObjetivo = 0.6f; break;
            case FaseDia.Cierre: colorObjetivo = new Color(0.6f, 0.6f, 0.75f); intensidadObjetivo = 0.8f; break;
        }

        FaseCambiada?.Invoke(fase);
    }

    private float DuracionDe(FaseDia f) => f switch
    {
        FaseDia.Manana => duracionManana,
        FaseDia.Tarde => duracionTarde,
        FaseDia.Noche => duracionNoche,
        _ => 0f
    };

    private static FaseDia Siguiente(FaseDia f) => f switch
    {
        FaseDia.Manana => FaseDia.Tarde,
        FaseDia.Tarde => FaseDia.Noche,
        _ => FaseDia.Cierre
    };

    private void OnGUI()
    {
        if (estilo == null)
        {
            estilo = new GUIStyle(GUI.skin.box) { fontSize = 20, alignment = TextAnchor.MiddleCenter };
        }

        string nombre = FaseActual switch
        {
            FaseDia.Manana => "Mañana",
            FaseDia.Tarde => "Tarde",
            FaseDia.Noche => "Noche",
            _ => "Cierre"
        };

        bool cierre = FaseActual == FaseDia.Cierre;
        string texto = cierre
            ? $"Día {Dia} - Cierre\nPulsa ESPACIO para empezar el día {Dia + 1}"
            : $"Día {Dia} - {nombre}  {Mathf.CeilToInt(TiempoRestante)} s";

        float ancho = 400f;
        GUI.Box(new Rect(Screen.width - ancho - 10f, 10f, ancho, cierre ? 70f : 40f), texto, estilo);
    }
}