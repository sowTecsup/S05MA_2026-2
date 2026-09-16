using Unity.Netcode;
using UnityEngine;

/// <summary>
/// El golpe: EVENTO remoto. La otra mitad del combate de la semana.
///
/// Tecla F, junto a otro jugador. WASD y ESPACIO ya estan cogidos por el
/// movimiento y por talar arboles, de la semana 2 y 3.
///
/// EL PUNTO IMPORTANTE DE TODA LA SESION esta en la firma del Rpc:
///
///     [Rpc(SendTo.Server, RequireOwnership = true)]
///     public void PedirGolpeRpc(NetworkObjectReference objetivo)
///
/// - Manda la INTENCION ("quiero golpear a ese"), no el resultado ("ponle 90
///   de vida"). El servidor lee la vida buena, resta y escribe. Si dos
///   atacantes coinciden en el mismo tick, los dos golpes cuentan. Con el
///   resultado, el segundo mensaje pisaria al primero y un golpe se perderia
///   sin dejar rastro en consola. Caso 02.
///
/// - RequireOwnership = true VA ESCRITO A MANO. Con el viejo [ServerRpc] era
///   el valor por defecto; con [Rpc] el defecto es el contrario, y sin esa
///   linea cualquier cliente podria llamar al metodo desde el avatar de otro y
///   repartir golpes en su nombre. Es la trampa que ya cazamos en la semana 3.
///
/// Lo que este script NO hace, y se queda escrito para que se vea:
///
/// - El servidor NO valida la distancia ni el dano. Se fia de que el cliente
///   solo llame a esto cuando toca. Un cliente modificado puede golpear desde
///   el otro extremo del mapa y con el dano que quiera. Eso es la semana 12.
///
/// - Gana quien tiene mejor conexion, porque su peticion llega antes. El
///   servidor no rebobina nada. Eso es la semana 11.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class PlayerCombat : NetworkBehaviour
{
    public int dano = 10;
    public float alcance = 3f;

    void Update()
    {
        if (!IsOwner) return;
        if (!Input.GetKeyDown(KeyCode.F)) return;

        PlayerHealth objetivo = ObjetivoMasCercano();
        if (objetivo == null)
        {
            Debug.Log("No hay nadie a mano. Acercate a otro jugador y pulsa F.");
            return;
        }

        // Solo se manda la referencia del objetivo. El dano lo pone el
        // servidor... por ahora leyendolo de aqui, que es justo lo que la
        // semana 12 va a llamar "confiar en el cliente".
        PedirGolpeRpc(objetivo.NetworkObject);
    }

    private PlayerHealth ObjetivoMasCercano()
    {
        PlayerHealth elegido = null;
        float mejor = float.MaxValue;

        foreach (var otro in FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None))
        {
            if (otro.NetworkObjectId == NetworkObjectId) continue;   // no te pegues a ti
            if (otro.IsDead) continue;

            float d = Vector3.Distance(transform.position, otro.transform.position);
            if (d < alcance && d < mejor) { mejor = d; elegido = otro; }
        }
        return elegido;
    }

    [Rpc(SendTo.Server, RequireOwnership = true)]
    public void PedirGolpeRpc(NetworkObjectReference referencia)
    {
        if (!referencia.TryGet(out NetworkObject objetivo)) return;

        PlayerHealth victima = objetivo.GetComponent<PlayerHealth>();
        if (victima == null) return;

        victima.RecibirDano(dano);

        // Adorno: el numero flotante. Si se pierde uno, no pasa nada, porque la
        // vida ya viaja aparte y es la que manda. Por eso va NO FIABLE.
        victima.MostrarNumeroRpc(dano);
    }
}
