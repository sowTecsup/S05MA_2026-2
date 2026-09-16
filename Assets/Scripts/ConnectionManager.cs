// =============================================================================
// ORDEN ......... 1 de 4 (mundo). No usa ningun otro script: puede ir primero.
// GAMEOBJECT .... "NetworkManager" (vacio, en la escena)
// COMPONENTES ... NetworkManager · UnityTransport · ConnectionManager
// ARRASTRAR ..... nada
// =============================================================================
using Unity.Multiplayer.PlayMode;
using Unity.Netcode;
using UnityEngine;

// Arranca solo: el editor principal es HOST y cada jugador virtual es CLIENTE.
public class ConnectionManager : MonoBehaviour
{
    void Start()
    {
        if (CurrentPlayer.IsMainEditor)
            NetworkManager.Singleton.StartHost();
        else
            NetworkManager.Singleton.StartClient();
    }
}
