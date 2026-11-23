using System;
using System.Collections.Generic;

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
    public bool victoria;
    public List<EntradaAlmacen> almacen = new();

    public static EstadoPartida Nueva(ConfiguracionPartida configuracion)
    {
        var estado = new EstadoPartida
        {
            modo = configuracion.modo,
            dia = 1,
            dinero = configuracion.dineroInicial,
            deudaRestante = configuracion.DeudaTotal
        };

        foreach (ObjetoInicial o in configuracion.almacenInicial)
            if (o.objeto != null) estado.AgregarAlAlmacen(o.objeto.Id, o.cantidad);

        return estado;
    }

    public int CantidadEnAlmacen(string id)
    {
        foreach (EntradaAlmacen e in almacen)
            if (e.id == id) return e.cantidad;
        return 0;
    }

    public void AgregarAlAlmacen(string id, int cantidad = 1)
    {
        if (cantidad <= 0) return;

        foreach (EntradaAlmacen e in almacen)
        {
            if (e.id != id) continue;
            e.cantidad += cantidad;
            return;
        }

        almacen.Add(new EntradaAlmacen { id = id, cantidad = cantidad });
    }

    public bool QuitarDelAlmacen(string id)
    {
        for (int i = 0; i < almacen.Count; i++)
        {
            if (almacen[i].id != id) continue;

            almacen[i].cantidad--;
            if (almacen[i].cantidad <= 0) almacen.RemoveAt(i);
            return true;
        }
        return false;
    }
}