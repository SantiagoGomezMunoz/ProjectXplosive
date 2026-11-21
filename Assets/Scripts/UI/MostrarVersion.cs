using UnityEngine;

public class MostrarVersion : MonoBehaviour
{
    private void OnGUI()
    {
        UIEscala.Aplicar();
        var estilo = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 13 };
        GUI.Label(new Rect((UIEscala.Ancho - 160f) / 2f, 2f, 160f, 20f), $"v{Application.version}", estilo);
    }
}