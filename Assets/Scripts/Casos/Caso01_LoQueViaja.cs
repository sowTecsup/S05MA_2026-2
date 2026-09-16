using Unity.Netcode;
using UnityEngine;

/// <summary>
/// CASO 01 - Que sincroniza NetworkTransform y que ejes conviene apagar.
///
/// NetworkTransform no manda "la posicion" en cada frame. En cada TICK de red
/// (30 por segundo por defecto) pasa dos filtros, y solo envia lo que sobrevive:
///
///   1. el eje tiene que estar marcado en Syncing (Position/Rotation/Scale X Y Z)
///   2. el cambio tiene que superar el umbral (Position Threshold = 0.001)
///
/// Dos maniquis hacen EXACTAMENTE el mismo baile, que mueve el servidor: van en
/// circulo, botan en Y y se bambolean en X y Z.
///
///   Maniqui_TodosLosEjes   Syncing con todo marcado (lo que viene por defecto)
///   Maniqui_Recortado      solo Position X/Z y Rotation Y, como la capsula
///
/// Como se ve, en tres etapas:
///
///   ETAPA A (lo que no se marca no viaja... ni llega)
///     1. Mira los dos maniquis desde una ventana de CLIENTE. El recortado no
///        bota ni se bambolea: esos ejes no viajan. Si el bote fuera parte del
///        juego, apagarlo seria un error; si es adorno, se anima en local.
///
///   ETAPA B (el umbral)
///     2. Pulsa 2 en el host: los maniquis se quedan QUIETOS.
///     3. Pulsa 1 en el host: los enviados por segundo caen a cero. Quieto no
///        supera el umbral, y un estado que no cambia no se envia.
///     4. Pulsa 3 en el host: Position Threshold a 0. Pulsa 1 otra vez: ahora
///        envian en CADA tick aunque no se muevan. Lo dice el propio codigo de
///        NGO: "setting this to zero will update position every network tick
///        whether it changed or not".
///
///   ETAPA C (la cuenta)
///     5. Pulsa 2 para que vuelvan a bailar y 1 en el cliente: los dos reciben
///        el mismo numero de estados, pero el recortado lleva 3 valores y el
///        otro hasta 9. Cuantos BYTES son exactamente, lo medimos la semana 6
///        con el Network Profiler.
/// </summary>
public class Caso01_LoQueViaja : NetworkBehaviour
{
    public NetworkTransformMedido todosLosEjes;
    public NetworkTransformMedido recortado;

    public float radio = 2f;
    public float velocidad = 1.2f;

    private bool quietos;
    private bool umbralCero;

    void Update()
    {
        if (!IsSpawned) return;

        if (IsServer && !quietos)
        {
            float t = Time.time * velocidad;
            Bailar(todosLosEjes, t);
            Bailar(recortado, t);
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Debug.Log("[Caso01] INFORME en esta maquina (" + (IsServer ? "servidor" : "cliente") + "):" +
                      "\n   " + todosLosEjes.Informe() +
                      "\n   " + recortado.Informe());
            todosLosEjes.Reiniciar();
            recortado.Reiniciar();
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            if (!IsServer) { Debug.LogWarning("[Caso01] El 2 es del host."); return; }
            quietos = !quietos;
            Debug.Log("[Caso01] Maniquis " + (quietos ? "QUIETOS. Pulsa 1 dentro de unos segundos." : "bailando otra vez."));
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            if (!IsServer) { Debug.LogWarning("[Caso01] El 3 es del host: el umbral lo aplica quien tiene autoridad."); return; }
            umbralCero = !umbralCero;
            float umbral = umbralCero ? 0f : 0.001f;
            todosLosEjes.PositionThreshold = umbral;
            recortado.PositionThreshold = umbral;
            Debug.Log("[Caso01] Position Threshold = " + umbral +
                      (umbralCero ? "  -> ahora se envia en cada tick aunque no cambie nada." : "  (valor por defecto)"));
        }
    }

    private void Bailar(NetworkTransformMedido maniqui, float t)
    {
        Vector3 origen = maniqui == todosLosEjes ? new Vector3(-12f, 1f, -6f) : new Vector3(-6f, 1f, -6f);

        maniqui.transform.position = origen + new Vector3(Mathf.Cos(t) * radio, Mathf.Abs(Mathf.Sin(t * 3f)) * 0.6f, Mathf.Sin(t) * radio);
        maniqui.transform.rotation = Quaternion.Euler(Mathf.Sin(t * 4f) * 15f, t * Mathf.Rad2Deg, Mathf.Cos(t * 4f) * 15f);
    }

    private void OnGUI()
    {
        Etiqueta(todosLosEjes, "Todos los ejes");
        Etiqueta(recortado, "Recortado (X Z + rot Y)");
    }

    private static void Etiqueta(Component c, string texto)
    {
        if (c == null || Camera.main == null) return;
        Vector3 p = Camera.main.WorldToScreenPoint(c.transform.position + Vector3.up * 1.4f);
        if (p.z > 0f) GUI.Label(new Rect(p.x - 80f, Screen.height - p.y, 200f, 22f), texto);
    }
}
