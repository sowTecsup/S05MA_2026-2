using Unity.Netcode;
using UnityEngine;

/// <summary>
/// CASO 04 - Que ahorran la entrega no fiable y la compresion.
///
/// La semana 4 enseno que un EVENTO perdido es un evento que no ocurrio: por eso
/// el golpe va fiable. Una POSICION perdida es otra cosa: el siguiente estado la
/// deja vieja. Reenviarla es pagar por algo que ya no sirve.
///
/// Dos cubos que el servidor hace girar en los TRES ejes, como un cubo lanzado:
///
///   Cubo_Estandar     todo por defecto: fiable, float completo, rotacion por
///                     angulos de Euler
///   Cubo_Comprimido   Use Unreliable Deltas + Use Half Float Precision +
///                     Use Quaternion Synchronization + Use Quaternion Compression
///
/// Lo que hace cada casilla, segun el codigo de NGO:
///   Use Unreliable Deltas            los estados normales van NO fiables. Siguen
///                                    siendo fiables la sincronizacion inicial,
///                                    los Teleport y UNA SINCRONIZACION COMPLETA
///                                    POR SEGUNDO mientras se mueve, para que un
///                                    paquete perdido no deje un eje descuadrado.
///                                    OJO: el comentario dice "Enabled by default"
///                                    pero el campo es "= false". Manda el codigo.
///   Use Half Float Precision         la mitad de bytes, y la posicion en deltas
///   Use Quaternion Synchronization   la rotacion entera en vez de tres angulos
///   Use Quaternion Compression       esa rotacion cabe en un entero sin signo.
///                                    Menos precision que half float.
///
/// La compresion de rotacion SOLO tiene sentido si se gira en varios ejes. La
/// capsula gira en Y: un angulo basta y no se marca. Los cubos del cofre, que se
/// lanzan y dan vueltas, si.
///
/// Como se ve, en dos etapas:
///
///   ETAPA A (red perfecta)
///     1. Pulsa 9 en un CLIENTE: los dos reciben lo mismo; el comprimido, todo
///        como no fiable salvo una sincronizacion completa por segundo.
///
///   ETAPA B (se pierden paquetes)
///     2. Pulsa 0 para poner los contadores a cero, y P hasta "200 ms + 20% perdida".
///     3. Espera diez segundos y pulsa 9. El estandar lo recibe TODO, tarde y
///        reenviado. Al comprimido le faltan estados... y se ve igual de bien,
///        porque cada uno que llega deja viejos a los perdidos.
///
/// Cuantos BYTES ahorra cada casilla lo mide la semana 6, con el Network Profiler.
/// </summary>
public class Caso04_LoQuePesaUnEstado : NetworkBehaviour
{
    public NetworkTransformMedido estandar;
    public NetworkTransformMedido comprimido;
    public float velocidadGiro = 90f;

    void Update()
    {
        if (!IsSpawned) return;

        if (IsServer)
        {
            Vector3 giro = new Vector3(1f, 0.7f, 0.4f) * velocidadGiro * Time.deltaTime;
            estandar.transform.Rotate(giro, Space.Self);
            comprimido.transform.Rotate(giro, Space.Self);
        }

        if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            Debug.Log("[Caso04] INFORME en esta maquina (" + (IsServer ? "servidor" : "cliente") + "):" +
                      "\n   " + estandar.Informe() +
                      "\n   " + comprimido.Informe());
        }

        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            estandar.Reiniciar();
            comprimido.Reiniciar();
            Debug.Log("[Caso04] Contadores a cero en esta maquina.");
        }
    }

    private void OnGUI()
    {
        Etiqueta(estandar, "Estandar");
        Etiqueta(comprimido, "Comprimido");
    }

    private static void Etiqueta(Component c, string texto)
    {
        if (c == null || Camera.main == null) return;
        Vector3 p = Camera.main.WorldToScreenPoint(c.transform.position + Vector3.up * 1.2f);
        if (p.z > 0f) GUI.Label(new Rect(p.x - 40f, Screen.height - p.y, 120f, 22f), texto);
    }
}
