using UnityEngine;

public class Partida : MonoBehaviour
{
    [SerializeField] private ConfiguracionPartida config;

    private EstadoPartida estado;

    public ConfiguracionPartida Config => config;
    public EstadoPartida Estado => estado ??= EstadoPartida.Nueva(config);
}