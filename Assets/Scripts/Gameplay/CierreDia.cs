using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class CierreDia : MonoBehaviour
{
    private struct Candidato
    {
        public ObjetoColocado exhibido; // null si está en el almacén
        public string id;
        public int valor;
    }

    [SerializeField] private GestorDia gestorDia;
    [SerializeField] private Partida partida;
    [SerializeField] private ColocadorObjetos colocador;
    [SerializeField] private EstadoTienda tienda;
    [SerializeField] private CatalogoObjetos catalogo;

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
        // Se pueden embargar los objetos exhibidos y los guardados, por su precio base
        var candidatos = new List<Candidato>();

        foreach (ObjetoColocado o in colocador.Grid.Todos)
            candidatos.Add(new Candidato { exhibido = o, id = o.data.Id, valor = o.data.precioBase });

        foreach (EntradaAlmacen e in partida.Estado.almacen)
        {
            ObjetoMalditoData d = catalogo.Buscar(e.id);
            int valor = d != null ? d.precioBase : 0;
            for (int i = 0; i < e.cantidad; i++)
                candidatos.Add(new Candidato { id = e.id, valor = valor });
        }

        var valores = new List<int>();
        foreach (Candidato c in candidatos) valores.Add(c.valor);

        ResultadoCierre r = Economia.CerrarDia(partida.Estado, partida.Config, valores);
        UltimoResultado = r;

        NombresEmbargados.Clear();
        foreach (int indice in r.indicesEmbargados)
        {
            Candidato c = candidatos[indice];
            ObjetoMalditoData d = catalogo.Buscar(c.id);
            NombresEmbargados.Add(d != null ? d.nombre : c.id);

            if (c.exhibido != null) colocador.Vender(c.exhibido);
            else partida.Estado.QuitarDelAlmacen(c.id);
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