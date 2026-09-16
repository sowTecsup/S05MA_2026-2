using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Netcode.Transports.UTP;
using Unity.Multiplayer.Tools.NetworkSimulator.Runtime;

/// <summary>
/// Arma la escena de la semana 5 con un clic.
///
/// Menu:  Tools > S05MA > Preparar escena Semana 5
///
/// Se hace por script, como en las semanas anteriores, para no depender de GUIDs
/// ni de la version exacta del editor.
///
/// Esta semana el script tiene un valor extra: TODAS LAS CASILLAS DEL INSPECTOR
/// DE LA SESION ESTAN ESCRITAS AQUI, con su motivo. Si quieres saber por que el
/// cubo lleva Switch Transform Space When Parented y la capsula no, esta abajo.
/// Y lo que dice el script es lo que veras en el inspector del prefab.
/// </summary>
public static class SetupSemana05
{
    private const string RutaEscena = "Assets/Scenes/Semana05.unity";
    private const string RutaAvatar = "Assets/Prefabs/PlayerAvatar.prefab";
    private const string RutaCubo = "Assets/Prefabs/CuboDeColor.prefab";
    private const string RutaListaPrefabs = "Assets/DefaultNetworkPrefabs.asset";

    [MenuItem("Tools/S05MA/Preparar escena Semana 5")]
    public static void PrepararEscena()
    {
        var escena = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        var suelo = GameObject.CreatePrimitive(PrimitiveType.Plane);
        suelo.name = "Suelo";
        suelo.transform.localScale = new Vector3(5f, 1f, 5f);

        System.IO.Directory.CreateDirectory("Assets/Prefabs");
        var avatar = CrearPrefabAvatar();
        var cubo = CrearPrefabCubo();

        // --- NetworkManager + red simulada -------------------------------------
        var nmGo = new GameObject("NetworkManager");
        var nm = nmGo.AddComponent<NetworkManager>();
        var utp = nmGo.AddComponent<UnityTransport>();
        nm.NetworkConfig = new NetworkConfig { NetworkTransport = utp };
        nm.NetworkConfig.PlayerPrefab = avatar;
        nm.NetworkConfig.Prefabs.NetworkPrefabsLists.Add(ListaDePrefabs(avatar, cubo));
        nmGo.AddComponent<ConnectionManager>();
        nmGo.AddComponent<NetworkSimulator>();
        nmGo.AddComponent<RedSimulada>();

        // --- El bosque de la semana 3, que sigue vivo ---------------------------
        int n = 0;
        for (int fila = 0; fila < 2; fila++)
            for (int col = 0; col < 3; col++)
            {
                var arbol = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                arbol.name = "Arbol_" + (++n);
                arbol.transform.position = new Vector3(-5f + col * 5f, 1f, 3f + fila * 5f);
                arbol.AddComponent<NetworkObject>();
                arbol.AddComponent<TreeNode>();
            }

        // --- El cofre ---------------------------------------------------------
        var cofre = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cofre.name = "Cofre";
        cofre.transform.position = new Vector3(0f, 0.4f, -1f);
        cofre.transform.localScale = new Vector3(1.2f, 0.8f, 0.8f);
        cofre.AddComponent<NetworkObject>();
        cofre.AddComponent<ChestNode>().cuboPrefab = cubo.GetComponent<NetworkObject>();

        // --- Los casos ---------------------------------------------------------
        var casos = new GameObject("Casos");
        casos.AddComponent<NetworkObject>();

        // Caso 01: mismo baile, distintos ejes.
        var c1 = casos.AddComponent<Caso01_LoQueViaja>();
        c1.todosLosEjes = Maniqui<NetworkTransformMedido>("Maniqui_TodosLosEjes", PrimitiveType.Capsule, new Vector3(-12f, 1f, -6f));
        c1.recortado = Maniqui<NetworkTransformMedido>("Maniqui_Recortado", PrimitiveType.Capsule, new Vector3(-6f, 1f, -6f));
        RecortarComoCapsula(c1.recortado);

        // Caso 02: la unica diferencia es Authority Mode.
        var c2 = casos.AddComponent<Caso02_QuienEscribe>();
        c2.maniquiServer = Maniqui<NetworkTransform>("Maniqui_Server", PrimitiveType.Capsule, new Vector3(6f, 1f, -6f));
        c2.maniquiOwner = Maniqui<NetworkTransform>("Maniqui_Owner", PrimitiveType.Capsule, new Vector3(10f, 1f, -6f));
        c2.maniquiServer.AuthorityMode = NetworkTransform.AuthorityModes.Server;   // el de por defecto
        c2.maniquiOwner.AuthorityMode = NetworkTransform.AuthorityModes.Owner;

        // Caso 03: una fila por tipo de interpolacion.
        var c3 = casos.AddComponent<Caso03_TiposDeInterpolacion>();
        for (int i = 0; i < 4; i++)
            c3.filas[i] = Maniqui<NetworkTransform>("Fila_" + (i + 1), PrimitiveType.Cube, new Vector3(0f, 0.5f, 12f + i * 2.2f));
        c3.filas[0].Interpolate = false;
        TipoDeInterpolacion(c3.filas[1], NetworkTransform.InterpolationTypes.LegacyLerp);
        TipoDeInterpolacion(c3.filas[2], NetworkTransform.InterpolationTypes.Lerp);
        TipoDeInterpolacion(c3.filas[3], NetworkTransform.InterpolationTypes.SmoothDampening);

        // Caso 04: el mismo giro, estandar frente a comprimido.
        var c4 = casos.AddComponent<Caso04_LoQuePesaUnEstado>();
        c4.estandar = Maniqui<NetworkTransformMedido>("Cubo_Estandar", PrimitiveType.Cube, new Vector3(-12f, 1.5f, 6f));
        c4.comprimido = Maniqui<NetworkTransformMedido>("Cubo_Comprimido", PrimitiveType.Cube, new Vector3(-9f, 1.5f, 6f));
        c4.comprimido.UseUnreliableDeltas = true;
        c4.comprimido.UseHalfFloatPrecision = true;
        c4.comprimido.UseQuaternionSynchronization = true;
        c4.comprimido.UseQuaternionCompression = true;

        // Caso 05: el hijo en espacio de mundo frente al hijo en espacio local.
        var c5 = casos.AddComponent<Caso05_ElHijoQueSeQuedaAtras>();
        var mundo = Maniqui<NetworkTransform>("Cubo_EspacioMundo", PrimitiveType.Cube, new Vector3(12f, 0.25f, 3f));
        var local = Maniqui<NetworkTransform>("Cubo_EspacioLocal", PrimitiveType.Cube, new Vector3(14f, 0.25f, 3f));
        mundo.transform.localScale = local.transform.localScale = Vector3.one * 0.5f;
        mundo.SwitchTransformSpaceWhenParented = false;
        local.SwitchTransformSpaceWhenParented = true;
        c5.cuboMundo = mundo.GetComponent<NetworkObject>();
        c5.cuboLocal = local.GetComponent<NetworkObject>();

        // Caso 06: vuelo o salto.
        var c6 = casos.AddComponent<Caso06_SaltoOVuelo>();
        c6.cubo = Maniqui<NetworkTransform>("Cubo_Salto", PrimitiveType.Cube, c6.esquinaA);

        // --- Camara: el mapa entero ---------------------------------------------
        var cam = Camera.main;
        if (cam != null)
        {
            cam.transform.position = new Vector3(0f, 26f, -24f);
            cam.transform.rotation = Quaternion.Euler(50f, 0f, 0f);
        }

        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(escena, RutaEscena);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[S05MA] Escena lista en " + RutaEscena +
                  ". Window > Multiplayer > Play Mode, 3 jugadores virtuales y Play.");
        if (!Application.isBatchMode)
            EditorUtility.DisplayDialog("S05MA",
                "Escena de la Semana 5 preparada.\n\n" +
                "Window > Multiplayer > Play Mode\n-> Virtual Players: 3\n-> Play\n\n" +
                "Host en la principal, Client en las otras.\n\n" +
                "Y esta semana, lo importante: pulsa P en una ventana de CLIENTE para\n" +
                "meterle 200 ms. En localhost todo se ve perfecto.", "Vamos");
    }

    // =========================================================================
    // EL AVATAR
    // =========================================================================
    private static GameObject CrearPrefabAvatar()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = "PlayerAvatar";
        go.AddComponent<NetworkObject>();

        var nt = go.AddComponent<NetworkTransform>();
        // Owner: el WASD responde al instante. Con Server el dueno no se mueve (caso 02).
        nt.AuthorityMode = NetworkTransform.AuthorityModes.Owner;
        RecortarComoCapsula(nt);
        // Flujo continuo: si se pierde un estado, el siguiente lo reemplaza (caso 04).
        nt.UseUnreliableDeltas = true;
        // Mapa pequeno: media precision sobra.
        nt.UseHalfFloatPrecision = true;
        // Sin Quaternion Synchronization: solo gira en Y, un angulo basta.

        go.AddComponent<PlayerAvatar>();
        go.AddComponent<PlayerHealth>();
        go.AddComponent<PlayerCombat>();
        go.AddComponent<PlayerCarry>();

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, RutaAvatar);
        Object.DestroyImmediate(go);
        return prefab;
    }

    // =========================================================================
    // EL CUBO DEL COFRE
    // =========================================================================
    private static GameObject CrearPrefabCubo()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "CuboDeColor";
        go.transform.localScale = Vector3.one * 0.5f;
        go.AddComponent<Rigidbody>();
        go.AddComponent<NetworkObject>();

        var nt = go.AddComponent<NetworkTransform>();
        // Server: nadie mas decide donde esta un objeto que dos pueden querer.
        nt.AuthorityMode = NetworkTransform.AuthorityModes.Server;
        // Mientras lo llevas, su posicion es RESPECTO A TI. Sin esto te persigue (caso 05).
        nt.SwitchTransformSpaceWhenParented = true;
        // Nunca cambia de tamano.
        nt.SyncScaleX = nt.SyncScaleY = nt.SyncScaleZ = false;
        // Lanzado da vueltas en los tres ejes: aqui SI compensa comprimir la rotacion (caso 04).
        nt.UseUnreliableDeltas = true;
        nt.UseHalfFloatPrecision = true;
        nt.UseQuaternionSynchronization = true;
        nt.UseQuaternionCompression = true;

        var cuerpo = go.AddComponent<NetworkRigidbody>();
        // APAGADO: con la fisica moviendo el transform, Switch Transform Space When
        // Parented no funciona. Lo dice el codigo de NGO.
        cuerpo.UseRigidBodyForMotion = false;
        // Encendido (por defecto): cinematico en quien no tiene autoridad. La fisica
        // corre solo en el servidor; los clientes ven el vuelo interpolado.
        cuerpo.AutoUpdateKinematicState = true;

        go.AddComponent<CuboDeColor>();

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, RutaCubo);
        Object.DestroyImmediate(go);
        return prefab;
    }

    // =========================================================================
    // Utilidades
    // =========================================================================

    /// <summary>X y Z de posicion, solo Y de rotacion, nada de escala.</summary>
    private static void RecortarComoCapsula(NetworkTransform nt)
    {
        nt.SyncPositionY = false;
        nt.SyncRotAngleX = false;
        nt.SyncRotAngleZ = false;
        nt.SyncScaleX = nt.SyncScaleY = nt.SyncScaleZ = false;
    }

    private static void TipoDeInterpolacion(NetworkTransform nt, NetworkTransform.InterpolationTypes tipo)
    {
        nt.PositionInterpolationType = tipo;
        nt.RotationInterpolationType = tipo;
        nt.ScaleInterpolationType = tipo;
    }

    /// <summary>Un objeto de red de la escena, movido por el servidor, sin colision.</summary>
    private static T Maniqui<T>(string nombre, PrimitiveType forma, Vector3 posicion) where T : NetworkTransform
    {
        var go = GameObject.CreatePrimitive(forma);
        go.name = nombre;
        go.transform.position = posicion;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.AddComponent<NetworkObject>();
        return go.AddComponent<T>();
    }

    private static NetworkPrefabsList ListaDePrefabs(params GameObject[] prefabs)
    {
        var lista = AssetDatabase.LoadAssetAtPath<NetworkPrefabsList>(RutaListaPrefabs);
        if (lista == null)
        {
            lista = ScriptableObject.CreateInstance<NetworkPrefabsList>();
            AssetDatabase.CreateAsset(lista, RutaListaPrefabs);
        }

        foreach (var p in prefabs)
        {
            bool esta = false;
            foreach (var registrado in lista.PrefabList)
                if (registrado.Prefab == p) { esta = true; break; }
            if (!esta) lista.Add(new NetworkPrefab { Prefab = p });
        }

        EditorUtility.SetDirty(lista);
        return lista;
    }
}
