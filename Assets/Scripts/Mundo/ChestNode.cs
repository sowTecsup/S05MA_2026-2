// =============================================================================
// ORDEN ......... 3 de 4. Despues de CuboDeColor (necesita su prefab).
//                 Antes de PlayerAvatar (PlayerAvatar lo usa).
// GAMEOBJECT .... "Cofre" en la escena (3D Object > Cube, escala 1.2 x 0.8 x 0.8)
// COMPONENTES ... NetworkObject · ChestNode
// ARRASTRAR ..... el prefab CuboDeColor al campo "Cubo Prefab"
// =============================================================================
using Unity.Netcode;
using UnityEngine;

// Mantienes la E y se abre. Si otro lo esta abriendo, tu no puedes.
// Al abrirse suelta cubos y desaparece.
public class ChestNode : NetworkBehaviour
{
    public const ulong NADIE = ulong.MaxValue;

    public NetworkObject cuboPrefab;
    public int cantidad = 4;
    public float segundosParaAbrir = 1.5f;

    // Estado: quien lo abre y cuanto lleva. Lo escribe el servidor, lo ven todos.
    public NetworkVariable<ulong> abriendo = new NetworkVariable<ulong>(NADIE);
    public NetworkVariable<float> progreso = new NetworkVariable<float>(0f);

    void Update()
    {
        // En todas las maquinas: se pone amarillo segun avanza.
        GetComponent<Renderer>().material.color = Color.Lerp(Color.gray, Color.yellow, progreso.Value);

        if (!IsServer || abriendo.Value == NADIE) return;
        progreso.Value += Time.deltaTime / segundosParaAbrir;
        if (progreso.Value >= 1f) Abrir();
    }

    // El cofre es del servidor: si exigiera ser dueno, nadie podria abrirlo.
    [Rpc(SendTo.Server, RequireOwnership = false)]
    public void EmpezarAbrirRpc(RpcParams rpcParams = default)
    {
        ulong quien = rpcParams.Receive.SenderClientId;   // del mensaje, no de un parametro
        if (abriendo.Value != NADIE && abriendo.Value != quien) return;   // ocupado
        abriendo.Value = quien;
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
    public void DejarDeAbrirRpc(RpcParams rpcParams = default)
    {
        if (abriendo.Value != rpcParams.Receive.SenderClientId) return;   // no es tuyo
        abriendo.Value = NADIE;
        progreso.Value = 0f;
    }

    private void Abrir()
    {
        for (int i = 0; i < cantidad; i++)
        {
            Vector3 punto = transform.position + Quaternion.Euler(0f, i * 90f, 0f) * new Vector3(1.8f, 1f, 0f);
            NetworkObject cubo = Instantiate(cuboPrefab, punto, Random.rotation);
            cubo.Spawn();                     // semana 2
        }
        NetworkObject.Despawn();              // semana 7
    }
}
