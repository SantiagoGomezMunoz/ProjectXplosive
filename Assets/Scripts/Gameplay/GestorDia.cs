using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

public enum FaseDia { Manana, Tarde, Noche, Cierre }

public class GestorDia : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Partida partida;

    [Header("Duración (segundos)")]
    [SerializeField] private float duracionManana = 60f;
    [SerializeField] private float duracionTarde = 90f;
    [SerializeField] private float duracionNoche = 60f;

    [Header("Ambiente")]
    [SerializeField] private Light2D luzGlobal;
    [SerializeField] private float velocidadTransicion = 2f;

    public FaseDia FaseActual { get; private set; } = FaseDia.Manana;
    public int Dia => partida.Estado.dia;
    public float TiempoRestante { get; private set; }
    public event Action<FaseDia> FaseCambiada;

    private Color colorObjetivo = Color.white;
    private float intensidadObjetivo = 1f;
    private float intensidadActual = 1f;
    private float parpadeoRestante;
    private GUIStyle estilo;

    private void Start()
    {
        if (luzGlobal != null) intensidadActual = luzGlobal.intensity;
        IniciarFase(FaseDia.Manana);
    }

    public void Parpadear(float duracion = 0.35f) => parpadeoRestante = duracion;

    private void Update()
    {
        if (luzGlobal != null)
        {
            float t = velocidadTransicion * Time.deltaTime;
            luzGlobal.color = Color.Lerp(luzGlobal.color, colorObjetivo, t);
            intensidadActual = Mathf.Lerp(intensidadActual, intensidadObjetivo, t);

            float factor = 1f;
            if (parpadeoRestante > 0f)
            {
                parpadeoRestante -= Time.deltaTime;
                if (parpadeoRestante > 0f)
                    factor = (Mathf.FloorToInt(parpadeoRestante * 14f) % 2 == 0) ? 0.25f : 1f;
            }
            luzGlobal.intensity = intensidadActual * factor;
        }

        Keyboard teclado = Keyboard.current;

        if (FaseActual == FaseDia.Cierre)
        {
            if (!partida.Estado.terminada && teclado != null && teclado.spaceKey.wasPressedThisFrame)
            {
                Economia.AvanzarDia(partida.Estado);
                IniciarFase(FaseDia.Manana);
            }
            return;
        }

        TiempoRestante -= Time.deltaTime;
        bool saltar = false;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        saltar = teclado != null && teclado.nKey.wasPressedThisFrame; // solo para pruebas
#endif
        if (TiempoRestante <= 0f || saltar) IniciarFase(Siguiente(FaseActual));
    }

    private void IniciarFase(FaseDia fase)
    {
        FaseActual = fase;
        TiempoRestante = DuracionDe(fase);

        switch (fase)
        {
            case FaseDia.Manana: colorObjetivo = new Color(1f, 0.95f, 0.8f);   intensidadObjetivo = 1f;   break;
            case FaseDia.Tarde:  colorObjetivo = new Color(1f, 0.7f, 0.45f);   intensidadObjetivo = 0.9f; break;
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
        UIEscala.Aplicar();

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
        string texto;
        if (cierre && partida.Estado.terminada)
            texto = $"Día {Dia} - Fin de la partida\nPulsa R para empezar de nuevo";
        else if (cierre)
            texto = $"Día {Dia} - Cierre\nPulsa ESPACIO para empezar el día {Dia + 1}";
        else
            texto = $"Día {Dia} - {nombre}  {Mathf.CeilToInt(TiempoRestante)} s";

        float ancho = 400f;
        GUI.Box(new Rect(UIEscala.Ancho - ancho - 10f, 10f, ancho, cierre ? 70f : 40f), texto, estilo);
    }
}