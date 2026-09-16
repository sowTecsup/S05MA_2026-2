using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Arranca la sesion sin escribir nada en clase.
///
/// El OnGUI esta aqui a proposito: las ventanas de jugador virtual de
/// Multiplayer Play Mode NO tienen Inspector, asi que los botones en pantalla
/// son la unica via para arrancar el cliente desde ellas.
///
/// El boton de Server (sin jugador) esta para poder probar sin la trampa del
/// host: un servidor dedicado no juega, y con el "libre" no se confunde nunca
/// con "del host". Es ademas lo que se hace de verdad en la semana 15.
/// </summary>
public class ConnectionManager : MonoBehaviour
{
    [ContextMenu("Start Host")]
    public void StartHost()
    {
        NetworkManager.Singleton.StartHost();
        Debug.Log("Host iniciado");
    }

    [ContextMenu("Start Client")]
    public void StartClient()
    {
        NetworkManager.Singleton.StartClient();
        Debug.Log("Cliente iniciado");
    }

    [ContextMenu("Start Server")]
    public void StartServer()
    {
        NetworkManager.Singleton.StartServer();
        Debug.Log("Servidor iniciado (sin jugador local)");
    }

    private void OnGUI()
    {
        if (NetworkManager.Singleton == null) return;
        if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer) return;

        GUI.skin.button.fontSize = 18;
        if (GUI.Button(new Rect(12, 12, 140, 36), "Host")) StartHost();
        if (GUI.Button(new Rect(12, 56, 140, 36), "Client")) StartClient();
        if (GUI.Button(new Rect(12, 100, 140, 36), "Server")) StartServer();
    }
}
