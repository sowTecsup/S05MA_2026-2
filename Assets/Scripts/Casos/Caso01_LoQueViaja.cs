// =============================================================================
// CASO 01 - Que sincroniza NetworkTransform y que ejes conviene apagar.
// ORDEN ......... despues de NetworkTransformMedido.
// GAMEOBJECT .... "Caso01" vacio en la escena, con NetworkObject · Caso01_LoQueViaja
//                 + dos capsulas en la escena, cada una con NetworkObject ·
//                   NetworkTransformMedido:
//                   "TodosLosEjes"  Syncing con todo marcado (por defecto)
//                   "Recortado"     solo Position X y Z, y Rotation Y
// ARRASTRAR ..... "TodosLosEjes" y "Recortado" a sus campos
// TECLAS ........ 1 informe · 2 parar/seguir (host) · 3 umbral 0 / por defecto (host)
// =============================================================================
using Unity.Netcode;
using UnityEngine;

// Las dos capsulas hacen el MISMO baile: circulo, bote en Y y bamboleo en X y Z.
// Mira desde un cliente: la recortada no bota ni se bambolea. Lo que no se marca no viaja.
public class Caso01_LoQueViaja : NetworkBehaviour
{
    public NetworkTransformMedido todosLosEjes;
    public NetworkTransformMedido recortado;

    private bool quietos;
    private Vector3 origenA, origenB;

    public override void OnNetworkSpawn()
    {
        origenA = todosLosEjes.transform.position;
        origenB = recortado.transform.position;
    }

    void Update()
    {
        if (IsServer && !quietos)
        {
            Bailar(todosLosEjes.transform, origenA);
            Bailar(recortado.transform, origenB);
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Debug.Log("[Caso01] TodosLosEjes: enviados " + todosLosEjes.enviados + ", recibidos " + todosLosEjes.recibidos +
                      " | Recortado: enviados " + recortado.enviados + ", recibidos " + recortado.recibidos);
            todosLosEjes.Reiniciar();
            recortado.Reiniciar();
        }

        // Quietos no superan el umbral: no se envia nada.
        if (Input.GetKeyDown(KeyCode.Alpha2) && IsServer) quietos = !quietos;

        // Con el umbral a 0 se envia en CADA tick, aunque no se muevan.
        if (Input.GetKeyDown(KeyCode.Alpha3) && IsServer)
        {
            float umbral = todosLosEjes.PositionThreshold == 0f ? 0.001f : 0f;
            todosLosEjes.PositionThreshold = umbral;
            recortado.PositionThreshold = umbral;
            Debug.Log("[Caso01] Position Threshold = " + umbral);
        }
    }

    private void Bailar(Transform t, Vector3 origen)
    {
        float s = Time.time;
        t.position = origen + new Vector3(Mathf.Cos(s) * 2f, Mathf.Abs(Mathf.Sin(s * 3f)) * 0.6f, Mathf.Sin(s) * 2f);
        t.rotation = Quaternion.Euler(Mathf.Sin(s * 4f) * 15f, s * 57f, Mathf.Cos(s * 4f) * 15f);
    }
}
