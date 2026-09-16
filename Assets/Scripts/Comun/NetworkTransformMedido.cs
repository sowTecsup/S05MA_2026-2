// =============================================================================
// ANDAMIAJE para los casos 01 y 04. Crealo ANTES que ellos.
// GAMEOBJECT .... se usa EN LUGAR de NetworkTransform en los objetos de esos casos
// COMPONENTES ... NetworkObject · NetworkTransformMedido
// ARRASTRAR ..... nada
// =============================================================================
using Unity.Netcode.Components;

// Un NetworkTransform que ademas CUENTA los estados que envia y recibe.
public class NetworkTransformMedido : NetworkTransform
{
    public int enviados;
    public int recibidos;
    public int recibidosNoFiables;

    // Se llama en quien tiene autoridad cada vez que sale un estado.
    protected override void OnAuthorityPushTransformState(ref NetworkTransformState estado)
    {
        enviados++;
    }

    // Se llama en los demas cada vez que llega uno.
    protected override void OnNetworkTransformStateUpdated(ref NetworkTransformState anterior, ref NetworkTransformState nuevo)
    {
        recibidos++;
        if (!nuevo.IsReliableStateUpdate()) recibidosNoFiables++;
    }

    public void Reiniciar() { enviados = recibidos = recibidosNoFiables = 0; }
}
