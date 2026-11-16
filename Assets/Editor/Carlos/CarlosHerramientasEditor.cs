using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Herramientas de editor de Carlos (jugador y herramientas). No van en el juego.
//  - Crea los ScriptableObject de balance (jugador, agua, megáfono).
//  - Integra en la SampleScene el jugador con cámara isométrica, las herramientas y
//    objetivos de prueba ubicados en la planta baja del edificio (ver AnalisisEdificio).
//  - Autoprueba de la detección, recarga, munición y bebedero.
// También se puede correr sin abrir Unity:
//   Unity.exe -batchmode -projectPath <proyecto> -executeMethod CarlosHerramientasEditor.CrearTodoDesdeConsola -logFile -
public static class CarlosHerramientasEditor
{
    const string CarpetaDatos = "Assets/Datos";
    const string RutaJugador = CarpetaDatos + "/Jugador/JugadorConfig.asset";
    const string RutaAgua = CarpetaDatos + "/Herramientas/Agua.asset";
    const string RutaMegafono = CarpetaDatos + "/Herramientas/Megafono.asset";
    const string CarpetaMateriales = CarpetaDatos + "/Pruebas/Materiales";
    const string RutaSample = "Assets/Scenes/SampleScene.unity";
    const string NombreGrupo = "Objetivos de prueba (Carlos)";

    // Planta baja del Edificio_Univalle (sale de AnalisisEdificio: piso en y≈0, segundo piso
    // en y≈3,5, techo en y≈7). El vestíbulo sur techado va de x 217 a 246 y z 56 a 61; el
    // corredor de x 225 a 243 y z 62 a 90; la plazoleta abierta está al sur (z < 55).
    static readonly Vector2 PosJugador = new Vector2(229f, 46f);
    const float FilaZ = 58.5f, FilaX0 = 219f, FilaPaso = 1.3f;
    const int FilaPuestos = 14, FilaHueco = 6;   // un hueco libre en la fila (GDD 4.1)
    static readonly Vector2 PosBebedero = new Vector2(213f, 57f);
    static readonly (string nombre, Vector2 pos, bool sistemas)[] Colados =
    {
        ("Sistemas A", new Vector2(225f, 51f), true),    // plazoleta, llegando a la fila
        ("Sistemas B", new Vector2(233f, 68f), true),    // corredor interior
        ("Sistemas C", new Vector2(243f, 53f), true),    // plazoleta, lado oriental
        ("Artes A",    new Vector2(221f, 66f), false),   // pasillo occidental
        ("Artes B",    new Vector2(238f, 75f), false),   // fondo del corredor
    };

    // ------------------------------------------------------------------ datos

    [MenuItem("No te me colés/Carlos/1 - Crear datos de herramientas")]
    public static void CrearDatos()
    {
        // Solo se llenan con los valores del GDD al crearse; si ya existen no se tocan
        // para no borrar lo que se haya ajustado en un playtest.
        CargarOCrear<JugadorConfig>(RutaJugador, _ => { });
        var efecto = Mat(CarpetaMateriales + "/EfectoHerramienta.mat", Color.white, unlit: true);
        CargarOCrear<HerramientaData>(RutaAgua, a => { ValoresAgua(a); a.materialEfecto = efecto; });
        CargarOCrear<HerramientaData>(RutaMegafono, m => { ValoresMegafono(m); m.materialEfecto = efecto; });
        AssetDatabase.SaveAssets();
        Debug.Log("[Carlos] Datos de herramientas listos en " + CarpetaDatos);
    }

    // Valores iniciales de la tabla de herramientas (GDD 8).
    public static void ValoresAgua(HerramientaData a)
    {
        a.tipo = TipoHerramienta.Agua;
        a.nombre = "Botella de agua";
        a.color = new Color(0.3f, 0.7f, 1f);
        a.alcance = 3f;
        a.radioRayo = 0.35f;
        a.recarga = 0.4f;
        a.municionMax = 6;
        a.segundosPorRelleno = 0.4f;
        a.verguenza = 0f;
        a.frenarSegundos = 1f;
    }

    public static void ValoresMegafono(HerramientaData m)
    {
        m.tipo = TipoHerramienta.Megafono;
        m.nombre = "Megáfono";
        m.color = new Color(1f, 0.85f, 0.2f);
        m.alcance = 5f;
        m.aperturaCono = 45f;
        m.recarga = 10f;
        m.municionMax = 0;
        m.verguenza = 60f;
        m.frenarSegundos = 0f;
    }

    // ------------------------------------------------------------------ SampleScene

    [MenuItem("No te me colés/Carlos/2 - Integrar en SampleScene (isométrica)")]
    public static void IntegrarEnSampleScene()
    {
        CrearDatos();
        var agua = AssetDatabase.LoadAssetAtPath<HerramientaData>(RutaAgua);
        var mega = AssetDatabase.LoadAssetAtPath<HerramientaData>(RutaMegafono);

        int capaEst = LayerMask.NameToLayer(Capas.Estudiante);
        int capaJug = LayerMask.NameToLayer(Capas.Jugador);
        if (capaEst < 0 || capaJug < 0)
        {
            Debug.LogError("[Carlos] Faltan las capas 'Estudiante' y 'Jugador' en Tags and Layers.");
            return;
        }

        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var escena = EditorSceneManager.OpenScene(RutaSample, OpenSceneMode.Single);

        var config = AssetDatabase.LoadAssetAtPath<JugadorConfig>(RutaJugador);
        var cc = Object.FindAnyObjectByType<CharacterController>();
        if (cc == null)
        {
            Debug.LogError("[Carlos] No hay jugador (CharacterController) en la SampleScene.");
            return;
        }
        var jugador = cc.gameObject;

        // Se puede correr varias veces: borra lo que se haya puesto antes.
        foreach (var nombre in new[] { "Herramientas", "Cuerpo", "Frente" })
        {
            var viejo = jugador.transform.Find(nombre);
            if (viejo != null) Object.DestroyImmediate(viejo.gameObject);
        }
        var grupoViejo = GameObject.Find(NombreGrupo);
        if (grupoViejo != null) Object.DestroyImmediate(grupoViejo);

        // Fuera la primera persona: el movimiento ahora lo hace JugadorIsometrico.
        foreach (var c in jugador.GetComponents<JugadorPrimeraPersona>()) Object.DestroyImmediate(c);
        foreach (var c in jugador.GetComponents<ApuntadorPrimeraPersona>()) Object.DestroyImmediate(c);

        // Cámara: se saca del jugador y pasa a ser isométrica.
        var cam = jugador.GetComponentInChildren<Camera>(true);
        if (cam == null) cam = Camera.main;
        cam.transform.SetParent(null, true);
        cam.name = "Camara isometrica";
        cam.gameObject.tag = "MainCamera";
        cam.orthographic = true;
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 200f;
        var iso = Asegurar<CamaraIsometrica>(cam.gameObject);
        iso.objetivo = jugador.transform;
        var edificio = GameObject.Find("Edificio_Univalle");
        if (edificio != null) iso.edificio = edificio.transform;

        // Jugador
        jugador.layer = capaJug;
        cc.height = 1.8f;
        cc.radius = 0.35f;
        cc.center = new Vector3(0f, 0.9f, 0f);
        Asegurar<EntradaJugador>(jugador);
        var mov = Asegurar<JugadorIsometrico>(jugador);
        mov.config = config;
        mov.camara = cam;
        jugador.transform.rotation = Quaternion.identity;

        var cuerpo = Visual(PrimitiveType.Capsule, "Cuerpo", jugador.transform, new Vector3(0f, 0.9f, 0f), new Vector3(0.7f, 0.9f, 0.7f), capaJug);
        Pintar(cuerpo, Mat(CarpetaMateriales + "/Jugador.mat", new Color(0.2f, 0.75f, 0.35f)));
        var frente = Visual(PrimitiveType.Cube, "Frente", jugador.transform, new Vector3(0f, 1.3f, 0.4f), new Vector3(0.25f, 0.15f, 0.3f), capaJug);
        Pintar(frente, Mat(CarpetaMateriales + "/Frente.mat", Color.black));

        var contenedor = new GameObject("Herramientas").transform;
        contenedor.SetParent(jugador.transform, false);
        var hAgua = new GameObject("Botella de agua").AddComponent<BotellaAgua>();
        hAgua.transform.SetParent(contenedor, false);
        hAgua.datos = agua;
        var hMega = new GameObject("Megáfono").AddComponent<Megafono>();
        hMega.transform.SetParent(contenedor, false);
        hMega.datos = mega;
        Asegurar<CinturonHerramientas>(jugador).Equipar(new Herramienta[] { hAgua, hMega });
        Asegurar<HUDHerramientasPrueba>(jugador);

        var grupo = new GameObject(NombreGrupo).transform;
        var marcador = Visual(PrimitiveType.Cylinder, "Marcador apuntado", grupo, Vector3.zero, new Vector3(0.5f, 0.01f, 0.5f), capaJug);
        Pintar(marcador, Mat(CarpetaMateriales + "/Marcador.mat", new Color(1f, 0.3f, 0.3f), unlit: true));
        mov.marcadorApuntado = marcador.transform;

        // Jugador en la plazoleta, frente al vestíbulo donde está la fila
        Ubicar(jugador.transform, PosJugador, 0.02f);

        var matInocente = Mat(CarpetaMateriales + "/Inocente.mat", new Color(0.6f, 0.6f, 0.6f));
        var matSistemas = Mat(CarpetaMateriales + "/Sistemas.mat", new Color(0.25f, 0.45f, 1f));
        var matArtes = Mat(CarpetaMateriales + "/Artes.mat", new Color(1f, 0.55f, 0.15f));

        // Fila de inocentes asegurados en el vestíbulo (atacarlos es falso positivo)
        for (int i = 0; i < FilaPuestos; i++)
        {
            if (i == FilaHueco) continue;
            var d = Diana($"Fila {i + 1}", Vector3.zero, capaEst, matInocente, "Fila", true, TipoHerramienta.Conos);
            d.transform.SetParent(grupo, true);
            Ubicar(d.transform, new Vector2(FilaX0 + i * FilaPaso, FilaZ), 1f);
        }

        // Colados de prueba alrededor de la fila y dentro del edificio
        foreach (var (nombre, pos, sistemas) in Colados)
        {
            var d = sistemas
                ? Diana(nombre, Vector3.zero, capaEst, matSistemas, "Sistemas", false, TipoHerramienta.Megafono)
                : Diana(nombre, Vector3.zero, capaEst, matArtes, "Artes", false, TipoHerramienta.Agua);
            d.transform.SetParent(grupo, true);
            Ubicar(d.transform, pos, 1f);
        }

        // Bebedero en la entrada occidental del vestíbulo
        var bebedero = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        bebedero.name = "Bebedero";
        bebedero.transform.SetParent(grupo, true);
        bebedero.transform.localScale = new Vector3(1.2f, 0.5f, 1.2f);
        Object.DestroyImmediate(bebedero.GetComponent<Collider>());
        var zona = bebedero.AddComponent<BoxCollider>();
        zona.isTrigger = true;
        zona.size = new Vector3(2f, 4f, 2f);
        bebedero.AddComponent<Rigidbody>().isKinematic = true;
        bebedero.AddComponent<Bebedero>();
        Pintar(bebedero, Mat(CarpetaMateriales + "/Bebedero.mat", new Color(0.2f, 0.45f, 1f)));
        Etiqueta(bebedero.transform, "Bebedero", 3f);
        Ubicar(bebedero.transform, PosBebedero, 0.5f);

        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);
        AssetDatabase.SaveAssets();
        Debug.Log("[Carlos] Jugador isométrico, herramientas y objetivos integrados en " + RutaSample);
    }

    static T Asegurar<T>(GameObject go) where T : Component
    {
        var c = go.GetComponent<T>();
        return c != null ? c : go.AddComponent<T>();
    }

    // Pone el objeto en la planta baja en (x, z). El rayo arranca debajo del segundo piso
    // (y≈3,5) para no caer sobre el techo; si el punto está ocupado (columna, pared)
    // busca el punto libre más cercano en espiral.
    static void Ubicar(Transform t, Vector2 xz, float alturaCentro)
    {
        Physics.SyncTransforms();
        int mascara = ~(Capas.MascaraEstudiante | Capas.MascaraJugador);
        for (float radio = 0f; radio <= 4f; radio += 0.5f)
        {
            int pasos = radio == 0f ? 1 : Mathf.CeilToInt(radio * 8f);
            for (int k = 0; k < pasos; k++)
            {
                float ang = k * Mathf.PI * 2f / pasos;
                Vector3 desde = new Vector3(xz.x + Mathf.Cos(ang) * radio, 2.6f, xz.y + Mathf.Sin(ang) * radio);
                if (!Physics.Raycast(desde, Vector3.down, out RaycastHit hit, 5f, mascara, QueryTriggerInteraction.Ignore)) continue;
                if (hit.normal.y < 0.9f) continue;
                Vector3 p = hit.point;
                if (Physics.CheckCapsule(p + Vector3.up * 0.45f, p + Vector3.up * 1.5f, 0.4f, mascara, QueryTriggerInteraction.Ignore)) continue;
                t.position = p + Vector3.up * alturaCentro;
                return;
            }
        }
        Debug.LogWarning($"[Carlos] No encontré piso libre cerca de {xz} para {t.name}.");
        t.position = new Vector3(xz.x, alturaCentro, xz.y);
    }

    static GameObject Visual(PrimitiveType tipo, string nombre, Transform padre, Vector3 pos, Vector3 escala, int capa)
    {
        var go = GameObject.CreatePrimitive(tipo);
        go.name = nombre;
        go.layer = capa;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        if (padre != null) go.transform.SetParent(padre, false);
        go.transform.localPosition = pos;
        go.transform.localScale = escala;
        return go;
    }

    // ------------------------------------------------------------------ autoprueba

    [MenuItem("No te me colés/Carlos/3 - Autoprueba de herramientas")]
    public static bool Autoprueba()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return false;
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        int capaEst = LayerMask.NameToLayer(Capas.Estudiante);
        var fallos = new List<string>();
        void Esperar(bool cond, string msg) { if (!cond) fallos.Add(msg); }

        // Copias con los valores del GDD: la prueba no depende de lo que se haya ajustado en los assets.
        var agua = ScriptableObject.CreateInstance<HerramientaData>();
        ValoresAgua(agua);
        agua.recarga = 0f;   // para disparar seguido en la prueba
        var mega = ScriptableObject.CreateInstance<HerramientaData>();
        ValoresMegafono(mega);

        var jugador = new GameObject("Jugador");
        jugador.layer = LayerMask.NameToLayer(Capas.Jugador);
        jugador.AddComponent<CharacterController>();
        var hAgua = new GameObject("Agua").AddComponent<BotellaAgua>();
        hAgua.transform.SetParent(jugador.transform);
        hAgua.datos = agua;
        hAgua.Reiniciar();
        var hMega = new GameObject("Mega").AddComponent<Megafono>();
        hMega.transform.SetParent(jugador.transform);
        hMega.datos = mega;
        hMega.Reiniciar();

        Vector3 o = new Vector3(0f, 1f, 0f);
        Vector3 f = Vector3.forward;

        // --- Megáfono: cono de 5 m y 45°
        var delante = Diana("delante", new Vector3(0f, 0f, 3f), capaEst, null, "x", false, TipoHerramienta.Conos);
        var borde = Diana("borde 20°", Quaternion.Euler(0, 20, 0) * new Vector3(0f, 0f, 4f), capaEst, null, "x", false, TipoHerramienta.Conos);
        var fuera = Diana("fuera 40°", Quaternion.Euler(0, 40, 0) * new Vector3(0f, 0f, 3f), capaEst, null, "x", false, TipoHerramienta.Conos);
        var lejos = Diana("lejos", new Vector3(0f, 0f, 7f), capaEst, null, "x", false, TipoHerramienta.Conos);
        var atras = Diana("atrás", new Vector3(0f, 0f, -3f), capaEst, null, "x", false, TipoHerramienta.Conos);
        Physics.SyncTransforms();

        Esperar(hMega.IntentarUsar(o, f, jugador), "El megáfono no se pudo usar estando listo");
        Esperar(delante.Impactos == 1, "Megáfono: no alcanzó al de adelante");
        Esperar(borde.Impactos == 1, "Megáfono: no alcanzó al que está a 20° (dentro de 22,5°)");
        Esperar(fuera.Impactos == 0, "Megáfono: alcanzó al que está a 40° (fuera del cono)");
        Esperar(lejos.Impactos == 0, "Megáfono: alcanzó al que está a 7 m (alcance 5 m)");
        Esperar(atras.Impactos == 0, "Megáfono: alcanzó al de atrás");
        Esperar(Mathf.Approximately(delante.Verguenza, 60f), $"Megáfono: Vergüenza {delante.Verguenza}, se esperaba 60");
        Esperar(!hMega.IntentarUsar(o, f, jugador), "Megáfono: se usó dos veces sin esperar la recarga de 10 s");
        Esperar(hMega.EnRecarga && hMega.ProgresoRecarga < 0.01f, "Megáfono: no reporta la recarga");
        foreach (var d in new[] { delante, borde, fuera, lejos, atras }) Object.DestroyImmediate(d.gameObject);

        // --- Agua: chorro de 3 m, el primero recibe, el muro lo corta
        var primero = Diana("primero", new Vector3(0f, 0f, 1.5f), capaEst, null, "x", false, TipoHerramienta.Conos);
        var segundo = Diana("segundo", new Vector3(0f, 0f, 2.5f), capaEst, null, "x", false, TipoHerramienta.Conos);
        var lejosAgua = Diana("a 5 m", new Vector3(3f, 0f, 0f) + new Vector3(2f, 0f, 0f), capaEst, null, "x", false, TipoHerramienta.Conos);
        var tras = Diana("tras muro", new Vector3(-0f, 0f, -2.5f), capaEst, null, "x", false, TipoHerramienta.Conos);
        var muro = GameObject.CreatePrimitive(PrimitiveType.Cube);
        muro.transform.position = new Vector3(0f, 1f, -1.2f);
        muro.transform.localScale = new Vector3(2f, 2f, 0.2f);
        Physics.SyncTransforms();

        Esperar(hAgua.IntentarUsar(o, f, jugador), "Agua: no se pudo usar");
        Esperar(primero.Impactos == 1 && segundo.Impactos == 0, "Agua: debía mojar solo al primero en la línea");
        hAgua.IntentarUsar(o, Vector3.right, jugador);
        Esperar(lejosAgua.Impactos == 0, "Agua: alcanzó a alguien a 5 m (alcance 3 m)");
        hAgua.IntentarUsar(o, Vector3.back, jugador);
        Esperar(tras.Impactos == 0, "Agua: el chorro atravesó el muro");

        // --- Munición: 6 cargas, ya se gastaron 3
        Esperar(hAgua.Municion == 3, $"Agua: munición {hAgua.Municion}, se esperaba 3");
        hAgua.IntentarUsar(o, Vector3.left, jugador);
        hAgua.IntentarUsar(o, Vector3.left, jugador);
        hAgua.IntentarUsar(o, Vector3.left, jugador);
        Esperar(hAgua.Municion == 0, "Agua: no llegó a 0 cargas");
        Esperar(!hAgua.IntentarUsar(o, Vector3.left, jugador), "Agua: disparó sin munición");

        // --- Bebedero: 0,4 s por carga → 1 s = 2 cargas, 3 s más = lleno
        var cinturon = jugador.AddComponent<CinturonHerramientas>();
        cinturon.Equipar(new Herramienta[] { hAgua, hMega });
        var beb = new GameObject("Bebedero");
        beb.AddComponent<BoxCollider>().isTrigger = true;
        var bebedero = beb.AddComponent<Bebedero>();
        bebedero.Rellenar(cinturon, 1f);
        Esperar(hAgua.Municion == 2, $"Bebedero: tras 1 s hay {hAgua.Municion} cargas, se esperaban 2");
        bebedero.Rellenar(cinturon, 3f);
        Esperar(hAgua.Municion == 6, $"Bebedero: tras 4 s hay {hAgua.Municion} cargas, se esperaban 6 (tope)");

        // --- Cinturón: ranuras y rueda
        Esperar(cinturon.Actual == hAgua, "Cinturón: la ranura inicial no es la 1");
        cinturon.Seleccionar(1);
        Esperar(cinturon.Actual == hMega, "Cinturón: la tecla 2 no eligió el megáfono");
        cinturon.Seleccionar(4);
        Esperar(cinturon.Actual == hMega, "Cinturón: una ranura vacía cambió la herramienta");
        cinturon.Ciclar(+1);
        Esperar(cinturon.Actual == hAgua, "Cinturón: la rueda no dio la vuelta");

        // --- Congelar (Derecho)
        hMega.Reiniciar();
        hMega.Congelar(3f);
        Esperar(!hMega.IntentarUsar(o, f, jugador), "Congelar: la herramienta se usó congelada");

        Object.DestroyImmediate(agua);
        Object.DestroyImmediate(mega);

        if (fallos.Count == 0)
        {
            Debug.Log("[Carlos] AUTOPRUEBA OK: megáfono, agua, munición, bebedero, cinturón y congelar.");
            return true;
        }
        foreach (var m in fallos) Debug.LogError("[Carlos] FALLO: " + m);
        return false;
    }

    // Para correrlo desde la consola (ver comentario arriba).
    public static void CrearTodoDesdeConsola()
    {
        EditorApplication.Exit(CrearTodoDesdeConsolaSinSalir() ? 0 : 1);
    }

    public static bool CrearTodoDesdeConsolaSinSalir()
    {
        bool ok = Autoprueba();
        IntegrarEnSampleScene();
        return ok;
    }

    // ------------------------------------------------------------------ utilidades

    static DianaPrueba Diana(string nombre, Vector3 pos, int capa, Material mat, string etiqueta, bool inocente, TipoHerramienta counter)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = nombre;
        go.layer = capa;
        go.transform.position = pos + Vector3.up;
        if (mat != null) Pintar(go, mat);
        var d = go.AddComponent<DianaPrueba>();
        d.etiqueta = etiqueta;
        d.esInocente = inocente;
        d.counter = counter;
        if (mat != null) Etiqueta(go.transform, etiqueta, 1.6f);
        return d;
    }

    static void Etiqueta(Transform padre, string texto, float altura)
    {
        var t = new GameObject("Etiqueta").AddComponent<TextMesh>();
        t.transform.SetParent(padre, false);
        t.transform.localPosition = new Vector3(0f, altura, 0f);
        t.text = texto;
        t.anchor = TextAnchor.LowerCenter;
        t.alignment = TextAlignment.Center;
        t.characterSize = 0.12f;
        t.fontSize = 48;
        t.color = Color.black;
    }

    static void Pintar(GameObject go, Material m) => go.GetComponent<Renderer>().sharedMaterial = m;

    static Material Mat(string ruta, Color color, bool unlit = false)
    {
        var m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (m == null)
        {
            var shader = Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit");
            m = new Material(shader);
            CrearCarpetas(Path.GetDirectoryName(ruta));
            AssetDatabase.CreateAsset(m, ruta);
        }
        m.SetColor("_BaseColor", color);
        EditorUtility.SetDirty(m);
        return m;
    }

    static T CargarOCrear<T>(string ruta, System.Action<T> alCrear) where T : ScriptableObject
    {
        var a = AssetDatabase.LoadAssetAtPath<T>(ruta);
        if (a != null) return a;
        a = ScriptableObject.CreateInstance<T>();
        alCrear(a);
        CrearCarpetas(Path.GetDirectoryName(ruta));
        AssetDatabase.CreateAsset(a, ruta);
        return a;
    }

    static void CrearCarpetas(string ruta)
    {
        ruta = ruta.Replace('\\', '/');
        if (AssetDatabase.IsValidFolder(ruta)) return;
        string padre = Path.GetDirectoryName(ruta).Replace('\\', '/');
        CrearCarpetas(padre);
        AssetDatabase.CreateFolder(padre, Path.GetFileName(ruta));
    }
}
