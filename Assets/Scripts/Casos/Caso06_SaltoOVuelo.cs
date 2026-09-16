using System;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

/// <summary>
/// CASO 06 - Cuando un cambio de posicion debe ser Teleport y no interpolarse.
///
/// La interpolacion hace muy bien su trabajo: recibe un estado nuevo y desliza
/// hasta el. Por eso, si el servidor pone un objeto en la otra punta del mapa con
/// un transform.position, los clientes lo ven CRUZAR EL MAPA VOLANDO. El
/// interpolador no sabe que eso no era un movimiento.
///
/// Teleport lo coloca SIN INTERPOLAR, y se envia siempre fiable aunque este
/// marcada la entrega no fiable. Con una regla que se aprende rompiendola: solo
/// lo puede llamar quien tiene AUTORIDAD sobre ese transform. Desde cualquier
/// otro sitio lanza:
///
///     "Teleporting on non-authoritative side is not allowed!"
///
/// En el mundo aparece dos veces:
///   - el cubo que se cae del mapa vuelve con Teleport (lo llama el servidor:
///     el cubo es Server)
///   - la reaparicion del jugador muerto: la DECIDE el servidor pero la EJECUTA
///     el dueno, porque la capsula es Owner (PlayerAvatar.ReaparecerRpc)
///
/// Y un matiz: un objeto que TODAVIA NO EXISTE en red no se teletransporta. Se
/// coloca en el Instantiate, antes del Spawn, y nace ahi. Es lo que hace el cofre.
///
/// Como se ve, en tres etapas (mira desde una ventana de CLIENTE):
///
///   ETAPA A (vuelo)
///     1. Pulsa N en el HOST: el cubo salta de esquina a esquina con
///        transform.position. En el cliente, cruza el mapa volando.
///
///   ETAPA B (salto)
///     2. Pulsa M en el HOST: lo mismo con Teleport. En el cliente, aparece.
///
///   ETAPA C (la regla)
///     3. Pulsa B en un CLIENTE: intenta el Teleport desde ahi. Excepcion: el
///        cubo es de autoridad de servidor.
/// </summary>
public class Caso06_SaltoOVuelo : NetworkBehaviour
{
    public NetworkTransform cubo;
    public Vector3 esquinaA = new Vector3(14f, 0.5f, -14f);
    public Vector3 esquinaB = new Vector3(-14f, 0.5f, -14f);

    private bool enA = true;

    void Update()
    {
        if (!IsSpawned) return;

        if (Input.GetKeyDown(KeyCode.N))
        {
            if (!IsServer) { Debug.LogWarning("[Caso06] La N es del host: el cubo es de autoridad de servidor."); return; }
            enA = !enA;
            cubo.transform.position = enA ? esquinaA : esquinaB;
            Debug.Log("[Caso06] transform.position a la otra esquina. Mira un cliente: VUELA.");
        }

        if (Input.GetKeyDown(KeyCode.M))
        {
            if (!IsServer) { Debug.LogWarning("[Caso06] La M es del host: el cubo es de autoridad de servidor."); return; }
            enA = !enA;
            cubo.Teleport(enA ? esquinaA : esquinaB, Quaternion.identity, cubo.transform.localScale);
            Debug.Log("[Caso06] Teleport a la otra esquina. Mira un cliente: APARECE.");
        }

        if (Input.GetKeyDown(KeyCode.B))
        {
            try
            {
                cubo.Teleport(esquinaA, Quaternion.identity, cubo.transform.localScale);
                Debug.Log("[Caso06] Teleport aceptado: esta maquina tiene autoridad (CanCommitToTransform = " +
                          cubo.CanCommitToTransform + "). Prueba desde un cliente.");
            }
            catch (Exception e)
            {
                Debug.Log("[Caso06] Teleport RECHAZADO en esta maquina: \"" + e.Message + "\"" +
                          "\n   CanCommitToTransform = " + cubo.CanCommitToTransform +
                          ". Solo teletransporta quien tiene autoridad.");
            }
        }
    }
}
