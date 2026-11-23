using System.Collections.Generic;
using UnityEngine;

public class GestorClientes : MonoBehaviour
{
    [SerializeField] private GestorDia gestorDia;
    [SerializeField] private ColocadorObjetos colocador;
    [SerializeField] private Partida partida;
    [SerializeField] private PerfilCliente[] perfiles;
    [SerializeField] private EstadoTienda tienda;
    [SerializeField] private Material materialSprites;
    [SerializeField] private float primerClienteEn = 2f;
    [SerializeField] private float margenFinal = 10f;

    private readonly List<Cliente> activos = new();
    private int generados;
    private int clientesDelDia;
    private float intervalo;
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
            IniciarTarde();
            return;
        }

        // Quien siga en la tienda al terminar la tarde, se va
        foreach (Cliente c in activos.ToArray()) c.Salir();
    }

    private void IniciarTarde()
    {
        int reputacion = partida.Estado.reputacion;
        TramoReputacion tramo = partida.Config.TramoPara(reputacion);

        generados = 0;
        clientesDelDia = partida.Config.ClientesPorTarde(reputacion);
        temporizador = primerClienteEn;

        // Las llegadas se reparten a lo largo de la tarde, dejando un margen al final
        intervalo = clientesDelDia > 1
            ? Mathf.Max(3f, (gestorDia.DuracionTarde - primerClienteEn - margenFinal) / (clientesDelDia - 1))
            : 0f;

        string nombre = tramo != null ? tramo.nombre : "";
        tienda.Mensaje($"Reputación {nombre}: hoy se esperan {clientesDelDia} clientes.");
    }

    private void Update()
    {
        if (gestorDia.FaseActual != FaseDia.Tarde) return;
        if (generados >= clientesDelDia || perfiles.Length == 0) return;

        temporizador -= Time.deltaTime;
        if (temporizador > 0f) return;

        temporizador = intervalo;
        Generar();
    }

    private void Generar()
    {
        var go = new GameObject("Cliente");
        var cliente = go.AddComponent<Cliente>();
        cliente.Termino += c => activos.Remove(c);
        activos.Add(cliente);
        cliente.Iniciar(perfiles[Random.Range(0, perfiles.Length)], colocador, tienda, materialSprites);
        generados++;
    }
}