using UnityEngine;

[CreateAssetMenu(menuName = "Tienda/Perfil de cliente", fileName = "NuevoCliente")]
public class PerfilCliente : ScriptableObject
{
    public string nombre;
    public Sprite sprite;
    public Color tinte = Color.white;
    public float velocidad = 2f;

    [Header("Se usan desde el día 11")]
    public float multiplicadorPrecio = 1f;
    public int peligroMinimoObjeto = 0;
    public bool soloSinInteracciones = false;
    public int umbralMiedo = 0; // 0 = no se asusta
}