using Unity.Netcode;
using UnityEngine;

/// <summary>
/// La vida del jugador: ESTADO replicado, escrito solo por el servidor.
///
/// Es la mitad "estado" del combate de esta semana. La otra mitad -el golpe-
/// vive en PlayerCombat y es un evento. Las dos juntas son el patron de la
/// sesion: la vida CONVERGE y el golpe SE VE.
///
/// Tres decisiones tomadas aqui, y las tres se defienden en la matriz de
/// autoridad del README:
///
/// 1. ESCRITURA DE SERVIDOR. Si el cliente pudiera escribir su propia vida,
///    se curaria solo. Es la fila mas obvia de la matriz y la primera que
///    audita la semana 12.
///
/// 2. estaMuerto NO ES UNA NetworkVariable. Se deduce de la vida (IsDead).
///    Replicarlo seria pagar dos veces por el mismo dato y arriesgarse a que
///    los dos deltas lleguen en distinto orden: vida 0 y estaMuerto false a la
///    vez, o sea vivo y muerto al mismo tiempo. El caso 05 lo provoca a
///    proposito para que se vea.
///
/// 3. EL EVENTO DEL GOLPE LLEVA SUS DATOS DENTRO. GolpeRecibidoRpc recibe el
///    dano y la vida resultante como parametros y NO lee vida.Value. Si lo
///    leyera, pintaria el valor viejo: el Rpc sale en el frame y el delta de
///    la variable espera al tick. Ese es el caso 03.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class PlayerHealth : NetworkBehaviour
{
    public int vidaMaxima = 100;
    public float segundosParaReaparecer = 3f;

    public NetworkVariable<int> vida = new NetworkVariable<int>(
        100,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    // Estado DERIVADO: no viaja por red, se calcula donde haga falta.
    public bool IsDead => vida.Value <= 0;

    private Renderer visual;
    private PlayerAvatar avatar;

    void Awake()
    {
        visual = GetComponent<Renderer>();
        avatar = GetComponent<PlayerAvatar>();
    }

    public override void OnNetworkSpawn()
    {
        vida.OnValueChanged += AlCambiarLaVida;

        // La leccion de la semana 3, aplicada: el valor inicial NO dispara el
        // callback, asi que hay que leerlo a mano al entrar. Sin esta linea,
        // el que llega tarde ve vivo a un jugador muerto.
        AlCambiarLaVida(0, vida.Value);

        if (IsServer) vida.Value = vidaMaxima;
    }

    public override void OnNetworkDespawn()
    {
        vida.OnValueChanged -= AlCambiarLaVida;
    }

    private void AlCambiarLaVida(int anterior, int actual)
    {
        Pintar();
        if (IsOwner && actual < anterior)
            Debug.Log("Me han quitado " + (anterior - actual) + " de vida. Me quedan " + actual + ".");
    }

    private void Pintar()
    {
        if (visual == null) return;
        // Muerto = gris. Vivo = el color del jugador, que es de la semana 3.
        visual.material.color = IsDead ? Color.gray
                                       : (avatar != null ? avatar.miColor.Value : Color.white);
    }

    /// <summary>
    /// Aplica dano. SOLO el servidor entra aqui: lo llama PlayerCombat despues
    /// de recibir la INTENCION del atacante. El cliente nunca manda el
    /// resultado, porque dos clientes leerian el mismo valor y el segundo
    /// pisaria al primero (caso 02).
    /// </summary>
    public void RecibirDano(int dano)
    {
        if (!IsServer) return;
        if (IsDead) return;                  // a un muerto no se le pega

        int antes = vida.Value;
        vida.Value = Mathf.Max(0, antes - dano);

        // El evento de acompanamiento. Va aparte del estado y lleva su dato.
        GolpeRecibidoRpc(dano, vida.Value);

        if (IsDead)
        {
            MuerteRpc();                     // fiable: cambia lo que puedes hacer
            Invoke(nameof(Reaparecer), segundosParaReaparecer);
        }
    }

    private void Reaparecer()
    {
        if (!IsServer) return;
        vida.Value = vidaMaxima;

        // Semana 5: el servidor ya NO mueve la capsula. Su NetworkTransform es
        // de autoridad Owner, asi que se le pide al dueno que se teletransporte.
        // Ver PlayerAvatar.ReaparecerRpc.
        Vector3 punto = new Vector3(Random.Range(-6f, 6f), 1f, Random.Range(-6f, 6f));
        avatar.ReaparecerRpc(punto);
        Debug.Log("[Mundo] Reaparece el jugador " + OwnerClientId + " con " + vida.Value + " de vida.");
    }

    // Un evento por golpe. Con la vida sola no bastaria: tres golpes en un tick
    // se colapsan en un unico cambio y sonaria una vez (caso 02).
    [Rpc(SendTo.Everyone)]
    public void GolpeRecibidoRpc(int dano, int vidaDespues)
    {
        // OJO: no se lee vida.Value aqui. El dato viene en el mensaje.
        Debug.Log("[Mundo] Golpe de " + dano + " al jugador " + OwnerClientId +
                  ". Le quedan " + vidaDespues + ".");
    }

    // El numero flotante es adorno puro: si un paquete se pierde, nadie lo
    // nota, porque la vida ya converge por su cuenta. Unreliable NO quiere
    // decir "mas rapido": quiere decir "no se reenvia si se pierde".
    [Rpc(SendTo.Everyone, Delivery = RpcDelivery.Unreliable)]
    public void MostrarNumeroRpc(int dano)
    {
        Debug.Log("[Mundo]   -" + dano);
    }

    [Rpc(SendTo.Everyone)]
    public void MuerteRpc()
    {
        Debug.Log("[Mundo] Ha muerto el jugador " + OwnerClientId +
                  ". Reaparece en " + segundosParaReaparecer + "s.");
    }
}
