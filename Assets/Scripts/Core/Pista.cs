using System.Collections.Generic;

public enum TipoPista { Etiqueta, Peligro }

public class Pista
{
    public TipoPista tipo;
    public bool ambigua;
    public List<Etiqueta> etiquetas = new(); // pista de etiqueta
    public int peligroMin;                   // pista de peligro (iguales si es exacta)
    public int peligroMax;
}