using System;

[Serializable]
public class ReglaInteraccion
{
    public Etiqueta etiquetaA;
    public Etiqueta etiquetaB;
    [UnityEngine.Range(-1f, 2f)] public float modificadorValor;
    public int deltaPeligro;
    [UnityEngine.TextArea] public string mensaje;
}