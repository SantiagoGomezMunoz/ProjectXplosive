using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Tienda/Catalogo de objetos", fileName = "CatalogoObjetos")]
public class CatalogoObjetos : ScriptableObject
{
    public List<ObjetoMalditoData> objetos = new();

    public ObjetoMalditoData Buscar(string id)
    {
        foreach (ObjetoMalditoData o in objetos)
            if (o != null && o.Id == id) return o;
        return null;
    }
}