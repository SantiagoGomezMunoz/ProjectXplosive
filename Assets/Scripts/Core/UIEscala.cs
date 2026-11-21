using UnityEngine;

public static class UIEscala
{
    private const float AlturaReferencia = 1080f;

    public static float Factor => Mathf.Clamp(Screen.height / AlturaReferencia, 0.5f, 1.5f);
    public static float Ancho => Screen.width / Factor;
    public static float Alto => Screen.height / Factor;

    public static void Aplicar() =>
        GUI.matrix = Matrix4x4.Scale(new Vector3(Factor, Factor, 1f));
}