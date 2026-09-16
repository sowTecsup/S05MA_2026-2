// =============================================================================
// CASO 04 - Que ahorran la entrega no fiable y la compresion.
// ORDEN ......... despues de NetworkTransformMedido.
// GAMEOBJECT .... "Caso04" vacio en la escena, con NetworkObject · Caso04_LoQuePesaUnEstado
//                 + dos cubos en la escena, cada uno con NetworkObject · NetworkTransformMedido:
//                   "Estandar"    todo por defecto
//                   "Comprimido"  Use Unreliable Deltas = SI · Use Half Float Precision = SI
//                                 Use Quaternion Synchronization = SI · Use Quaternion Compression = SI
// ARRASTRAR ..... los dos cubos a sus campos
// TECLAS ........ P en un cliente hasta "200 ms + 20% perdida" · 9 informe
// =============================================================================
using Unity.Netcode;
using UnityEngine;

// Los dos giran igual en tres ejes. Con perdida, el Estandar lo recibe todo (tarde y
// reenviado); al Comprimido le faltan estados y se ve igual: el siguiente reemplaza al perdido.
public class Caso04_LoQuePesaUnEstado : NetworkBehaviour
{
    public NetworkTransformMedido estandar;
    public NetworkTransformMedido comprimido;

    void Update()
    {
        if (IsServer)
        {
            Vector3 giro = new Vector3(90f, 60f, 30f) * Time.deltaTime;
            estandar.transform.Rotate(giro);
            comprimido.transform.Rotate(giro);
        }

        if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            Debug.Log("[Caso04] Estandar: recibidos " + estandar.recibidos + " (no fiables " + estandar.recibidosNoFiables + ")" +
                      " | Comprimido: recibidos " + comprimido.recibidos + " (no fiables " + comprimido.recibidosNoFiables + ")");
            estandar.Reiniciar();
            comprimido.Reiniciar();
        }
    }
}
