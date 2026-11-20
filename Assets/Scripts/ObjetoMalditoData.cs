using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Tienda/Objeto maldito", fileName = "NuevoObjeto")]
public class ObjetoMalditoData : ScriptableObject
{
    public string nombre;
    public Sprite sprite;
    public int precioBase = 10;
    [Range(0, 5)] public int peligro;
    public List<Etiqueta> etiquetas = new();
    public List<Vector2Int> aura = new()
    {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
    };
}