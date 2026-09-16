// =============================================================================
// CASO 02 - Quien escribe el transform: AuthorityMode Server u Owner.
// ORDEN ......... cualquiera.
// GAMEOBJECT .... "Caso02" vacio en la escena, con NetworkObject · Caso02_QuienEscribe
//                 + dos capsulas en la escena, cada una con NetworkObject · NetworkTransform:
//                   "ManiquiServer"  Authority Mode = Server
//                   "ManiquiOwner"   Authority Mode = Owner
// ARRASTRAR ..... los dos maniquis a sus campos
// TECLAS ........ 4 hacerte dueno de los dos · IJKL moverlos · 5 informe
// =============================================================================
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

// Desde un CLIENTE: pulsa 4 y muevelos con IJKL. Eres dueno de los dos, pero solo se
// mueve el Owner. Ser dueno no da permiso: lo da la casilla.
//   CanCommitToTransform = IsServerAuthoritative() ? IsServer : IsOwner;
// En el HOST se mueven los dos, porque el host es el servidor.
public class Caso02_QuienEscribe : NetworkBehaviour
{
    public NetworkTransform maniquiServer;
    public NetworkTransform maniquiOwner;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha4)) HazmeDuenoRpc();

        Vector3 dir = new Vector3(
            (Input.GetKey(KeyCode.L) ? 1 : 0) - (Input.GetKey(KeyCode.J) ? 1 : 0), 0f,
            (Input.GetKey(KeyCode.I) ? 1 : 0) - (Input.GetKey(KeyCode.K) ? 1 : 0));
        Vector3 paso = dir * 4f * Time.deltaTime;

        if (maniquiServer.IsOwner) maniquiServer.transform.position += paso;
        if (maniquiOwner.IsOwner) maniquiOwner.transform.position += paso;

        if (Input.GetKeyDown(KeyCode.Alpha5))
            Debug.Log("[Caso02] Server: IsOwner=" + maniquiServer.IsOwner + " CanCommit=" + maniquiServer.CanCommitToTransform +
                      " | Owner: IsOwner=" + maniquiOwner.IsOwner + " CanCommit=" + maniquiOwner.CanCommitToTransform);
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
    private void HazmeDuenoRpc(RpcParams rpcParams = default)
    {
        ulong quien = rpcParams.Receive.SenderClientId;
        maniquiServer.NetworkObject.ChangeOwnership(quien);
        maniquiOwner.NetworkObject.ChangeOwnership(quien);
    }
}
