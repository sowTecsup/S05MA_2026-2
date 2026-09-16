using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Abrir el cofre, agarrar, soltar y lanzar. Va en el avatar.
///
///   E (mantener) junto al cofre ... abrirlo
///   E junto a un cubo libre ....... agarrarlo
///   E llevando un cubo ............ soltarlo donde estas
///   Q llevando un cubo ............ lanzarlo hacia delante
///
/// El patron es el de la semana 4: el cliente manda la INTENCION y el servidor
/// decide. El cliente nunca mueve el cubo, ni lo emparenta, ni le pone
/// velocidad: el cubo es de autoridad de servidor y ademas, en cliente-servidor,
/// SOLO EL SERVIDOR PUEDE EMPARENTAR un NetworkObject.
///
/// Los tres RPC son acciones del jugador sobre lo suyo, asi que llevan
/// RequireOwnership = true escrito a mano (semana 3). Los del cofre, no: el
/// cofre no es de nadie.
///
/// La comprobacion "si alguien lo lleva, tu no" se hace EN EL SERVIDOR. Si solo
/// se hiciera en el cliente, dos jugadores que pulsan E en el mismo instante
/// verian los dos el cubo libre en su pantalla, y el segundo se lo quitaria de
/// las manos al primero.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class PlayerCarry : NetworkBehaviour
{
    public float alcance = 2.5f;
    public float velocidadLanzamiento = 9f;

    private ChestNode cofreQueAbro;

    void Update()
    {
        if (!IsOwner) return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            CuboDeColor llevado = CuboQueLlevo();
            if (llevado != null)
            {
                PedirSoltarRpc(llevado.NetworkObject);
            }
            else if ((cofreQueAbro = CofreCercano()) != null)
            {
                cofreQueAbro.EmpezarAbrirRpc();
            }
            else
            {
                CuboDeColor cubo = CuboLibreCercano();
                if (cubo != null) PedirAgarrarRpc(cubo.NetworkObject);
            }
        }

        if (Input.GetKeyUp(KeyCode.E) && cofreQueAbro != null)
        {
            if (cofreQueAbro.IsSpawned) cofreQueAbro.DejarDeAbrirRpc();
            cofreQueAbro = null;
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            CuboDeColor llevado = CuboQueLlevo();
            if (llevado != null) PedirLanzarRpc(llevado.NetworkObject);
        }
    }

    private CuboDeColor CuboQueLlevo()
    {
        foreach (var cubo in FindObjectsByType<CuboDeColor>(FindObjectsSortMode.None))
            if (cubo.transform.parent == transform) return cubo;
        return null;
    }

    private ChestNode CofreCercano()
    {
        foreach (var cofre in FindObjectsByType<ChestNode>(FindObjectsSortMode.None))
            if (cofre.IsSpawned && Vector3.Distance(transform.position, cofre.transform.position) < cofre.alcance)
                return cofre;
        return null;
    }

    private CuboDeColor CuboLibreCercano()
    {
        CuboDeColor elegido = null;
        float mejor = alcance;
        foreach (var cubo in FindObjectsByType<CuboDeColor>(FindObjectsSortMode.None))
        {
            if (cubo.LoLlevaAlguien) continue;
            float d = Vector3.Distance(transform.position, cubo.transform.position);
            if (d < mejor) { mejor = d; elegido = cubo; }
        }
        return elegido;
    }

    [Rpc(SendTo.Server, RequireOwnership = true)]
    public void PedirAgarrarRpc(NetworkObjectReference referencia)
    {
        if (!referencia.TryGet(out NetworkObject objeto)) return;
        CuboDeColor cubo = objeto.GetComponent<CuboDeColor>();
        if (cubo == null) return;

        if (cubo.LoLlevaAlguien)
        {
            Debug.Log("[Mundo] El jugador " + OwnerClientId + " quiso agarrar un cubo que ya lleva otro.");
            return;
        }
        if (CuboQueLlevo() != null) return;              // una cosa cada vez

        cubo.Agarrar(NetworkObject);
    }

    [Rpc(SendTo.Server, RequireOwnership = true)]
    public void PedirSoltarRpc(NetworkObjectReference referencia)
    {
        if (!referencia.TryGet(out NetworkObject objeto)) return;
        if (objeto.transform.parent != transform) return;    // no lo llevas tu
        objeto.GetComponent<CuboDeColor>().Soltar(Vector3.zero);
    }

    [Rpc(SendTo.Server, RequireOwnership = true)]
    public void PedirLanzarRpc(NetworkObjectReference referencia)
    {
        if (!referencia.TryGet(out NetworkObject objeto)) return;
        if (objeto.transform.parent != transform) return;
        objeto.GetComponent<CuboDeColor>().Soltar(transform.forward * velocidadLanzamiento + Vector3.up * 3f);
    }
}
