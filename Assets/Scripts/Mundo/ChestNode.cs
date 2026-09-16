using Unity.Netcode;
using UnityEngine;

/// <summary>
/// EL AVANCE DEL MUNDO COMPARTIDO - el cofre.
///
/// Como en Fortnite, pero simplificado: te acercas, MANTIENES la E y el cofre
/// se va abriendo. Si sueltas antes de tiempo, se corta. Al abrirse suelta unos
/// cubos de colores alrededor y desaparece.
///
/// Y la regla que lo hace multijugador: MIENTRAS ALGUIEN LO ESTA ABRIENDO, NADIE
/// MAS PUEDE. Quien lo abre es ESTADO, no evento -el que llega a mitad tiene que
/// ver que esta ocupado- asi que va en una NetworkVariable de escritura de
/// servidor. Es la semana 3 y la 4 aplicadas, nada nuevo:
///
///   abriendo  quien lo esta abriendo. NADIE si esta libre.
///   progreso  de 0 a 1. Lo escribe el servidor, lo pintan todos.
///
/// Los dos RPC van con RequireOwnership = false, ESCRITO A MANO. El cofre es del
/// servidor: si exigiera ser dueno, ningun jugador podria abrirlo. Quien lo pide
/// se lee del propio mensaje (RpcParams), no de un parametro que mande el
/// cliente, que podria mentir.
///
/// Dos lineas que no son el tema de hoy y se usan tal cual:
///   cubo.Spawn()             nacer en caliente: ya lo vimos en la semana 2
///   NetworkObject.Despawn()  desaparecer: la semana 7 explica el ciclo entero
///
/// Fijate en DONDE se coloca cada cubo: en el Instantiate, ANTES del Spawn.
/// Un objeto que todavia no existe en red no se teletransporta: nace ahi.
///
/// Lo que se queda mal a proposito: el servidor no comprueba que quien abre
/// este cerca del cofre. Se fia del cliente. Semana 12.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class ChestNode : NetworkBehaviour
{
    public const ulong NADIE = ulong.MaxValue;

    public NetworkObject cuboPrefab;
    public int cantidad = 4;
    public float segundosParaAbrir = 1.5f;
    public float alcance = 2.5f;

    public NetworkVariable<ulong> abriendo = new NetworkVariable<ulong>(
        NADIE,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public NetworkVariable<float> progreso = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    void Update()
    {
        if (!IsSpawned || !IsServer) return;
        if (abriendo.Value == NADIE) return;

        // Si el que lo abria se ha desconectado, el cofre queda libre.
        if (!NetworkManager.ConnectedClients.ContainsKey(abriendo.Value))
        {
            Liberar();
            return;
        }

        progreso.Value = Mathf.Min(1f, progreso.Value + Time.deltaTime / segundosParaAbrir);
        if (progreso.Value >= 1f) Abrir();
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
    public void EmpezarAbrirRpc(RpcParams rpcParams = default)
    {
        ulong quien = rpcParams.Receive.SenderClientId;

        if (abriendo.Value != NADIE && abriendo.Value != quien)
        {
            OcupadoRpc(abriendo.Value, RpcTarget.Single(quien, RpcTargetUse.Temp));
            return;
        }
        abriendo.Value = quien;
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
    public void DejarDeAbrirRpc(RpcParams rpcParams = default)
    {
        // Solo corta quien lo estaba abriendo: soltar la E no te deja cortarle
        // la apertura a otro.
        if (abriendo.Value != rpcParams.Receive.SenderClientId) return;
        Liberar();
    }

    [Rpc(SendTo.SpecifiedInParams)]
    private void OcupadoRpc(ulong quienLoAbre, RpcParams rpcParams)
    {
        Debug.Log("[Cofre] Lo esta abriendo el jugador " + quienLoAbre + ". Espera a que termine.");
    }

    private void Liberar()
    {
        abriendo.Value = NADIE;
        progreso.Value = 0f;
    }

    private void Abrir()
    {
        for (int i = 0; i < cantidad; i++)
        {
            float angulo = i * Mathf.PI * 2f / cantidad;
            Vector3 punto = transform.position + new Vector3(Mathf.Cos(angulo) * 1.8f, 1f, Mathf.Sin(angulo) * 1.8f);

            NetworkObject cubo = Instantiate(cuboPrefab, punto, Random.rotation);
            cubo.GetComponent<CuboDeColor>().indiceColor = i;
            cubo.Spawn();                               // semana 2
        }

        Debug.Log("[Cofre] Abierto por el jugador " + abriendo.Value + ": " + cantidad + " cubos en el suelo.");
        NetworkObject.Despawn();                        // semana 7
    }

    // Barra de progreso encima del cofre, en TODAS las pantallas: lee el estado.
    private void OnGUI()
    {
        if (!IsSpawned || abriendo.Value == NADIE || Camera.main == null) return;

        Vector3 p = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 1.5f);
        if (p.z < 0f) return;

        var caja = new Rect(p.x - 60f, Screen.height - p.y - 12f, 120f, 24f);
        GUI.Box(caja, "");
        GUI.Box(new Rect(caja.x, caja.y, caja.width * progreso.Value, caja.height), "");
        GUI.Label(new Rect(caja.x + 6f, caja.y + 3f, caja.width, caja.height),
                  "Jugador " + abriendo.Value + "  " + Mathf.RoundToInt(progreso.Value * 100f) + "%");
    }
}
