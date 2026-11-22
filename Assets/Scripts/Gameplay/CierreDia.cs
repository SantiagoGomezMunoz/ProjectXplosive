using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class CierreDia : MonoBehaviour
{
    [SerializeField] private GestorDia gestorDia;
    [SerializeField] private Partida partida;
    [SerializeField] private ColocadorObjetos colocador;
    [SerializeField] private EstadoTienda tienda;

    public ResultadoCierre UltimoResultado { get; private set; }
    public List<string> NombresEmbargados { get; } = new();

    private void OnEnable() => gestorDia.FaseCambiada += AlCambiarFase;

    private void OnDisable()
    {
        if (gestorDia != null) gestorDia.FaseCambiada -= AlCambiarFase;
    }

    private void AlCambiarFase(FaseDia fase)
    {
        if (fase == FaseDia.Cierre) Cerrar();
    }

    private void Cerrar()
    {
        // Por ahora solo se pueden embargar los objetos exhibidos, por su precio base
        var objetos = new List<ObjetoColocado>(colocador.Grid.Todos);
        var valores = new List<int>();
        foreach (ObjetoColocado o in objetos) valores.Add(o.data.precioBase);

        ResultadoCierre r = Economia.CerrarDia(partida.Estado, partida.Config, valores);
        UltimoResultado = r;

        NombresEmbargados.Clear();
        foreach (int i in r.indicesEmbargados)
        {
            NombresEmbargados.Add(objetos[i].data.nombre);
            colocador.Vender(objetos[i]);
        }

        if (r.alquilerPagado)
            tienda.Mensaje($"Alquiler pagado: -{r.alquiler}.");
        else if (!r.partidaTerminada)
            tienda.Mensaje($"No pudiste pagar el alquiler ({r.alquiler}): -{Economia.ReputacionPorImpago} de reputación.");

        if (r.cuotaPagada)
            tienda.Mensaje($"Cuota de la deuda pagada: -{r.cuota}.");

        foreach (string nombre in NombresEmbargados)
            tienda.Mensaje($"Embargado: {nombre}.");
    }

    private void Update()
    {
        if (gestorDia.FaseActual != FaseDia.Cierre || !partida.Estado.terminada) return;

        Keyboard teclado = Keyboard.current;
        if (teclado != null && teclado.rKey.wasPressedThisFrame)
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}