using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

/// <summary>
/// CASO 03 - Que controlan los tipos de interpolacion bajo latencia.  (*)
///
/// Con 200 ms y jitter los estados no llegan espaciados: llegan a rachas. Si el
/// cliente los aplicara al llegar, lo remoto iria a saltos. Con Interpolate
/// marcado (viene asi), el cliente los GUARDA en un buffer y dibuja un poco POR
/// DETRAS, deslizandose entre dos estados que ya tiene. Ese retraso no es un
/// defecto: es el precio de la suavidad.
///
/// Cuatro cubos recorren el MISMO zigzag, movidos por el servidor, uno por fila:
///
///   Fila 1  Interpolate apagado
///   Fila 2  Interpolation Type = LegacyLerp       (el de por defecto)
///   Fila 3  Interpolation Type = Lerp
///   Fila 4  Interpolation Type = SmoothDampening
///
/// Lo que dice el codigo de NGO de cada uno:
///   LegacyLerp       lineal, como las versiones antiguas. NO usa la latencia
///                    medida para el tamano del buffer. El mas barato.
///   Lerp             lineal, con un buffer que mantiene el tiempo hasta el
///                    objetivo cuando la latencia sube.
///   SmoothDampening  amortigua segun la velocidad del cambio. Suave en los
///                    giros, y el mas caro: lo dice el propio codigo.
///
/// Como se ve, en tres etapas:
///
///   ETAPA A (red perfecta)
///     1. Mira desde un CLIENTE: las cuatro filas casi iguales. En localhost
///        todo interpola bien, y por eso esto nunca se ve en tu maquina.
///
///   ETAPA B (200 ms)
///     2. Pulsa P en ese cliente hasta "200 ms". La fila 1 va a saltos; las
///        otras tres, suaves y un poco por detras del servidor.
///     3. Pulsa 7: cuanto va por detras (tick latency en segundos) y el RTT.
///     4. Graba las cuatro filas a la vez: ese video es la tabla de la sesion.
///
///   ETAPA C (red mala)
///     5. P otra vez, "200 ms + 20% perdida". Fijate en las esquinas del zigzag:
///        ahi es donde se distinguen los tres tipos.
///     6. El 6 (en el host) para y reanuda el zigzag.
///
/// Una cosa que NetworkTransform NO hace: extrapolar. Adivinar hacia donde sigue
/// el cubo quitaria el retraso, pero cuando adivina mal atraviesa paredes. NGO
/// elige no adivinar.
///
/// (*) Caso destacado.
/// </summary>
public class Caso03_TiposDeInterpolacion : NetworkBehaviour
{
    public NetworkTransform[] filas = new NetworkTransform[4];
    public float velocidad = 3f;
    public float largo = 8f;

    private bool pausado;
    private float t;

    private static readonly string[] NOMBRES =
        { "Sin interpolar", "LegacyLerp", "Lerp", "SmoothDampening" };

    void Update()
    {
        if (!IsSpawned) return;

        if (IsServer && !pausado)
        {
            t += Time.deltaTime * velocidad;
            for (int i = 0; i < filas.Length; i++)
            {
                if (filas[i] == null) continue;
                // Zigzag con esquinas marcadas: una onda triangular.
                float x = Mathf.PingPong(t, largo) - largo * 0.5f;
                float z = Mathf.PingPong(t * 2f, 1.5f);
                filas[i].transform.position = new Vector3(x, 0.5f, 12f + i * 2.2f + z);
            }
        }

        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            if (!IsServer) { Debug.LogWarning("[Caso03] El 6 es del host."); return; }
            pausado = !pausado;
            Debug.Log("[Caso03] Zigzag " + (pausado ? "parado." : "en marcha."));
        }

        if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            if (IsServer)
            {
                Debug.Log("[Caso03] En el servidor no hay nada que interpolar: el mueve los cubos. Pulsa 7 en un cliente.");
                return;
            }
            ulong rtt = NetworkManager.NetworkConfig.NetworkTransport.GetCurrentRtt(NetworkManager.ServerClientId);
            Debug.Log("[Caso03] INFORME en el cliente " + NetworkManager.LocalClientId + ":" +
                      "\n   RTT con el servidor ............. " + rtt + " ms" +
                      "\n   ticks de retraso del buffer ...... " + NetworkTransform.GetTickLatency().ToString("0.00") +
                      "\n   segundos de retraso del buffer ... " + NetworkTransform.GetTickLatencyInSeconds().ToString("0.000") +
                      "\n   (con Interpolate, lo remoto se dibuja ese tiempo POR DETRAS del servidor)");
        }
    }

    private void OnGUI()
    {
        if (Camera.main == null) return;
        for (int i = 0; i < filas.Length; i++)
        {
            if (filas[i] == null) continue;
            Vector3 p = Camera.main.WorldToScreenPoint(new Vector3(-largo * 0.5f - 1f, 0.5f, 12f + i * 2.2f));
            if (p.z > 0f) GUI.Label(new Rect(p.x - 150f, Screen.height - p.y - 10f, 150f, 22f), NOMBRES[i]);
        }
    }
}
