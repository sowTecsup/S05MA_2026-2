// =============================================================================
// CASO 06 - Cuando un cambio de posicion debe ser Teleport y no interpolarse.
// ORDEN ......... cualquiera.
// GAMEOBJECT .... "Caso06" vacio en la escena, con NetworkObject · Caso06_SaltoOVuelo
//                 + un cubo en la escena con NetworkObject · NetworkTransform (Authority Mode = Server)
// ARRASTRAR ..... el cubo al campo "Cubo"
// TECLAS ........ N mover con position (host) · M mover con Teleport (host) · B Teleport desde un cliente
// =============================================================================
using System;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

// Mira desde un cliente. Con N el cubo CRUZA volando: la interpolacion no sabe que no
// era un movimiento. Con M APARECE. Con B, en un cliente, salta la excepcion: solo
// puede hacer Teleport quien tiene autoridad.
public class Caso06_SaltoOVuelo : NetworkBehaviour
{
    public NetworkTransform cubo;
    public Vector3 lejos = new Vector3(10f, 0f, 0f);

    private bool alReves;

    void Update()
    {
        Vector3 destino = cubo.transform.position + (alReves ? -lejos : lejos);

        if (Input.GetKeyDown(KeyCode.N) && IsServer)
        {
            cubo.transform.position = destino;                                       // vuela
            alReves = !alReves;
        }

        if (Input.GetKeyDown(KeyCode.M) && IsServer)
        {
            cubo.Teleport(destino, cubo.transform.rotation, cubo.transform.localScale); // aparece
            alReves = !alReves;
        }

        if (Input.GetKeyDown(KeyCode.B))
        {
            try { cubo.Teleport(destino, cubo.transform.rotation, cubo.transform.localScale); }
            catch (Exception e) { Debug.Log("[Caso06] Rechazado: " + e.Message); }
        }
    }
}
