using UnityEngine;

public class ResumenCierre : MonoBehaviour
{
    [SerializeField] private GestorDia gestorDia;
    [SerializeField] private EstadoTienda tienda;
    [SerializeField] private ColocadorObjetos colocador;
    [SerializeField] private Partida partida;
    [SerializeField] private CierreDia cierre;

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
        ResultadoCierre r = cierre.UltimoResultado;
        EstadoPartida estado = partida.Estado;
        int variacion = tienda.Reputacion - tienda.ReputacionInicioDia;
        string signo = variacion >= 0 ? "+" : "";

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"FIN DEL DÍA {r.dia}");
        sb.AppendLine();
        sb.AppendLine($"Ventas: {tienda.VentasDia} (ingresos {tienda.IngresosDia})");
        sb.AppendLine($"Clientes asustados: {tienda.SustosDia}");
        sb.AppendLine($"Noche: {tienda.CalmadosNoche} calmados, {tienda.DescontroladosNoche} sin calmar");
        sb.AppendLine($"Peligro de la tienda: {colocador.PeligroActual}");
        sb.AppendLine(r.alquilerPagado
            ? $"Alquiler: -{r.alquiler}"
            : $"Alquiler: {r.alquiler} (no se pudo pagar)");

        if (r.cuota > 0)
            sb.AppendLine(r.cuotaPagada
                ? $"Cuota de la deuda: -{r.cuota}"
                : $"Cuota de la deuda: {r.cuota} (no se pudo pagar)");

        if (cierre.NombresEmbargados.Count > 0)
            sb.AppendLine($"Embargados: {string.Join(", ", cierre.NombresEmbargados)}");

        if (estado.modo == ModoJuego.Campana)
            sb.AppendLine($"Deuda restante: {estado.deudaRestante}");

        string tramo = partida.Config.TramoPara(tienda.Reputacion)?.nombre;
        string repTexto = string.IsNullOrEmpty(tramo) ? $"{tienda.Reputacion}" : $"{tienda.Reputacion} ({tramo})";
        sb.AppendLine($"Reputación: {repTexto} ({signo}{variacion})");
        sb.AppendLine($"Dinero: {tienda.Dinero}");
        sb.AppendLine();
        sb.Append(Final());
        return sb.ToString();
    }

    private string Final()
    {
        EstadoPartida e = partida.Estado;

        if (e.terminada && e.victoria)
            return "\"¡Pagaste la deuda! La tienda es tuya.\"\n\nPulsa R para empezar una nueva partida.";

        if (e.terminada)
            return $"\"Quiebra: no pudiste pagar. Días completos: {e.dia - 1}.\"\n\nPulsa R para empezar una nueva partida.";

        return $"\"{ElegirFrase()}\"";
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

        UIEscala.Aplicar();

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
        GUI.Box(new Rect((UIEscala.Ancho - ancho) / 2f, (UIEscala.Alto - alto) / 2f, ancho, alto), texto, estilo);
    }
}