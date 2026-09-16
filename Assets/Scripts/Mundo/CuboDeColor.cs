using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

/// <summary>
/// EL AVANCE DEL MUNDO COMPARTIDO - los cubos que suelta el cofre.
///
/// Se agarran (E), se sueltan (E otra vez) y se lanzan (Q). Si alguien lleva
/// uno, nadie mas puede agarrarlo: "lo lleva alguien" es tener padre, y el
/// padre es estado replicado por NGO.
///
/// Todo lo interesante esta en el PREFAB, no aqui. Tres componentes:
///
///   Rigidbody          para caer y rodar cuando se lanza
///   NetworkRigidbody   con "Use Rigid Body For Motion" APAGADO. Encendido,
///                      "Switch Transform Space When Parented" no funciona:
///                      lo dice el propio codigo de NGO.
///                      Y con "Auto Update Kinematic State" encendido (por
///                      defecto) el cubo es CINEMATICO en quien no tiene
///                      autoridad: la fisica corre SOLO en el servidor y los
///                      clientes ven el vuelo interpolado.
///   NetworkTransform   Authority Mode SERVER: nadie mas decide donde esta.
///                      Switch Transform Space When Parented ENCENDIDO: mientras
///                      lo llevas sincroniza su posicion RESPECTO A TI, no al
///                      mundo. Sin eso, en tu pantalla el cubo te persigue: el
///                      servidor lo coloca donde el cree que estas, que es donde
///                      estabas hace 200 ms. Caso 05.
///                      Rotacion en los TRES ejes: un cubo lanzado da vueltas.
///                      Aqui si tiene sentido Use Quaternion Synchronization con
///                      compresion; en la capsula, que solo gira en Y, no. Caso 04.
///
/// Si se cae del mapa, el servidor lo devuelve a donde nacio con TELEPORT. Con
/// un transform.position, los clientes lo verian volar desde el vacio hasta su
/// sitio: la interpolacion no sabe que eso no era un movimiento. Caso 06.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(Rigidbody))]
public class CuboDeColor : NetworkBehaviour
{
    public static readonly Color[] PALETA =
    {
        new Color(0.90f, 0.30f, 0.30f), new Color(0.30f, 0.60f, 0.95f),
        new Color(0.35f, 0.80f, 0.40f), new Color(0.95f, 0.80f, 0.25f)
    };

    public Vector3 enLaMano = new Vector3(0f, 0.5f, 0.9f);
    public float alturaMinima = -5f;

    // Lo fija el cofre ANTES del Spawn. En el servidor se convierte en estado.
    [HideInInspector] public int indiceColor;

    public NetworkVariable<Color> color = new NetworkVariable<Color>(
        Color.white,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private Vector3 puntoDeNacimiento;
    private Renderer visual;

    public bool LoLlevaAlguien => transform.parent != null;

    void Awake() { visual = GetComponent<Renderer>(); }

    public override void OnNetworkSpawn()
    {
        color.OnValueChanged += AlCambiarColor;
        Pintar(color.Value);

        if (IsServer)
        {
            color.Value = PALETA[indiceColor % PALETA.Length];
            puntoDeNacimiento = transform.position;
        }
    }

    public override void OnNetworkDespawn()
    {
        color.OnValueChanged -= AlCambiarColor;
    }

    private void AlCambiarColor(Color anterior, Color nuevo) { Pintar(nuevo); }

    private void Pintar(Color c)
    {
        if (visual != null) visual.material.color = c;
    }

    /// <summary>Solo servidor. El portador tiene que ser un NetworkObject.</summary>
    public void Agarrar(NetworkObject portador)
    {
        if (!IsServer) return;

        // Mientras se lleva, la fisica no puede tirar de el.
        GetComponent<NetworkRigidbody>().SetIsKinematic(true);

        NetworkObject.TrySetParent(portador, false);
        transform.localPosition = enLaMano;
        transform.localRotation = Quaternion.identity;
    }

    /// <summary>Solo servidor. Velocidad cero = soltar; con velocidad = lanzar.</summary>
    public void Soltar(Vector3 velocidad)
    {
        if (!IsServer) return;

        NetworkObject.TryRemoveParent(true);             // se queda donde estaba

        var cuerpo = GetComponent<NetworkRigidbody>();
        cuerpo.SetIsKinematic(false);
        cuerpo.SetLinearVelocity(velocidad);
    }

    void Update()
    {
        if (!IsSpawned || !IsServer) return;
        if (LoLlevaAlguien || transform.position.y > alturaMinima) return;

        // Se ha caido del mapa. Teleport, no transform.position: ver arriba.
        GetComponent<NetworkRigidbody>().SetLinearVelocity(Vector3.zero);
        GetComponent<NetworkTransform>().Teleport(puntoDeNacimiento, Quaternion.identity, transform.localScale);
        Debug.Log("[Mundo] Un cubo se cayo del mapa: vuelve con Teleport a donde nacio.");
    }
}
