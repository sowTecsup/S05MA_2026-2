using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

/// <summary>
/// CASO 02 - Quien escribe el transform: AuthorityMode Server u Owner.  (*)
///
/// Es la sorpresa de la sesion. Le pones un NetworkTransform al avatar, borras
/// los RPC de mover, le das a Play... y el cliente NO SE MUEVE. Tiembla y vuelve.
///
/// La razon esta en una sola linea del codigo de NGO:
///
///     CanCommitToTransform = IsServerAuthoritative() ? IsServer : IsOwner;
///
/// Authority Mode viene en SERVER. Con Server, solo el servidor puede escribir
/// el transform: cada frame, NetworkTransform le devuelve al dueno la posicion
/// que dice el servidor. Ser dueno no te da permiso; te lo da la casilla.
///
/// Dos maniquis identicos salvo en esa casilla:
///
///   Maniqui_Server   Authority Mode = Server
///   Maniqui_Owner    Authority Mode = Owner
///
/// Como se ve, en tres etapas:
///
///   ETAPA A (ser dueno no basta)
///     1. En una ventana de CLIENTE pulsa 4: el servidor te hace dueno de los
///        dos (ChangeOwnership, semana 2).
///     2. Muevelos con I J K L. El de Owner camina; el de Server tiembla y vuelve.
///     3. Pulsa 5 en esa ventana: CanCommitToTransform = false en el de Server,
///        aunque IsOwner = true en los dos.
///
///   ETAPA B (la trampa del host)
///     4. Repite desde la ventana del HOST: ahi los DOS se mueven, porque el host
///        ES el servidor. Por eso este fallo nunca aparece si solo pruebas en la
///        ventana principal.
///
///   ETAPA C (lo que compra cada uno)
///     5. Pulsa P en el cliente (200 ms): el de Owner sigue respondiendo al
///        instante en su pantalla. Si el servidor moviera la capsula a partir de
///        tus teclas, sentirias esos 200 ms en el dedo.
///        Owner compra respuesta y regala confianza: el cliente decide donde
///        esta (semana 12). Server compra confianza y regala retardo (semana 10).
///
/// (*) Caso destacado.
/// </summary>
public class Caso02_QuienEscribe : NetworkBehaviour
{
    public NetworkTransform maniquiServer;
    public NetworkTransform maniquiOwner;
    public float velocidad = 4f;

    void Update()
    {
        if (!IsSpawned) return;

        if (Input.GetKeyDown(KeyCode.Alpha4)) PedirPropiedadRpc();

        Vector3 dir = Vector3.zero;
        if (Input.GetKey(KeyCode.I)) dir += Vector3.forward;
        if (Input.GetKey(KeyCode.K)) dir += Vector3.back;
        if (Input.GetKey(KeyCode.J)) dir += Vector3.left;
        if (Input.GetKey(KeyCode.L)) dir += Vector3.right;

        if (dir != Vector3.zero)
        {
            Vector3 paso = dir * velocidad * Time.deltaTime;
            // Los dos se mueven EN LOCAL igual. Lo que cambia es si el
            // componente deja que ese cambio cuente.
            if (maniquiServer.IsOwner) maniquiServer.transform.Translate(paso, Space.World);
            if (maniquiOwner.IsOwner) maniquiOwner.transform.Translate(paso, Space.World);
        }

        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            Debug.Log("[Caso02] INFORME en esta maquina (" + (IsServer ? "servidor" : "cliente " + NetworkManager.LocalClientId) + "):" +
                      "\n   Maniqui_Server  IsOwner=" + maniquiServer.IsOwner + "  CanCommitToTransform=" + maniquiServer.CanCommitToTransform +
                      "\n   Maniqui_Owner   IsOwner=" + maniquiOwner.IsOwner + "  CanCommitToTransform=" + maniquiOwner.CanCommitToTransform);
        }
    }

    // El caso es del servidor: cualquiera puede pedirlo, por eso NO exige dueno.
    [Rpc(SendTo.Server, RequireOwnership = false)]
    private void PedirPropiedadRpc(RpcParams rpcParams = default)
    {
        ulong quien = rpcParams.Receive.SenderClientId;
        maniquiServer.NetworkObject.ChangeOwnership(quien);
        maniquiOwner.NetworkObject.ChangeOwnership(quien);
        Debug.Log("[Caso02] Los dos maniquis son ahora del cliente " + quien + ". Muevelos con I J K L.");
    }

    private void OnGUI()
    {
        Etiqueta(maniquiServer, "Authority Mode: Server");
        Etiqueta(maniquiOwner, "Authority Mode: Owner");
    }

    private static void Etiqueta(Component c, string texto)
    {
        if (c == null || Camera.main == null) return;
        Vector3 p = Camera.main.WorldToScreenPoint(c.transform.position + Vector3.up * 1.4f);
        if (p.z > 0f) GUI.Label(new Rect(p.x - 80f, Screen.height - p.y, 200f, 22f), texto);
    }
}
