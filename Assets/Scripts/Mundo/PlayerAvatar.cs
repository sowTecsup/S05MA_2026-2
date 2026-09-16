// =============================================================================
// ORDEN ......... 4 de 4. El ultimo: usa ChestNode y CuboDeColor.
// GAMEOBJECT .... prefab "PlayerAvatar" (3D Object > Capsule)
// COMPONENTES ... NetworkObject
//                 NetworkTransform  Authority Mode = Owner
//                                   Sync Position: X y Z (Y NO)
//                                   Sync Rotation: solo Y
//                                   Sync Scale: nada
//                                   Use Unreliable Deltas = SI
//                                   Use Half Float Precision = SI
//                 PlayerAvatar
// ARRASTRAR ..... el prefab a NetworkManager > Player Prefab
// =============================================================================
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

// WASD mover · E abrir cofre (mantener) / agarrar / soltar · Q lanzar · R volver al inicio.
// No hay RPC de movimiento: la posicion la replica el NetworkTransform.
public class PlayerAvatar : NetworkBehaviour
{
    public float velocidad = 5f;
    public float alcance = 2.5f;
    public float fuerzaLanzamiento = 9f;

    private static readonly Color[] COLORES = { Color.cyan, Color.magenta, Color.white, Color.black };
    private ChestNode cofre;

    public override void OnNetworkSpawn()
    {
        GetComponent<Renderer>().material.color = COLORES[OwnerClientId % (ulong)COLORES.Length];
    }

    void Update()
    {
        if (!IsOwner) return;

        // Mover: solo el transform local. NetworkTransform lo replica.
        Vector3 paso = new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical")) * velocidad * Time.deltaTime;
        transform.Translate(paso, Space.World);
        if (paso != Vector3.zero) transform.rotation = Quaternion.LookRotation(paso);

        CuboDeColor llevado = CuboQueLlevo();

        if (Input.GetKeyDown(KeyCode.E))
        {
            ChestNode cercano = FindFirstObjectByType<ChestNode>();
            CuboDeColor libre = CuboLibreCerca();

            if (llevado != null) PedirSoltarRpc(llevado.NetworkObject);
            else if (cercano != null && Cerca(cercano.transform)) { cofre = cercano; cofre.EmpezarAbrirRpc(); }
            else if (libre != null) PedirAgarrarRpc(libre.NetworkObject);
        }

        if (Input.GetKeyUp(KeyCode.E) && cofre != null)
        {
            cofre.DejarDeAbrirRpc();
            cofre = null;
        }

        if (Input.GetKeyDown(KeyCode.Q) && llevado != null) PedirLanzarRpc(llevado.NetworkObject);
        if (Input.GetKeyDown(KeyCode.R)) PedirReaparecerRpc();
    }

    // ---- Cubos: el cliente pide, el servidor decide -------------------------

    [Rpc(SendTo.Server, RequireOwnership = true)]
    public void PedirAgarrarRpc(NetworkObjectReference referencia)
    {
        if (!referencia.TryGet(out NetworkObject objeto)) return;
        CuboDeColor cubo = objeto.GetComponent<CuboDeColor>();
        if (cubo.LoLlevaAlguien) return;            // se comprueba EN EL SERVIDOR
        cubo.Agarrar(NetworkObject);
    }

    [Rpc(SendTo.Server, RequireOwnership = true)]
    public void PedirSoltarRpc(NetworkObjectReference referencia)
    {
        if (!referencia.TryGet(out NetworkObject objeto)) return;
        if (objeto.transform.parent != transform) return;       // no lo llevas tu
        objeto.GetComponent<CuboDeColor>().Soltar(Vector3.zero);
    }

    [Rpc(SendTo.Server, RequireOwnership = true)]
    public void PedirLanzarRpc(NetworkObjectReference referencia)
    {
        if (!referencia.TryGet(out NetworkObject objeto)) return;
        if (objeto.transform.parent != transform) return;       // no lo llevas tu
        objeto.GetComponent<CuboDeColor>().Soltar(transform.forward * fuerzaLanzamiento + Vector3.up * 3f);
    }

    // ---- Reaparecer: lo decide el servidor, lo ejecuta el dueno -------------

    [Rpc(SendTo.Server, RequireOwnership = true)]
    private void PedirReaparecerRpc()
    {
        ReaparecerRpc(new Vector3(0f, 1f, -4f));
    }

    // La capsula es Owner: solo el dueno puede hacer Teleport sobre ella.
    [Rpc(SendTo.Owner)]
    public void ReaparecerRpc(Vector3 punto)
    {
        GetComponent<NetworkTransform>().Teleport(punto, transform.rotation, transform.localScale);
    }

    // ---- Ayudas ---------------------------------------------------------------

    private bool Cerca(Transform t) => Vector3.Distance(transform.position, t.position) < alcance;

    private CuboDeColor CuboQueLlevo()
    {
        foreach (var cubo in FindObjectsByType<CuboDeColor>(FindObjectsSortMode.None))
            if (cubo.transform.parent == transform) return cubo;
        return null;
    }

    private CuboDeColor CuboLibreCerca()
    {
        foreach (var cubo in FindObjectsByType<CuboDeColor>(FindObjectsSortMode.None))
            if (!cubo.LoLlevaAlguien && Cerca(cubo.transform)) return cubo;
        return null;
    }
}
