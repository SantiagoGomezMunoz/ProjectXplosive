using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Tienda/Tabla de interacciones", fileName = "TablaInteracciones")]
public class TablaInteracciones : ScriptableObject
{
    public List<ReglaInteraccion> reglas = new();

    public bool TryObtener(Etiqueta a, Etiqueta b, out ReglaInteraccion regla)
    {
        foreach (ReglaInteraccion r in reglas)
        {
            if ((r.etiquetaA == a && r.etiquetaB == b) || (r.etiquetaA == b && r.etiquetaB == a))
            {
                regla = r;
                return true;
            }
        }
        regla = null;
        return false;
    }
}