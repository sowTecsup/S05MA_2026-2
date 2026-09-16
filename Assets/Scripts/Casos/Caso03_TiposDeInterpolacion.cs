// =============================================================================
// CASO 03 - Que controlan los tipos de interpolacion bajo latencia.
// ORDEN ......... cualquiera.
// GAMEOBJECT .... "Caso03" vacio en la escena, con NetworkObject · Caso03_TiposDeInterpolacion
//                 + cuatro cubos en la escena, en fila, con NetworkObject · NetworkTransform:
//                   cubo 1  Interpolate = NO
//                   cubo 2  Interpolation Type = LegacyLerp
//                   cubo 3  Interpolation Type = Lerp
//                   cubo 4  Interpolation Type = SmoothDampening
// ARRASTRAR ..... los cuatro cubos a la lista "Cubos", en ese orden
// TECLAS ........ P en un cliente para meter 200 ms · 7 informe de latencia
// =============================================================================
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

// El servidor mueve los cuatro cubos en el mismo zigzag. Con 200 ms, el cubo 1 va a saltos
// y los otros tres suaves, un poco por detras: ese retraso es el precio de la suavidad.
public class Caso03_TiposDeInterpolacion : NetworkBehaviour
{
    public NetworkTransform[] cubos = new NetworkTransform[4];

    private Vector3[] origenes;

    public override void OnNetworkSpawn()
    {
        origenes = new Vector3[cubos.Length];
        for (int i = 0; i < cubos.Length; i++) origenes[i] = cubos[i].transform.position;
    }

    void Update()
    {
        if (IsServer)
        {
            float x = Mathf.PingPong(Time.time * 3f, 8f);
            float z = Mathf.PingPong(Time.time * 6f, 1.5f);
            for (int i = 0; i < cubos.Length; i++)
                cubos[i].transform.position = origenes[i] + new Vector3(x, 0f, z);
        }

        if (Input.GetKeyDown(KeyCode.Alpha7) && !IsServer)
            Debug.Log("[Caso03] Lo remoto se dibuja " + NetworkTransform.GetTickLatencyInSeconds().ToString("0.000") +
                      " s por detras del servidor.");
    }
}
