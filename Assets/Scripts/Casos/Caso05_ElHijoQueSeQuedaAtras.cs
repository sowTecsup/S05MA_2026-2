// =============================================================================
// CASO 05 - Que le pasa al transform al emparentar un NetworkObject.
// ORDEN ......... cualquiera.
// GAMEOBJECT .... "Caso05" vacio en la escena, con NetworkObject · Caso05_ElHijoQueSeQuedaAtras
//                 + dos cubos pequenos en la escena, cada uno con NetworkObject · NetworkTransform
//                 (Authority Mode = Server):
//                   "CuboMundo"  Switch Transform Space When Parented = NO
//                   "CuboLocal"  Switch Transform Space When Parented = SI
// ARRASTRAR ..... los dos cubos a sus campos
// TECLAS ........ P en un cliente hasta "200 ms" · H agarrar los dos · G soltarlos
// =============================================================================
using Unity.Netcode;
using UnityEngine;

// Pulsa H y camina en zigzag a 200 ms. El CuboMundo te persigue: el servidor lo coloca
// donde EL cree que estas. El CuboLocal va pegado: viaja su posicion respecto a ti.
public class Caso05_ElHijoQueSeQuedaAtras : NetworkBehaviour
{
    public NetworkObject cuboMundo;
    public NetworkObject cuboLocal;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.H)) AgarrarRpc();
        if (Input.GetKeyDown(KeyCode.G)) SoltarRpc();
    }

    // Solo el servidor puede emparentar, por eso se pide con un RPC.
    [Rpc(SendTo.Server, RequireOwnership = false)]
    private void AgarrarRpc(RpcParams rpcParams = default)
    {
        NetworkObject jugador = NetworkManager.ConnectedClients[rpcParams.Receive.SenderClientId].PlayerObject;

        cuboMundo.TrySetParent(jugador, false);          // el padre TIENE que ser un NetworkObject
        cuboLocal.TrySetParent(jugador, false);
        cuboMundo.transform.localPosition = new Vector3(-0.8f, 0.5f, 0.9f);
        cuboLocal.transform.localPosition = new Vector3(0.8f, 0.5f, 0.9f);
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
    private void SoltarRpc()
    {
        cuboMundo.TryRemoveParent(true);
        cuboLocal.TryRemoveParent(true);
    }
}
