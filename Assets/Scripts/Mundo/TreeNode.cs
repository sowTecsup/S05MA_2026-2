using Unity.Netcode;
using UnityEngine;

/// <summary>
/// EL AVANCE DEL MUNDO COMPARTIDO - el arbol, ahora con estado.
///
/// La semana pasada el arbol se reclamaba y se talaba, pero todo lo que sabia
/// del mundo vivia en un ClientRpc: al que se conectaba tarde no le llegaba
/// nada. Hoy el arbol GUARDA lo que es, en vez de anunciarlo.
///
/// Dos NetworkVariable, las dos de escritura de servidor:
///   colorDueno    de quien es. Color.gray significa libre.
///   vidaRestante  cuantos hachazos aguanta.
///
/// Y con eso muere la limitacion de la semana 2: antes deduciamos el dueno de
/// la propiedad del NetworkObject, y como el host ES el servidor, "libre" y
/// "del host" eran indistinguibles. Ahora el dueno no se deduce: se guarda.
/// Color.gray quiere decir libre para todos por igual, tambien para el host.
///
/// Fijate en OnNetworkSpawn: pinta el valor QUE YA VENIA y ademas se suscribe.
/// Las dos cosas. Si solo se suscribiera, el que llega tarde veria el arbol
/// gris aunque tenga dueno - es el caso 05, aqui aplicado.
///
/// Uso: acercate con el avatar y pulsa ESPACIO.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class TreeNode : NetworkBehaviour
{
    public const int VIDA_MAXIMA = 5;
    public float alcance = 3f;
    public float segundosParaRebrotar = 8f;

    public NetworkVariable<Color> colorDueno = new NetworkVariable<Color>(
        Color.gray,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public NetworkVariable<int> vidaRestante = new NetworkVariable<int>(
        VIDA_MAXIMA,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private Renderer visual;
    private float rebrotaEn = -1f;

    void Awake()
    {
        visual = GetComponent<Renderer>();
    }

    public override void OnNetworkSpawn()
    {
        colorDueno.OnValueChanged += AlCambiarColor;
        vidaRestante.OnValueChanged += AlCambiarVida;

        // El estado que YA venia en el mensaje de spawn. Sin esta linea, quien
        // se conecta con la partida empezada ve el bosque entero gris.
        Pintar(colorDueno.Value);
    }

    public override void OnNetworkDespawn()
    {
        colorDueno.OnValueChanged -= AlCambiarColor;
        vidaRestante.OnValueChanged -= AlCambiarVida;
    }

    private void AlCambiarColor(Color anterior, Color nuevo) { Pintar(nuevo); }

    private void AlCambiarVida(int anterior, int nuevo)
    {
        Debug.Log("[" + name + "] vida " + anterior + " -> " + nuevo);
    }

    private void Pintar(Color c)
    {
        if (visual != null) visual.material.color = c;
    }

    /// <summary>Lo llama el avatar del jugador que esta cerca y pulsa ESPACIO.</summary>
    public void Golpear(Color colorDelJugador)
    {
        GolpearRpc(colorDelJugador);
    }

    // Cualquiera puede pedirlo: el que reclama todavia NO es el dueno, asi que
    // exigir propiedad aqui haria imposible reclamar nada. Es la misma asimetria
    // de la semana pasada, escrita ya en la API de hoy.
    [Rpc(SendTo.Server, RequireOwnership = false)]
    private void GolpearRpc(Color colorDelJugador, RpcParams p = default)
    {
        if (vidaRestante.Value <= 0) return;

        // Libre: se lo lleva el primero que llega.
        if (colorDueno.Value == Color.gray)
        {
            colorDueno.Value = colorDelJugador;
            Debug.Log("[" + name + "] reclamado por el cliente " + p.Receive.SenderClientId);
        }
        else if (colorDueno.Value != colorDelJugador)
        {
            Debug.Log("[" + name + "] el cliente " + p.Receive.SenderClientId +
                      " intento talar un arbol ajeno. Rechazado.");
            return;
        }

        vidaRestante.Value = vidaRestante.Value - 1;

        if (vidaRestante.Value == 0)
        {
            colorDueno.Value = Color.gray;
            rebrotaEn = Time.time + segundosParaRebrotar;
            Debug.Log("[" + name + "] agotado. Rebrota en " + segundosParaRebrotar + " s.");
        }
    }

    void Update()
    {
        // El temporizador corre SOLO en el servidor. Si corriera en todas las
        // maquinas, cada una rebrotaria el arbol por su cuenta y en su momento.
        if (!IsServer || rebrotaEn < 0f || Time.time < rebrotaEn) return;

        rebrotaEn = -1f;
        vidaRestante.Value = VIDA_MAXIMA;
        Debug.Log("[" + name + "] ha rebrotado.");
    }
}
