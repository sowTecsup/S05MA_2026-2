// =============================================================================
// ANDAMIAJE para ver la latencia. Crealo cuando ya exista el NetworkManager.
// GAMEOBJECT .... "NetworkManager"
// COMPONENTES ... Network Simulator (paquete Multiplayer Tools) · RedSimulada
// ARRASTRAR ..... nada
// USO ........... tecla P en una ventana: red perfecta -> 200 ms -> 200 ms + 20% perdida
// =============================================================================
using Unity.Multiplayer.Tools.NetworkSimulator.Runtime;
using UnityEngine;

public class RedSimulada : MonoBehaviour
{
    private static readonly NetworkSimulatorPreset[] REDES =
    {
        NetworkSimulatorPreset.Create("Red perfecta"),
        NetworkSimulatorPreset.Create("200 ms", "", 200, 20),
        NetworkSimulatorPreset.Create("200 ms + 20% perdida", "", 200, 20, 0, 20)
    };

    private int actual;

    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.P)) return;
        actual = (actual + 1) % REDES.Length;
        GetComponent<NetworkSimulator>().ConnectionPreset = REDES[actual];
        Debug.Log("[Red] " + REDES[actual].Name);
    }
}
