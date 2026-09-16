using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

/// <summary>
/// CASO 05 - Que le pasa al transform al emparentar un NetworkObject.  (*)
///
/// Agarrar algo es hacerlo HIJO de tu capsula. Dos reglas de NGO salen solas:
///   - en cliente-servidor SOLO EL SERVIDOR puede emparentar (salvo que el
///     objeto marque Allow Owner To Parent). Por eso se pide con un RPC.
///   - el padre TIENE QUE SER un NetworkObject. TrySetParent con un Transform
///     cualquiera -una mano- devuelve false y no hace nada. Por eso el objeto va
///     en la raiz de la capsula, con un desplazamiento.
///
/// Y el problema de verdad: la capsula la escribe su DUENO (Owner) y el cubo lo
/// escribe el SERVIDOR (Server). Dos autoridades, dos caminos por la red.
///
///   Cubo_EspacioMundo   Switch Transform Space When Parented APAGADO
///   Cubo_EspacioLocal   Switch Transform Space When Parented ENCENDIDO
///
/// El de mundo sincroniza DONDE ESTA EN EL MUNDO. Esa posicion la calcula el
/// servidor con donde EL cree que estas, que es donde estabas hace un RTT. En tu
/// pantalla, el cubo te persigue.
///
/// El de local sincroniza DONDE ESTA RESPECTO A TI. Ese valor no cambia mientras
/// lo llevas, asi que cada maquina lo coloca pegado a SU version de tu capsula.
///
/// Y una trampa para el que lee la documentacion por encima: al marcar la casilla,
/// NGO activa tambien Tick Sync Children. Pero ese ajuste solo sincroniza por tick
/// a los NetworkTransform con el MISMO Authority Mode. Entre Owner y Server no
/// hace nada. Lo que salva al cubo es el espacio local, no el tick.
///
/// Como se ve, en tres etapas:
///
///   ETAPA A (red perfecta)
///     1. En un CLIENTE, pulsa H: el servidor te pone los dos cubos delante.
///     2. Camina con WASD: los dos van pegados. En localhost no hay diferencia.
///
///   ETAPA B (200 ms)
///     3. Pulsa P hasta "200 ms" y camina en zigzag: el de mundo se queda atras y
///        te alcanza al pararte. El de local va pegado.
///
///   ETAPA C (soltar)
///     4. Pulsa G: el servidor los suelta donde estan (TryRemoveParent). La casilla
///        los devuelve a espacio de mundo sin tiron.
///
/// (*) Caso destacado.
/// </summary>
public class Caso05_ElHijoQueSeQuedaAtras : NetworkBehaviour
{
    public NetworkObject cuboMundo;
    public NetworkObject cuboLocal;

    void Update()
    {
        if (!IsSpawned) return;

        if (Input.GetKeyDown(KeyCode.H)) AgarrarRpc();
        if (Input.GetKeyDown(KeyCode.G)) SoltarRpc();
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
    private void AgarrarRpc(RpcParams rpcParams = default)
    {
        ulong quien = rpcParams.Receive.SenderClientId;
        NetworkObject capsula = NetworkManager.ConnectedClients[quien].PlayerObject;
        if (capsula == null) return;

        // TrySetParent con un Transform sin NetworkObject devolveria false.
        bool a = cuboMundo.TrySetParent(capsula, false);
        bool b = cuboLocal.TrySetParent(capsula, false);
        cuboMundo.transform.localPosition = new Vector3(-0.8f, 0.5f, 0.9f);
        cuboLocal.transform.localPosition = new Vector3(0.8f, 0.5f, 0.9f);
        cuboMundo.transform.localRotation = Quaternion.identity;
        cuboLocal.transform.localRotation = Quaternion.identity;

        Debug.Log("[Caso05] Cubos emparentados a la capsula del jugador " + quien +
                  " (TrySetParent: " + a + ", " + b + "). Izquierda = espacio de mundo, derecha = local.");
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
    private void SoltarRpc()
    {
        cuboMundo.TryRemoveParent(true);
        cuboLocal.TryRemoveParent(true);
        Debug.Log("[Caso05] Cubos soltados donde estaban.");
    }

    private void OnGUI()
    {
        Etiqueta(cuboMundo, "Espacio de mundo");
        Etiqueta(cuboLocal, "Espacio local");
    }

    private static void Etiqueta(Component c, string texto)
    {
        if (c == null || Camera.main == null) return;
        Vector3 p = Camera.main.WorldToScreenPoint(c.transform.position + Vector3.up * 0.8f);
        if (p.z > 0f) GUI.Label(new Rect(p.x - 60f, Screen.height - p.y, 140f, 22f), texto);
    }
}
