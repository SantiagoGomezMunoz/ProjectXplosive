using System;

[Serializable]
public class EstadoPartida
{
    public ModoJuego modo;
    public int dia = 1;
    public int dinero;
    public int deudaRestante;
    public int cuotasPagadas;
    public int reputacion;
    public bool terminada;

    public static EstadoPartida Nueva(ConfiguracionPartida configuracion)
    {
        return new EstadoPartida
        {
            modo = configuracion.modo,
            dia = 1,
            dinero = configuracion.dineroInicial,
            deudaRestante = configuracion.DeudaTotal,
            cuotasPagadas = 0,
            reputacion = 0,
            terminada = false
        };
    }
}