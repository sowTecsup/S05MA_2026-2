using Unity.Netcode.Components;
using UnityEngine;

/// <summary>
/// ANDAMIAJE - no es el tema de hoy.
///
/// Un NetworkTransform identico al de NGO que ademas CUENTA. Lo usan los casos
/// 01, 03 y 04 para poder decir un numero en vez de "parece que va mejor".
///
/// Se apoya en dos metodos virtuales que trae el propio componente:
///
///   OnAuthorityPushTransformState    se llama en QUIEN TIENE AUTORIDAD cada vez
///                                    que sale un estado hacia los demas
///   OnNetworkTransformStateUpdated   se llama en LOS DEMAS cada vez que llega
///
/// En el inspector es exactamente un NetworkTransform: todas las casillas de la
/// sesion estan ahi y funcionan igual.
/// </summary>
public class NetworkTransformMedido : NetworkTransform
{
    [Header("Medicion (solo lectura)")]
    public int enviados;
    public int recibidos;
    public int recibidosFiables;
    public int recibidosNoFiables;
    public int sincronizacionesCompletas;

    private float desde;

    public float Segundos => Mathf.Max(0.001f, Time.time - desde);

    public void Reiniciar()
    {
        enviados = recibidos = recibidosFiables = recibidosNoFiables = sincronizacionesCompletas = 0;
        desde = Time.time;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Reiniciar();
    }

    protected override void OnAuthorityPushTransformState(ref NetworkTransformState estado)
    {
        enviados++;
        base.OnAuthorityPushTransformState(ref estado);
    }

    protected override void OnNetworkTransformStateUpdated(ref NetworkTransformState anterior, ref NetworkTransformState nuevo)
    {
        recibidos++;
        if (nuevo.IsReliableStateUpdate()) recibidosFiables++;
        else recibidosNoFiables++;
        if (nuevo.IsUnreliableFrameSync()) sincronizacionesCompletas++;
        base.OnNetworkTransformStateUpdated(ref anterior, ref nuevo);
    }

    public string Informe()
    {
        return name + ": enviados " + enviados + " (" + (enviados / Segundos).ToString("0.0") + "/s)" +
               "  recibidos " + recibidos + " (" + (recibidos / Segundos).ToString("0.0") + "/s)" +
               "  fiables " + recibidosFiables + "  no fiables " + recibidosNoFiables +
               "  sincronizaciones completas " + sincronizacionesCompletas;
    }
}
