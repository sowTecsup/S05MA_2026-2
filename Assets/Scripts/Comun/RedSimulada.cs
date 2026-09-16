using Unity.Multiplayer.Tools.NetworkSimulator.Runtime;
using UnityEngine;

/// <summary>
/// ANDAMIAJE - no es el tema de hoy, pero sin esto no hay tema.
///
/// En el aula la red mide 1 ms y todo se ve perfecto. Esta semana hay que VER
/// la latencia, asi que el NetworkManager lleva un Network Simulator (paquete
/// Multiplayer Tools) y esta tecla le cambia la conexion en caliente:
///
///   P   red perfecta  ->  200 ms  ->  200 ms con 20% de perdida  ->  red perfecta
///
/// Va POR VENTANA: cada jugador virtual de Multiplayer Play Mode tiene su
/// propio simulador. Pulsa P en la ventana del cliente que quieras castigar.
/// La conexion activa se ve arriba a la derecha.
/// </summary>
[RequireComponent(typeof(NetworkSimulator))]
public class RedSimulada : MonoBehaviour
{
    private static readonly NetworkSimulatorPreset[] CONEXIONES =
    {
        NetworkSimulatorPreset.Create("Red perfecta"),
        NetworkSimulatorPreset.Create("200 ms", "latencia de una red domestica lejana", 200, 20),
        NetworkSimulatorPreset.Create("200 ms + 20% perdida", "red mala de verdad", 200, 20, 0, 20)
    };

    private int actual;
    private NetworkSimulator simulador;

    void Awake()
    {
        simulador = GetComponent<NetworkSimulator>();
        simulador.ConnectionPreset = CONEXIONES[actual];
    }

    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.P)) return;
        actual = (actual + 1) % CONEXIONES.Length;
        simulador.ConnectionPreset = CONEXIONES[actual];
        Debug.Log("[Red] Esta ventana simula ahora: " + CONEXIONES[actual].Name);
    }

    void OnGUI()
    {
        GUI.Label(new Rect(Screen.width - 230, 10, 220, 24), "Red (P): " + CONEXIONES[actual].Name);
    }
}
