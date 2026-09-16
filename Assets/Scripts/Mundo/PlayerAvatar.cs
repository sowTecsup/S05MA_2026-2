using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

/// <summary>
/// El avatar del jugador. WASD para moverse, ESPACIO para talar un arbol.
///
/// Lo que cambia respecto a la semana 4: LOS DOS RPC DE MOVER HAN DESAPARECIDO.
///
/// Hasta hoy el dueno se movia en su pantalla y mandaba su posicion con un
/// [Rpc] en cada frame: unos 60 mensajes fiables por segundo por jugador, al
/// que llegaba tarde no le llegaba nada y los demas lo veian a saltos. La
/// posicion es ESTADO, y el criterio de la semana 4 decia que el estado no
/// viaja como evento.
///
/// Ahora el prefab lleva un NetworkTransform, y el Update solo mueve el
/// transform local. Replicarlo es trabajo del componente. Lo que se decide
/// esta en el INSPECTOR del prefab, no en este archivo:
///
///   Authority Mode ........ Owner   (con Server el dueno NO se mueve: caso 02)
///   Sync Position ......... X y Z   (el suelo es plano: Y no viaja)
///   Sync Rotation ......... solo Y  (la capsula no se inclina)
///   Sync Scale ............ nada
///   Use Unreliable Deltas . si      (flujo continuo: la siguiente reemplaza)
///   Use Half Float ........ si      (mapa pequeno, precision de sobra)
///
/// Y el precio de Owner, que se queda escrito: el cliente decide donde esta.
/// Un cliente modificado puede correr el doble o teletransportarse. Validarlo
/// es la semana 12. Moverse con autoridad de servidor sin sentir el retardo es
/// la semana 10.
///
/// El color sigue siendo una NetworkVariable de escritura del DUENO, la unica
/// fila de la matriz que lo permite: mentir sobre tu color no da ventaja.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class PlayerAvatar : NetworkBehaviour
{
    public float moveSpeed = 5f;

    public NetworkVariable<Color> miColor = new NetworkVariable<Color>(
        Color.white,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);

    private static readonly Color[] PALETA =
    {
        new Color(0.85f, 0.25f, 0.25f), new Color(0.25f, 0.55f, 0.90f),
        new Color(0.30f, 0.75f, 0.35f), new Color(0.90f, 0.70f, 0.20f),
        new Color(0.65f, 0.35f, 0.80f), new Color(0.20f, 0.75f, 0.75f)
    };

    private Renderer visual;

    void Awake() { visual = GetComponent<Renderer>(); }

    public override void OnNetworkSpawn()
    {
        miColor.OnValueChanged += AlCambiarColor;
        Pintar(miColor.Value);           // el que ya venia (semana 3)

        if (IsOwner)
        {
            miColor.Value = PALETA[(int)(OwnerClientId % (ulong)PALETA.Length)];
            Debug.Log("Este avatar es mio. WASD mover, ESPACIO talar, F golpear, " +
                      "E abrir el cofre o agarrar, Q lanzar.");
        }
    }

    public override void OnNetworkDespawn()
    {
        miColor.OnValueChanged -= AlCambiarColor;
    }

    private void AlCambiarColor(Color anterior, Color nuevo) { Pintar(nuevo); }

    private void Pintar(Color c)
    {
        if (visual == null) return;
        var salud = GetComponent<PlayerHealth>();
        if (salud != null && salud.IsDead) return;   // el gris de la muerte manda
        visual.material.color = c;
    }

    void Update()
    {
        if (!IsOwner) return;

        var salud = GetComponent<PlayerHealth>();
        if (salud != null && salud.IsDead) return;

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        Vector3 paso = new Vector3(h, 0f, v) * moveSpeed * Time.deltaTime;

        // Ya no hay PedirMoverRpc: la posicion la replica NetworkTransform.
        transform.Translate(paso, Space.World);
        if (paso != Vector3.zero) transform.rotation = Quaternion.LookRotation(paso);

        if (Input.GetKeyDown(KeyCode.Space)) GolpearElArbolMasCercano();
    }

    private void GolpearElArbolMasCercano()
    {
        TreeNode elegido = null;
        float mejor = float.MaxValue;

        foreach (var arbol in FindObjectsByType<TreeNode>(FindObjectsSortMode.None))
        {
            float d = Vector3.Distance(transform.position, arbol.transform.position);
            if (d < arbol.alcance && d < mejor) { mejor = d; elegido = arbol; }
        }

        if (elegido == null) return;
        elegido.Golpear(miColor.Value);
    }

    /// <summary>
    /// La reaparicion: la DECIDE el servidor y la EJECUTA el dueno.
    ///
    /// Con autoridad Owner, el servidor no puede mover esta capsula: su
    /// NetworkTransform le devolveria la posicion que manda el dueno. Y aunque
    /// pudiera, un transform.position a la otra punta del mapa se INTERPOLA: los
    /// demas verian al muerto cruzar el mundo volando.
    ///
    /// Teleport resuelve las dos cosas, con una regla: solo lo puede llamar
    /// quien tiene autoridad sobre ese transform. Desde el servidor lanzaria
    /// "Teleporting on non-authoritative side is not allowed!". Caso 06.
    /// </summary>
    [Rpc(SendTo.Owner)]
    public void ReaparecerRpc(Vector3 punto)
    {
        GetComponent<NetworkTransform>().Teleport(punto, transform.rotation, transform.localScale);
        Debug.Log("[Mundo] Reaparezco con Teleport en " + punto + ".");
    }
}
