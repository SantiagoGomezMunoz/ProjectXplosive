using UnityEngine;
using UnityEngine.InputSystem;

public class SalirConEsc : MonoBehaviour
{
    private void Update()
    {
        Keyboard teclado = Keyboard.current;
        if (teclado == null || !teclado.escapeKey.wasPressedThisFrame) return;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}