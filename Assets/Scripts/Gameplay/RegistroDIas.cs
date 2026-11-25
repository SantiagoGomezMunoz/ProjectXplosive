using System.IO;
using System.Text;
using UnityEngine;

public class RegistroDias : MonoBehaviour
{
    [SerializeField] private GestorDia gestorDia;
    [SerializeField] private Partida partida;
    [SerializeField] private EstadoTienda tienda;
    [SerializeField] private ColocadorObjetos colocador;
    [SerializeField] private CierreDia cierre;

    private const string Cabecera =
        "sesion,version,fecha,modo,dia,acto,dinero,reputacion,tramo,ventas,ingresos,sustos," +
        "calmados,sin_calmar,peligro,lotes_ganados,gasto_subasta,alquiler,alquiler_pagado," +
        "cuota,cuota_pagada,embargados,deuda_restante,terminada,victoria";

    private readonly string sesion = System.Guid.NewGuid().ToString("N").Substring(0, 8);
    private string ruta;
    private bool pendiente;

    private void Awake()
    {
        ruta = Path.Combine(Application.persistentDataPath, "registro_dias.csv");
        Debug.Log("Registro de días: " + ruta);
    }

    private void OnEnable() => gestorDia.FaseCambiada += AlCambiarFase;

    private void OnDisable()
    {
        if (gestorDia != null) gestorDia.FaseCambiada -= AlCambiarFase;
    }

    private void AlCambiarFase(FaseDia fase)
    {
        if (fase == FaseDia.Cierre) pendiente = true; // se escribe en Update, cuando todos ya registraron sus datos
    }

    private void Update()
    {
        if (!pendiente) return;
        pendiente = false;
        Escribir();
    }

    private void Escribir()
    {
        ResultadoCierre r = cierre.UltimoResultado;
        if (r == null) return;

        EstadoPartida e = partida.Estado;
        string tramo = partida.Config.TramoPara(e.reputacion)?.nombre ?? "";

        string[] campos =
        {
            sesion,
            Application.version,
            System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            e.modo.ToString(),
            r.dia.ToString(),
            (partida.Config.IndiceActo(r.dia) + 1).ToString(),
            e.dinero.ToString(),
            e.reputacion.ToString(),
            tramo,
            tienda.VentasDia.ToString(),
            tienda.IngresosDia.ToString(),
            tienda.SustosDia.ToString(),
            tienda.CalmadosNoche.ToString(),
            tienda.DescontroladosNoche.ToString(),
            colocador.PeligroActual.ToString(),
            tienda.LotesGanadosDia.ToString(),
            tienda.GastoSubastaDia.ToString(),
            r.alquiler.ToString(),
            (r.alquilerPagado ? 1 : 0).ToString(),
            r.cuota.ToString(),
            (r.cuotaPagada ? 1 : 0).ToString(),
            r.indicesEmbargados.Count.ToString(),
            e.deudaRestante.ToString(),
            (e.terminada ? 1 : 0).ToString(),
            (e.victoria ? 1 : 0).ToString()
        };

        try
        {
            var texto = new StringBuilder();
            if (!File.Exists(ruta)) texto.AppendLine(Cabecera);
            texto.AppendLine(string.Join(",", campos));
            File.AppendAllText(ruta, texto.ToString(), Encoding.UTF8);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"No se pudo guardar el registro de días: {ex.Message}");
        }
    }
}