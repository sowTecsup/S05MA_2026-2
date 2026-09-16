// =============================================================================
// ORDEN ......... 2 de 4. Crealo ANTES que ChestNode y PlayerAvatar: los dos lo usan.
// GAMEOBJECT .... prefab "CuboDeColor" (3D Object > Cube, escala 0.5)
// COMPONENTES ... NetworkObject
//                 Rigidbody
//                 NetworkTransform  Authority Mode = Server
//                                   Switch Transform Space When Parented = SI
//                                   Use Quaternion Synchronization = SI
//                                   Use Quaternion Compression = SI
//                 NetworkRigidbody  Use Rigid Body For Motion = NO
//                 CuboDeColor
// ARRASTRAR ..... el prefab a NetworkManager > Network Prefabs Lists
// =============================================================================
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

// Un cubo que se agarra, se suelta y se lanza. Todo lo decide el servidor.
public class CuboDeColor : NetworkBehaviour
{
    private static readonly Color[] COLORES = { Color.red, Color.blue, Color.green, Color.yellow };

    public Vector3 enLaMano = new Vector3(0f, 0.5f, 0.9f);

    private Vector3 puntoDeNacimiento;

    public bool LoLlevaAlguien => transform.parent != null;

    public override void OnNetworkSpawn()
    {
        // El NetworkObjectId es igual en todas las maquinas: mismo color para todos.
        GetComponent<Renderer>().material.color = COLORES[NetworkObjectId % 4];
        puntoDeNacimiento = transform.position;
    }

    // Solo servidor.
    public void Agarrar(NetworkObject portador)
    {
        GetComponent<NetworkRigidbody>().SetIsKinematic(true);   // la fisica no tira de el
        NetworkObject.TrySetParent(portador, false);
        transform.localPosition = enLaMano;
    }

    // Solo servidor. Velocidad cero = soltar; con velocidad = lanzar.
    public void Soltar(Vector3 velocidad)
    {
        NetworkObject.TryRemoveParent(true);            // se queda donde estaba
        var cuerpo = GetComponent<NetworkRigidbody>();
        cuerpo.SetIsKinematic(false);
        cuerpo.SetLinearVelocity(velocidad);
    }

    void Update()
    {
        if (!IsServer || LoLlevaAlguien || transform.position.y > -5f) return;

        // Se cayo del mapa: vuelve con Teleport. Con transform.position, volaria de vuelta.
        GetComponent<NetworkRigidbody>().SetLinearVelocity(Vector3.zero);
        GetComponent<NetworkTransform>().Teleport(puntoDeNacimiento, Quaternion.identity, transform.localScale);
    }
}
