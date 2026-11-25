using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class GeneradorCatalogoActo1
{
    private const string Carpeta = "Assets/Objetos";
    private const string NombreSpriteProvisional = "MarcadorCasillas";

    private struct Definicion
    {
        public string archivo;   // será el Id del objeto: sin tildes ni espacios
        public string nombre;
        public Etiqueta etiqueta;
        public int precio;
        public int peligro;
        public Vector2Int[] alcance;
        public string descripcion;
    }

    private static readonly Vector2Int[] Cruz =
        { new(0, 1), new(0, -1), new(-1, 0), new(1, 0) };

    private static readonly Vector2Int[] Diagonales =
        { new(1, 1), new(1, -1), new(-1, 1), new(-1, -1) };

    private static readonly Vector2Int[] Anillo =
    {
        new(0, 1), new(0, -1), new(-1, 0), new(1, 0),
        new(1, 1), new(1, -1), new(-1, 1), new(-1, -1)
    };

    private static readonly Vector2Int[] LineaArriba = { new(0, 1), new(0, 2) };
    private static readonly Vector2Int[] LineaAmbosLados = { new(-1, 0), new(1, 0) };

    private static readonly Definicion[] Objetos =
    {
        new Definicion
        {
            archivo = "VelaEterna", nombre = "Vela Eterna", etiqueta = Etiqueta.Fuego,
            precio = 14, peligro = 1, alcance = Diagonales,
            descripcion = "Nunca se consume. Tampoco se apaga."
        },
        new Definicion
        {
            archivo = "BraseroSinFondo", nombre = "Brasero Sin Fondo", etiqueta = Etiqueta.Fuego,
            precio = 16, peligro = 1, alcance = LineaArriba,
            descripcion = "Siempre hay brasas, aunque nadie recuerda haberlas encendido."
        },
        new Definicion
        {
            archivo = "CoronaArdiente", nombre = "Corona Ardiente", etiqueta = Etiqueta.Fuego,
            precio = 40, peligro = 3, alcance = Anillo,
            descripcion = "Quien la lleva deja de sentir frío. Y de sentir otras cosas."
        },
        new Definicion
        {
            archivo = "CampanaDeEscarcha", nombre = "Campana de Escarcha", etiqueta = Etiqueta.Frio,
            precio = 15, peligro = 0, alcance = Cruz,
            descripcion = "Suena a lo lejos y todo a su alrededor se enfría."
        },
        new Definicion
        {
            archivo = "LagrimaDeHielo", nombre = "Lágrima de Hielo", etiqueta = Etiqueta.Frio,
            precio = 18, peligro = 0, alcance = LineaAmbosLados,
            descripcion = "Nunca termina de derretirse."
        },
        new Definicion
        {
            archivo = "SarcofagoDeNieve", nombre = "Sarcófago de Nieve", etiqueta = Etiqueta.Frio,
            precio = 38, peligro = 2, alcance = Anillo,
            descripcion = "Algo duerme dentro. Mejor que siga durmiendo."
        },
        new Definicion
        {
            archivo = "PlatoVacio", nombre = "Plato Vacío", etiqueta = Etiqueta.Hambre,
            precio = 22, peligro = 2, alcance = Cruz,
            descripcion = "Nunca se llena. Nadie sabe qué espera."
        },
        new Definicion
        {
            archivo = "LibroConDientes", nombre = "Libro con Dientes", etiqueta = Etiqueta.Hambre,
            precio = 26, peligro = 2, alcance = Diagonales,
            descripcion = "Se cierra solo cuando alguien lo lee."
        },
        new Definicion
        {
            archivo = "OllaInsaciable", nombre = "Olla Insaciable", etiqueta = Etiqueta.Hambre,
            precio = 45, peligro = 4, alcance = Anillo,
            descripcion = "Se come lo que le pongas al lado."
        }
    };

    [MenuItem("Tienda/Generar objetos del acto 1")]
    public static void Generar()
    {
        if (!AssetDatabase.IsValidFolder(Carpeta)) AssetDatabase.CreateFolder("Assets", "Objetos");

        Sprite provisional = BuscarSpriteProvisional();
        CatalogoObjetos catalogo = BuscarCatalogo();

        if (catalogo == null)
            Debug.LogWarning("No encontré ningún CatalogoObjetos: los objetos se crean, pero hay que añadirlos a mano.");
        if (provisional == null)
            Debug.LogWarning($"No encontré el sprite '{NombreSpriteProvisional}': los objetos nuevos quedan sin sprite.");

        int creados = 0;
        int existentes = 0;
        int anadidos = 0;

        foreach (Definicion d in Objetos)
        {
            string ruta = $"{Carpeta}/{d.archivo}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<ObjetoMalditoData>(ruta);

            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<ObjetoMalditoData>();
                asset.nombre = d.nombre;
                asset.sprite = provisional;
                asset.precioBase = d.precio;
                asset.peligro = d.peligro;
                asset.etiquetas = new List<Etiqueta> { d.etiqueta };
                asset.aura = new List<Vector2Int>(d.alcance);
                asset.descripcion = d.descripcion;
                AssetDatabase.CreateAsset(asset, ruta);
                creados++;
            }
            else
            {
                existentes++; // no se sobrescribe lo que ya existe
            }

            if (catalogo != null && !catalogo.objetos.Contains(asset))
            {
                catalogo.objetos.Add(asset);
                anadidos++;
            }
        }

        if (catalogo != null) EditorUtility.SetDirty(catalogo);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Acto 1: {creados} objetos creados, {existentes} ya existían, {anadidos} añadidos al catálogo.");
    }

    [MenuItem("Tienda/Revisar catálogo")]
    public static void Revisar()
    {
        CatalogoObjetos catalogo = BuscarCatalogo();
        if (catalogo == null)
        {
            Debug.LogWarning("No encontré ningún CatalogoObjetos.");
            return;
        }

        var ids = new HashSet<string>();
        var provisionales = new List<string>();
        var sinSprite = new List<string>();
        var sinDescripcion = new List<string>();
        int duplicados = 0;

        foreach (ObjetoMalditoData o in catalogo.objetos)
        {
            if (o == null)
            {
                Debug.LogWarning("El catálogo tiene una entrada vacía.");
                continue;
            }

            if (!ids.Add(o.Id))
            {
                duplicados++;
                Debug.LogWarning($"Id duplicado: {o.Id}");
            }

            if (o.sprite == null) sinSprite.Add(o.nombre);
            else if (o.sprite.name == NombreSpriteProvisional) provisionales.Add(o.nombre);

            if (string.IsNullOrWhiteSpace(o.descripcion)) sinDescripcion.Add(o.nombre);
        }

        int propios = ids.Count - provisionales.Count - sinSprite.Count;
        Debug.Log(
            $"Catálogo: {ids.Count} objetos. Con sprite propio: {propios}. " +
            $"Con sprite provisional: {provisionales.Count} ({string.Join(", ", provisionales)}). " +
            $"Sin sprite: {sinSprite.Count}. Sin descripción: {sinDescripcion.Count}. " +
            $"Ids duplicados: {duplicados}.");
    }

    private static Sprite BuscarSpriteProvisional()
    {
        foreach (string guid in AssetDatabase.FindAssets($"{NombreSpriteProvisional} t:Sprite"))
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guid));
            if (sprite != null) return sprite;
        }
        return null;
    }

    private static CatalogoObjetos BuscarCatalogo()
    {
        string[] guids = AssetDatabase.FindAssets("t:CatalogoObjetos");
        return guids.Length > 0
            ? AssetDatabase.LoadAssetAtPath<CatalogoObjetos>(AssetDatabase.GUIDToAssetPath(guids[0]))
            : null;
    }
}