using UnityEngine;

public class ResumenCierre : MonoBehaviour
{
    [SerializeField] private GestorDia gestorDia;
    [SerializeField] private EstadoTienda tienda;
    [SerializeField] private ColocadorObjetos colocador;

    private static readonly string[] frasesSinVentas =
    {
        "Hoy entraron más corrientes de aire que clientes.",
        "Nadie compró nada. Los objetos, en cambio, estuvieron muy activos."
    };

    private static readonly string[] frasesNocheMala =
    {
        "Los objetos hicieron fiesta mientras tú intentabas vigilar.",
        "Mañana habrá que recordarles que la tienda no es un parque de diversiones."
    };

    private static readonly string[] frasesSustos =
    {
        "Un cliente salió corriendo. La puerta aún se está balanceando.",
        "Alguien juró no volver. Dice lo mismo todos los días."
    };

    private static readonly string[] frasesBuenDia =
    {
        "Todo en calma. Sospechosamente en calma.",
        "Hoy hasta los objetos se portaron bien. Desconfía."
    };

    private static readonly string[] frasesNormal =
    {
        "Un día más en la tienda. Nadie perdió nada importante.",
        "Ni bien ni mal: la tienda sigue en pie, y eso ya es bastante."
    };

    private bool construir;
    private bool visible;
    private string texto = "";
    private GUIStyle estilo;

    private void OnEnable() => gestorDia.FaseCambiada += AlCambiarFase;

    private void OnDisable()
    {
        if (gestorDia != null) gestorDia.FaseCambiada -= AlCambiarFase;
    }

    private void AlCambiarFase(FaseDia fase)
    {
        if (fase == FaseDia.Cierre)
        {
            construir = true; // se arma en Update, cuando todos los scripts ya registraron sus datos
            return;
        }

        construir = false;
        visible = false;
        if (fase == FaseDia.Manana && tienda != null) tienda.NuevoDia();
    }

    private void Update()
    {
        if (!construir) return;
        construir = false;
        texto = Construir();
        visible = true;
    }

    private string Construir()
    {
        int variacion = tienda.Reputacion - tienda.ReputacionInicioDia;
        string signo = variacion >= 0 ? "+" : "";

        return
            $"FIN DEL DÍA {gestorDia.Dia}\n\n" +
            $"Ventas: {tienda.VentasDia} (ingresos {tienda.IngresosDia})\n" +
            $"Clientes asustados: {tienda.SustosDia}\n" +
            $"Noche: {tienda.CalmadosNoche} calmados, {tienda.DescontroladosNoche} sin calmar\n" +
            $"Peligro de la tienda: {colocador.PeligroActual}\n" +
            $"Reputación: {tienda.Reputacion} ({signo}{variacion})\n" +
            $"Dinero: {tienda.Dinero}\n\n" +
            $"\"{ElegirFrase()}\"";
    }

    private string ElegirFrase()
    {
        string[] pool;

        if (tienda.VentasDia == 0) pool = frasesSinVentas;
        else if (tienda.DescontroladosNoche >= 3) pool = frasesNocheMala;
        else if (tienda.SustosDia >= 2) pool = frasesSustos;
        else if (tienda.VentasDia >= 3 && tienda.DescontroladosNoche == 0) pool = frasesBuenDia;
        else pool = frasesNormal;

        return pool[Random.Range(0, pool.Length)];
    }

    private void OnGUI()
    {
        if (!visible) return;

        if (estilo == null)
        {
            estilo = new GUIStyle(GUI.skin.box)
            {
                fontSize = 20,
                alignment = TextAnchor.UpperLeft,
                wordWrap = true,
                padding = new RectOffset(20, 20, 16, 16)
            };
        }

        float ancho = 560f;
        float alto = estilo.CalcHeight(new GUIContent(texto), ancho);
        GUI.Box(new Rect((Screen.width - ancho) / 2f, (Screen.height - alto) / 2f, ancho, alto), texto, estilo);
    }
}