using System.Collections.Generic;
using UnityEngine;

// PROVISIONAL: hasta que exista la subasta (paso 5), cada mañana llegan objetos al azar.
public class SuministroProvisional : MonoBehaviour
{
    [SerializeField] private GestorDia gestorDia;
    [SerializeField] private Partida partida;
    [SerializeField] private CatalogoObjetos catalogo;
    [SerializeField] private EstadoTienda tienda;
    [SerializeField] private int desdeElDia = 2;

    private void OnEnable() => gestorDia.FaseCambiada += AlCambiarFase;

    private void OnDisable()
    {
        if (gestorDia != null) gestorDia.FaseCambiada -= AlCambiarFase;
    }

    private void AlCambiarFase(FaseDia fase)
    {
        if (fase != FaseDia.Manana || partida.Estado.dia < desdeElDia) return;

        int cantidad = partida.Config.suministroDiarioProvisional;
        if (cantidad <= 0 || catalogo.objetos.Count == 0) return;

        var nombres = new List<string>();
        for (int i = 0; i < cantidad; i++)
        {
            ObjetoMalditoData d = catalogo.objetos[Random.Range(0, catalogo.objetos.Count)];
            partida.Estado.AgregarAlAlmacen(d.Id);
            nombres.Add(d.nombre);
        }

        tienda.Mensaje($"Llegó un pedido al almacén: {string.Join(", ", nombres)}.");
    }
}