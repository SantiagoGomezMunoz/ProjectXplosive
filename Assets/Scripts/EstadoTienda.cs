using System.Collections.Generic;
using UnityEngine;

public class EstadoTienda : MonoBehaviour
{
    [SerializeField] private ColocadorObjetos colocador;
    [SerializeField] private int reputacionPorVenta = 1;
    [SerializeField] private int reputacionPorSusto = -1;
    [SerializeField] private int mensajesVisibles = 4;

    public int Dinero { get; private set; }
    public int Reputacion { get; private set; }

    // Contadores del día (para el resumen del cierre)
    public int VentasDia { get; private set; }
    public int IngresosDia { get; private set; }
    public int SustosDia { get; private set; }
    public int CalmadosNoche { get; private set; }
    public int DescontroladosNoche { get; private set; }
    public int ReputacionInicioDia { get; private set; }

    private readonly Queue<string> mensajes = new();
    private GUIStyle estiloPanel;
    private GUIStyle estiloMensajes;

    public void NuevoDia()
    {
        VentasDia = 0;
        IngresosDia = 0;
        SustosDia = 0;
        CalmadosNoche = 0;
        DescontroladosNoche = 0;
        ReputacionInicioDia = Reputacion;
    }

    public void RegistrarNoche(int calmados, int descontrolados)
    {
        CalmadosNoche = calmados;
        DescontroladosNoche = descontrolados;
    }

    public void RegistrarVenta(int monto, string cliente, string objeto)
    {
        Dinero += monto;
        Reputacion += reputacionPorVenta;
        VentasDia++;
        IngresosDia += monto;
        Mensaje($"{cliente} compró {objeto} por {monto}.");
    }

    public void RegistrarSusto(string cliente)
    {
        Reputacion += reputacionPorSusto;
        SustosDia++;
        Mensaje($"¡{cliente} se asustó y se fue! Reputación {reputacionPorSusto}.");
    }

    public void CambiarReputacion(int delta, string motivo)
    {
        Reputacion += delta;
        Mensaje($"{motivo} Reputación {delta}.");
    }

    public void Mensaje(string texto)
    {
        Debug.Log(texto);
        mensajes.Enqueue(texto);
        while (mensajes.Count > mensajesVisibles) mensajes.Dequeue();
    }

    private void OnGUI()
    {
        UIEscala.Aplicar();

        if (estiloPanel == null)
        {
            estiloPanel = new GUIStyle(GUI.skin.box) { fontSize = 20, alignment = TextAnchor.MiddleLeft };
            estiloMensajes = new GUIStyle(GUI.skin.box) { fontSize = 16, alignment = TextAnchor.UpperLeft };
        }

        int peligro = colocador != null ? colocador.PeligroActual : 0;
        GUI.Box(new Rect(10, UIEscala.Alto - 50, 620, 40),
            $"Dinero: {Dinero}    Reputación: {Reputacion}    Peligro: {peligro}",
            estiloPanel);

        if (mensajes.Count > 0)
        {
            float alto = mensajes.Count * 24f + 10f;
            GUI.Box(new Rect(10, UIEscala.Alto - 50 - alto - 5, 620, alto),
                string.Join("\n", mensajes), estiloMensajes);
        }
    }
}