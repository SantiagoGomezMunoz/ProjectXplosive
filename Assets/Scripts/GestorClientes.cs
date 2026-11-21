using System.Collections.Generic;
using UnityEngine;

public class GestorClientes : MonoBehaviour
{
    [SerializeField] private GestorDia gestorDia;
    [SerializeField] private ColocadorObjetos colocador;
    [SerializeField] private PerfilCliente[] perfiles;
    [SerializeField] private float primerClienteEn = 2f;
    [SerializeField] private float intervaloLlegada = 15f;
    [SerializeField] private int clientesPorDia = 6;
    [SerializeField] private EstadoTienda tienda;

    private readonly List<Cliente> activos = new();
    private int generados;
    private float temporizador;

    private void OnEnable() => gestorDia.FaseCambiada += AlCambiarFase;

    private void OnDisable()
    {
        if (gestorDia != null) gestorDia.FaseCambiada -= AlCambiarFase;
    }

    private void AlCambiarFase(FaseDia fase)
    {
        if (fase == FaseDia.Tarde)
        {
            generados = 0;
            temporizador = primerClienteEn;
        }
        else
        {
            // Quien siga en la tienda al terminar la tarde, se va
            foreach (Cliente c in activos.ToArray()) c.Salir();
        }
    }

    private void Update()
    {
        if (gestorDia.FaseActual != FaseDia.Tarde) return;
        if (generados >= clientesPorDia || perfiles.Length == 0) return;

        temporizador -= Time.deltaTime;
        if (temporizador > 0f) return;

        temporizador = intervaloLlegada;
        Generar();
    }

    private void Generar()
    {
        var go = new GameObject("Cliente");
        var cliente = go.AddComponent<Cliente>();
        cliente.Termino += c => activos.Remove(c);
        activos.Add(cliente);
        cliente.Iniciar(perfiles[Random.Range(0, perfiles.Length)], colocador, tienda);
        generados++;
    }
}