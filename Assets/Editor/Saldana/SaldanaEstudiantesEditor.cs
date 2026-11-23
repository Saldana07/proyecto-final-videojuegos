using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

// Herramientas de editor de Saldaña (estudiantes y fila). No van en el juego.
//  1. Crea los ScriptableObject (Sistemas, Artes, config), los tells y el prefab del estudiante.
//  2. Crea Escena_Saldana (copia de la SampleScene de Carlos, sin sus dianas) con la fila,
//     el spawner y el NavMesh horneado.
//  3. Vuelve a hornear el NavMesh de la escena abierta.
public static class SaldanaEstudiantesEditor
{
    const string CarpetaDatos = "Assets/Datos/Estudiantes";
    const string CarpetaMateriales = CarpetaDatos + "/Materiales";
    const string CarpetaPrefabs = "Assets/Prefabs/Estudiantes";
    const string RutaConfig = CarpetaDatos + "/ConfigEstudiantes.asset";
    const string RutaSistemas = CarpetaDatos + "/Sistemas.asset";
    const string RutaArtes = CarpetaDatos + "/Artes.asset";
    const string RutaPrefab = CarpetaPrefabs + "/Estudiante.prefab";
    const string RutaTellSistemas = CarpetaPrefabs + "/Tell_Sistemas.prefab";
    const string RutaTellArtes = CarpetaPrefabs + "/Tell_Artes.prefab";
    const string RutaSample = "Assets/Scenes/SampleScene.unity";
    const string RutaEscena = "Assets/Scenes/Escena_Saldana.unity";
    const string RutaNavMesh = "Assets/Scenes/Escena_Saldana_NavMesh.asset";
    const string NombreGrupo = "Estudiantes (Saldaña)";

    // Planta baja del Edificio_Univalle (medidas de Carlos): vestíbulo sur techado x 217–246,
    // z 56–61; corredor x 225–243, z 62–90; plazoleta abierta al sur (z < 55).
    static readonly Vector2 InicioFila = new Vector2(219f, 58.5f);
    static readonly Vector2 PuntoBano = new Vector2(234f, 86f);
    static readonly Vector2[] Apariciones =
    {
        new Vector2(229f, 33f),   // sur de la plazoleta
        new Vector2(210f, 40f),   // suroeste
        new Vector2(248f, 40f),   // sureste
    };
    // Volumen del NavMesh: solo la planta baja (y de −0,5 a 2,5) alrededor de la fila y la plazoleta.
    static readonly Vector3 CentroNav = new Vector3(230f, 1f, 60f);
    static readonly Vector3 TamanoNav = new Vector3(76f, 3f, 70f);

    // =============================== 1. Datos y prefab ===============================

    [MenuItem("No te me colés/Saldaña/1 - Crear datos y prefab del estudiante")]
    public static void CrearDatosYPrefab()
    {
        if (LayerMask.NameToLayer(Capas.Estudiante) < 0)
        {
            Debug.LogError("[Saldaña] Falta la capa 'Estudiante' en Project Settings > Tags and Layers.");
            return;
        }
        Carpeta(CarpetaDatos);
        Carpeta(CarpetaMateriales);
        Carpeta(CarpetaPrefabs);

        // Solo se llenan con los valores del GDD al crearse: si ya existen no se tocan,
        // para no borrar lo que se haya ajustado en un playtest.
        var config = CargarOCrear<ConfigEstudiantes>(RutaConfig, _ => { });
        var matCuerpo = Mat("Cuerpo", new Color(0.7f, 0.7f, 0.7f), false);
        var matAnillo = Mat("Anillo", Color.white, true);
        Mat("Hueco", new Color(1f, 0.85f, 0.2f), true);
        var tellSistemas = CrearTellSistemas(Mat("Tell_Sistemas", new Color(0.2f, 0.45f, 1f), false));
        var tellArtes = CrearTellArtes(Mat("Tell_Artes", new Color(1f, 0.55f, 0.15f), false));

        var sistemas = CargarOCrear<FacultadData>(RutaSistemas, ValoresSistemas);
        var artes = CargarOCrear<FacultadData>(RutaArtes, ValoresArtes);
        sistemas.tellPrefab = tellSistemas;
        artes.tellPrefab = tellArtes;
        EditorUtility.SetDirty(sistemas);
        EditorUtility.SetDirty(artes);

        CrearPrefab(config, matCuerpo, matAnillo);
        AssetDatabase.SaveAssets();
        Debug.Log("[Saldaña] Datos (Sistemas, Artes, ConfigEstudiantes) y prefab del estudiante listos.");
    }

    // GDD 7.2
    static void ValoresSistemas(FacultadData f)
    {
        f.nombre = "Sistemas";
        f.tell = "Se detiene a 2 m de la fila mirando el celular y se ajusta los audífonos";
        f.tactica = "Entra de lado sin contacto visual, fingiendo una llamada";
        f.colorAcento = new Color(0.2f, 0.45f, 1f);
        f.counter = TipoHerramienta.Megafono;   // pánico escénico
        f.resistencia = TipoHerramienta.Agua;   // tiene el celular en bolsa
        f.tiempoIntento = 2f;
        f.velocidad = 1.8f;
        f.diaAparicion = 1;
    }

    static void ValoresArtes(FacultadData f)
    {
        f.nombre = "Artes";
        f.tell = "Camina tapado con una maqueta o un pliego enorme";
        f.tactica = "El objeto tapa la vista y bloquea el tiro desde un lado";
        f.colorAcento = new Color(1f, 0.55f, 0.15f);
        f.counter = TipoHerramienta.Agua;         // corre a salvar su trabajo
        f.resistencia = TipoHerramienta.Megafono; // le encanta el público
        f.tiempoIntento = 2.5f;
        f.velocidad = 1.5f;
        f.diaAparicion = 1;
    }

    static GameObject CrearTellSistemas(Material mat)
    {
        var raiz = new GameObject("Tell_Sistemas");
        Parte(PrimitiveType.Cube, "Celular", raiz.transform, new Vector3(0.22f, 0.2f, 0.38f), new Vector3(0.2f, 0.34f, 0.05f), mat);
        Parte(PrimitiveType.Cube, "Audifonos", raiz.transform, new Vector3(0f, 0.52f, 0f), new Vector3(0.8f, 0.12f, 0.16f), mat);
        return GuardarPrefab(raiz, RutaTellSistemas);
    }

    static GameObject CrearTellArtes(Material mat)
    {
        var raiz = new GameObject("Tell_Artes");
        Parte(PrimitiveType.Cube, "Maqueta", raiz.transform, new Vector3(0f, -0.1f, 0.5f), new Vector3(1.2f, 0.85f, 0.06f), mat);
        return GuardarPrefab(raiz, RutaTellArtes);
    }

    static void CrearPrefab(ConfigEstudiantes config, Material matCuerpo, Material matAnillo)
    {
        int capa = LayerMask.NameToLayer(Capas.Estudiante);
        var raiz = new GameObject("Estudiante");

        var capsula = raiz.AddComponent<CapsuleCollider>();
        capsula.height = 1.8f;
        capsula.radius = 0.35f;
        capsula.center = new Vector3(0f, 0.9f, 0f);

        var agente = raiz.AddComponent<NavMeshAgent>();
        agente.radius = 0.35f;
        agente.height = 1.8f;
        agente.speed = 1.8f;
        agente.angularSpeed = 540f;
        agente.acceleration = 12f;
        agente.stoppingDistance = 0.1f;
        agente.enabled = false; // se activa al ponerlo sobre el NavMesh

        var obstaculo = raiz.AddComponent<NavMeshObstacle>();
        obstaculo.shape = NavMeshObstacleShape.Capsule;
        obstaculo.center = new Vector3(0f, 0.9f, 0f);
        obstaculo.radius = 0.35f;
        obstaculo.height = 1.8f;
        obstaculo.carving = false;
        obstaculo.enabled = false; // solo mientras está quieto en la fila

        var est = raiz.AddComponent<Estudiante>();
        est.config = config;
        est.cuerpo = Parte(PrimitiveType.Capsule, "Cuerpo", raiz.transform, new Vector3(0f, 0.9f, 0f), new Vector3(0.7f, 0.9f, 0.7f), matCuerpo).GetComponent<Renderer>();
        Parte(PrimitiveType.Cube, "Frente", raiz.transform, new Vector3(0f, 1.45f, 0.32f), new Vector3(0.22f, 0.1f, 0.12f), matCuerpo);

        var anillo = Parte(PrimitiveType.Cylinder, "Anillo", raiz.transform, new Vector3(0f, 0.03f, 0f), new Vector3(1.2f, 0.01f, 1.2f), matAnillo).GetComponent<Renderer>();
        anillo.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        anillo.enabled = false;
        est.anillo = anillo;

        var punto = new GameObject("PuntoTell").transform;
        punto.SetParent(raiz.transform, false);
        punto.localPosition = new Vector3(0f, 1.3f, 0f);
        est.puntoTell = punto;

        var texto = new GameObject("Etiqueta");
        texto.transform.SetParent(raiz.transform, false);
        texto.transform.localPosition = new Vector3(0f, 2.2f, 0f);
        var tm = texto.AddComponent<TextMesh>();
        tm.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        texto.GetComponent<MeshRenderer>().sharedMaterial = tm.font.material;
        tm.anchor = TextAnchor.LowerCenter;
        tm.alignment = TextAlignment.Center;
        tm.fontSize = 48;
        tm.characterSize = 0.06f;
        tm.color = Color.white;
        est.etiqueta = tm;

        foreach (var t in raiz.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = capa;
        GuardarPrefab(raiz, RutaPrefab);
    }

    // =============================== 2. Escena de prueba ===============================

    [MenuItem("No te me colés/Saldaña/2 - Crear escena de prueba (Escena_Saldana)")]
    public static void CrearEscena()
    {
        CrearDatosYPrefab();
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab);
        if (prefab == null) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(RutaEscena) == null && !AssetDatabase.CopyAsset(RutaSample, RutaEscena))
        {
            Debug.LogError("[Saldaña] No pude copiar " + RutaSample + " a " + RutaEscena);
            return;
        }
        var escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);

        // Las dianas de prueba de Carlos sobran aquí: llegan estudiantes de verdad.
        // El jugador, la cámara, el bebedero y el marcador se quedan.
        foreach (var raizEscena in escena.GetRootGameObjects())
            foreach (var d in raizEscena.GetComponentsInChildren<DianaPrueba>(true))
                if (d != null) Object.DestroyImmediate(d.gameObject);

        var viejo = GameObject.Find(NombreGrupo);
        if (viejo != null) Object.DestroyImmediate(viejo);
        var grupo = new GameObject(NombreGrupo).transform;

        var config = AssetDatabase.LoadAssetAtPath<ConfigEstudiantes>(RutaConfig);
        var estudiante = prefab.GetComponent<Estudiante>();

        var fila = new GameObject("Fila").AddComponent<Fila>();
        fila.transform.SetParent(grupo, false);
        fila.transform.position = Piso(InicioFila);
        fila.prefabEstudiante = estudiante;
        fila.config = config;
        fila.materialHueco = AssetDatabase.LoadAssetAtPath<Material>(CarpetaMateriales + "/Hueco.mat");
        var bano = new GameObject("Punto baño").transform;
        bano.SetParent(fila.transform, false);
        bano.position = Piso(PuntoBano);
        fila.puntoBano = bano;

        var spawner = new GameObject("Spawner").AddComponent<SpawnerEstudiantes>();
        spawner.transform.SetParent(grupo, false);
        spawner.prefab = estudiante;
        spawner.config = config;
        spawner.facultades = new[]
        {
            AssetDatabase.LoadAssetAtPath<FacultadData>(RutaSistemas),
            AssetDatabase.LoadAssetAtPath<FacultadData>(RutaArtes),
        };
        var puntos = new Transform[Apariciones.Length];
        for (int i = 0; i < Apariciones.Length; i++)
        {
            var t = new GameObject($"Aparición {i + 1}").transform;
            t.SetParent(spawner.transform, false);
            t.position = Piso(Apariciones[i]);
            puntos[i] = t;
        }
        spawner.puntosAparicion = puntos;

        var nav = new GameObject("NavMesh").AddComponent<NavMeshSurface>();
        nav.transform.SetParent(grupo, false);
        ConfigurarYHornear(nav);

        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);
        Debug.Log("[Saldaña] Escena de prueba lista en " + RutaEscena + ". Dale Play.");
    }

    // =============================== 3. NavMesh ===============================

    [MenuItem("No te me colés/Saldaña/3 - Volver a hornear el NavMesh")]
    public static void Rehornear()
    {
        var nav = Object.FindAnyObjectByType<NavMeshSurface>();
        if (nav == null)
        {
            Debug.LogError("[Saldaña] No hay NavMesh en la escena abierta. Usa primero el paso 2.");
            return;
        }
        ConfigurarYHornear(nav);
        EditorSceneManager.MarkSceneDirty(nav.gameObject.scene);
        EditorSceneManager.SaveScene(nav.gameObject.scene);
    }

    static void ConfigurarYHornear(NavMeshSurface nav)
    {
        nav.collectObjects = CollectObjects.Volume;
        nav.center = CentroNav - nav.transform.position;
        nav.size = TamanoNav;
        nav.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        nav.layerMask = ~(Capas.MascaraEstudiante | Capas.MascaraJugador);
        Physics.SyncTransforms();
        nav.BuildNavMesh();
        if (nav.navMeshData == null)
        {
            Debug.LogError("[Saldaña] No se pudo hornear el NavMesh.");
            return;
        }
        AssetDatabase.DeleteAsset(RutaNavMesh);
        AssetDatabase.CreateAsset(nav.navMeshData, RutaNavMesh);
        EditorUtility.SetDirty(nav);
        Debug.Log("[Saldaña] NavMesh horneado y guardado en " + RutaNavMesh);
    }

    // =============================== Utilidades ===============================

    static Vector3 Piso(Vector2 xz)
    {
        Physics.SyncTransforms();
        int mascara = ~(Capas.MascaraEstudiante | Capas.MascaraJugador);
        if (Physics.Raycast(new Vector3(xz.x, 2.6f, xz.y), Vector3.down, out RaycastHit hit, 6f, mascara, QueryTriggerInteraction.Ignore))
            return hit.point;
        Debug.LogWarning($"[Saldaña] No encontré piso en {xz}; uso y = 0.");
        return new Vector3(xz.x, 0f, xz.y);
    }

    static GameObject Parte(PrimitiveType tipo, string nombre, Transform padre, Vector3 pos, Vector3 escala, Material mat)
    {
        var go = GameObject.CreatePrimitive(tipo);
        go.name = nombre;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(padre, false);
        go.transform.localPosition = pos;
        go.transform.localScale = escala;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }

    static GameObject GuardarPrefab(GameObject raiz, string ruta)
    {
        var prefab = PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
        Object.DestroyImmediate(raiz);
        return prefab;
    }

    static T CargarOCrear<T>(string ruta, System.Action<T> valores) where T : ScriptableObject
    {
        var a = AssetDatabase.LoadAssetAtPath<T>(ruta);
        if (a != null) return a;
        a = ScriptableObject.CreateInstance<T>();
        valores(a);
        AssetDatabase.CreateAsset(a, ruta);
        return a;
    }

    static Material Mat(string nombre, Color color, bool unlit)
    {
        string ruta = CarpetaMateriales + "/" + nombre + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (m != null) return m;
        var shader = Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find(unlit ? "Unlit/Color" : "Standard");
        m = new Material(shader);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
        if (m.HasProperty("_Color")) m.SetColor("_Color", color);
        AssetDatabase.CreateAsset(m, ruta);
        return m;
    }

    static void Carpeta(string ruta)
    {
        if (AssetDatabase.IsValidFolder(ruta)) return;
        int corte = ruta.LastIndexOf('/');
        string padre = ruta.Substring(0, corte);
        Carpeta(padre);
        AssetDatabase.CreateFolder(padre, ruta.Substring(corte + 1));
    }
}
