using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Herramientas de editor de Héctor (partida, HUD, resultados, audio y rendimiento). No van en el juego.
//  1. Crea Escena_Juego: la escena de Saldaña (que ya trae lo de Carlos) + la partida completa.
//  2. Aplica los ajustes de rendimiento para llegar a 60 FPS o más.
//  3. Autoprueba de las fórmulas y de la lógica de la partida (sin escena).
//  4. Abre Escena_Juego y le da Play.
public static class HectorPartidaEditor
{
    const string CarpetaDatos = "Assets/Datos/Partida";
    const string RutaConfig = CarpetaDatos + "/PartidaConfig.asset";
    const string RutaConfigEstudiantes = "Assets/Datos/Estudiantes/ConfigEstudiantes.asset";
    const string RutaConfigEstudiantesJuego = CarpetaDatos + "/ConfigEstudiantes_Juego.asset";
    const string RutaMatZona = CarpetaDatos + "/ZonaCaliente.mat";
    const string CarpetaAudio = "Assets/Audio";
    const string RutaSaldana = "Assets/Scenes/Escena_Saldana.unity";
    const string RutaEscena = "Assets/Scenes/Escena_Juego.unity";
    const string RutaPC = "Assets/Settings/PC_RPAsset.asset";
    const string RutaRendererPC = "Assets/Settings/PC_Renderer.asset";
    const string NombreGrupo = "Partida (Héctor)";

    // =============================== 1. Escena de juego ===============================

    [MenuItem("No te me colés/Héctor/1 - Crear escena de juego (Escena_Juego)", priority = 1)]
    public static void CrearEscena()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(RutaSaldana) == null)
        {
            Debug.LogError("[Héctor] Falta " + RutaSaldana + ". Créala primero con No te me colés → Saldaña → 2.");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        Carpeta(CarpetaDatos);
        var config = CargarOCrear<PartidaConfig>(RutaConfig);
        var configJuego = ConfigEstudiantesJuego();
        var matZona = MaterialZona();
        AsignarAudioSinSonido();

        // Si ya existe no se vuelve a copiar (se pierden cambios hechos a mano): solo se rehace el grupo de Héctor.
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(RutaEscena) == null && !AssetDatabase.CopyAsset(RutaSaldana, RutaEscena))
        {
            Debug.LogError("[Héctor] No pude copiar " + RutaSaldana + " a " + RutaEscena);
            return;
        }
        var escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);

        var viejo = GameObject.Find(NombreGrupo);
        if (viejo != null) Object.DestroyImmediate(viejo);

        // El HUD provisional de Carlos sobra: lo reemplaza el HUD final.
        foreach (var h in Object.FindObjectsByType<HUDHerramientasPrueba>(FindObjectsInactive.Include))
            h.enabled = false;
        // Sin las dianas de prueba (por si quedó alguna).
        foreach (var d in Object.FindObjectsByType<DianaPrueba>(FindObjectsInactive.Include))
            Object.DestroyImmediate(d.gameObject);

        var fila = Object.FindAnyObjectByType<Fila>();
        var spawner = Object.FindAnyObjectByType<SpawnerEstudiantes>();
        if (fila == null || spawner == null)
        {
            Debug.LogError("[Héctor] La escena no tiene la Fila o el Spawner de Saldaña.");
            return;
        }
        // Mismo balance que Saldaña, pero sin las etiquetas de depuración encima de cada estudiante.
        fila.config = configJuego;
        spawner.config = configJuego;
        EditorUtility.SetDirty(fila);
        EditorUtility.SetDirty(spawner);

        var grupo = new GameObject(NombreGrupo);
        var partida = grupo.AddComponent<PartidaManager>();
        partida.config = config;
        partida.fila = fila;
        partida.spawner = spawner;
        var medidor = grupo.AddComponent<MedidorRendimiento>();
        medidor.partida = partida;
        var hud = grupo.AddComponent<HUDPartida>();
        hud.partida = partida;
        hud.cinturon = Object.FindAnyObjectByType<CinturonHerramientas>();
        hud.medidor = medidor;
        grupo.AddComponent<TelemetriaPartida>().partida = partida;

        var audio = grupo.AddComponent<AudioPartida>();
        audio.partida = partida;
        audio.cinturon = hud.cinturon;
        audio.timbre = Clip("Timbre");
        audio.agua = Clip("Agua");
        audio.megafono = Clip("Megafono");
        audio.relleno = Clip("Relleno");
        audio.vacio = Clip("Vacio");
        audio.expulsado = Clip("Expulsado");
        audio.falsoPositivo = Clip("FalsoPositivo");
        audio.colado = Clip("Colado");
        audio.clickUI = Clip("ClickUI");

        var marcadores = new GameObject("Marcadores de la fila").AddComponent<MarcadoresFila>();
        marcadores.transform.SetParent(grupo.transform, false);
        marcadores.fila = fila;
        marcadores.partida = partida;
        marcadores.materialZona = matZona;

        if (Object.FindAnyObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            es.transform.SetParent(grupo.transform, false);
        }

        AjustarCamara();

        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);
        AgregarABuild(RutaEscena);
        Debug.Log("[Héctor] Escena de juego lista en " + RutaEscena + ". Dale Play (o No te me colés → Héctor → 4).");
    }

    // Copia de ConfigEstudiantes con las etiquetas apagadas (GDD: "apágalo para el playtest real").
    // Se vuelve a copiar cada vez para que los ajustes de Saldaña lleguen al juego.
    static ConfigEstudiantes ConfigEstudiantesJuego()
    {
        var original = AssetDatabase.LoadAssetAtPath<ConfigEstudiantes>(RutaConfigEstudiantes);
        var copia = AssetDatabase.LoadAssetAtPath<ConfigEstudiantes>(RutaConfigEstudiantesJuego);
        if (copia == null)
        {
            copia = original != null ? Object.Instantiate(original) : ScriptableObject.CreateInstance<ConfigEstudiantes>();
            AssetDatabase.CreateAsset(copia, RutaConfigEstudiantesJuego);
        }
        else if (original != null) EditorUtility.CopySerialized(original, copia);
        copia.name = "ConfigEstudiantes_Juego";
        copia.mostrarEtiqueta = false;
        EditorUtility.SetDirty(copia);
        AssetDatabase.SaveAssets();
        return copia;
    }

    static Material MaterialZona()
    {
        var m = AssetDatabase.LoadAssetAtPath<Material>(RutaMatZona);
        if (m != null) return m;
        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        m = new Material(shader);
        // Unlit transparente: una franja roja suave sobre el piso.
        m.SetFloat("_Surface", 1f);
        m.SetFloat("_Blend", 0f);
        m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_ZWrite", 0f);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = (int)RenderQueue.Transparent;
        m.SetOverrideTag("RenderType", "Transparent");
        m.SetColor("_BaseColor", new Color(1f, 0.2f, 0.15f, 0.28f));
        AssetDatabase.CreateAsset(m, RutaMatZona);
        return m;
    }

    // Los HerramientaData de Carlos no tienen sonidos: suena AudioPartida (en 2D). Aquí solo se avisa.
    static void AsignarAudioSinSonido()
    {
        foreach (var guid in AssetDatabase.FindAssets("t:HerramientaData"))
        {
            var d = AssetDatabase.LoadAssetAtPath<HerramientaData>(AssetDatabase.GUIDToAssetPath(guid));
            if (d != null && d.sonidoUso != null)
                Debug.Log($"[Héctor] {d.nombre} ya tiene sonido propio (3D); también sonará el de AudioPartida.");
        }
    }

    static AudioClip Clip(string nombre)
    {
        var c = AssetDatabase.LoadAssetAtPath<AudioClip>($"{CarpetaAudio}/{nombre}.wav");
        if (c == null) Debug.LogWarning($"[Héctor] Falta {CarpetaAudio}/{nombre}.wav");
        return c;
    }

    // La cámara a 35 m (antes 60) da el mismo encuadre ortográfico y deja usar sombras más cortas.
    static void AjustarCamara()
    {
        var cam = Object.FindAnyObjectByType<CamaraIsometrica>();
        if (cam == null) return;
        cam.distancia = 35f;
        cam.tamano = 8f;          // un poco más cerca del jugador (antes 10)
        cam.radioCorte = 8f;      // menos techo oculto alrededor (antes 14)
        cam.soloBajoTecho = true; // y solo cuando el jugador está bajo techo
        var c = cam.GetComponent<Camera>();
        c.nearClipPlane = 1f;
        c.farClipPlane = 90f;
        EditorUtility.SetDirty(cam);
        EditorUtility.SetDirty(c);
    }

    static void AgregarABuild(string ruta)
    {
        var lista = EditorBuildSettings.scenes.ToList();
        lista.RemoveAll(s => s.path == ruta);
        lista.Insert(0, new EditorBuildSettingsScene(ruta, true));
        EditorBuildSettings.scenes = lista.ToArray();
    }

    // =============================== 2. Rendimiento ===============================

    [MenuItem("No te me colés/Héctor/2 - Optimizar rendimiento (60 FPS)", priority = 2)]
    public static void Optimizar()
    {
        var log = new StringBuilder("[Héctor] Ajustes de rendimiento:\n");
        var pc = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(RutaPC);
        if (pc == null) { Debug.LogError("[Héctor] No encontré " + RutaPC); return; }

        var so = new SerializedObject(pc);
        // Sombras: la cámara ortográfica ve el piso entre ~20 y ~52 m; 55 m y una sola cascada alcanzan.
        Poner(so, "m_ShadowDistance", 55f, log);
        Poner(so, "m_ShadowCascadeCount", 1, log);
        Poner(so, "m_MainLightShadowmapResolution", 2048, log);
        Poner(so, "m_SoftShadowQuality", 2, log);                 // alta → media
        Poner(so, "m_AdditionalLightShadowsSupported", false, log); // la escena solo tiene la luz del sol
        // Antialiasing: la cámara ya usa SMAA; MSAA 4x encima es pagar dos veces.
        Poner(so, "m_MSAA", 1, log);
        // Nadie usa la textura de color ni la de profundidad (URP las pide solo si un efecto las necesita).
        Poner(so, "m_RequireOpaqueTexture", false, log);
        Poner(so, "m_RequireDepthTexture", false, log);
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(pc);

        // SSAO vuelve a dibujar los ~3800 pedazos del edificio en un pase de normales: se QUITA del renderer.
        // Ojo: solo desactivarlo rompe la build (URP elimina sus shaders al compilar y el renderer
        // falla al crearse: "Couldn't find the required resources for the ScreenSpaceAmbientOcclusion").
        // Para volver a tenerlo: PC_Renderer → Add Renderer Feature → Screen Space Ambient Occlusion.
        var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RutaRendererPC);
        if (renderer != null)
        {
            var sr = new SerializedObject(renderer);
            var lista = sr.FindProperty("m_RendererFeatures");
            var mapa = sr.FindProperty("m_RendererFeatureMap");
            for (int i = lista.arraySize - 1; i >= 0; i--)
            {
                var f = lista.GetArrayElementAtIndex(i).objectReferenceValue as ScriptableRendererFeature;
                if (f == null || !f.GetType().Name.Contains("ScreenSpaceAmbientOcclusion")) continue;
                int antes = lista.arraySize;
                lista.GetArrayElementAtIndex(i).objectReferenceValue = null;
                lista.DeleteArrayElementAtIndex(i);
                if (lista.arraySize == antes) lista.DeleteArrayElementAtIndex(i);
                if (mapa != null && i < mapa.arraySize) mapa.DeleteArrayElementAtIndex(i);
                sr.ApplyModifiedProperties();
                AssetDatabase.RemoveObjectFromAsset(f);
                Object.DestroyImmediate(f, true);
                log.AppendLine("  SSAO (renderer feature) → quitado del PC_Renderer");
            }
            renderer.SetDirty();
            EditorUtility.SetDirty(renderer);
        }

        // Medir sin el tope del monitor: VSync apagado en la calidad PC.
        if (QualitySettings.vSyncCount != 0) { QualitySettings.vSyncCount = 0; log.AppendLine("  VSync → apagado"); }

        AssetDatabase.SaveAssets();
        Debug.Log(log.ToString());
        Escribir("optimizacion_hector.txt", log.ToString());
    }

    static void Poner(SerializedObject so, string prop, object valor, StringBuilder log)
    {
        var p = so.FindProperty(prop);
        if (p == null) { log.AppendLine($"  {prop}: no existe en esta versión de URP"); return; }
        string antes;
        switch (p.propertyType)
        {
            case SerializedPropertyType.Float: antes = p.floatValue.ToString(); p.floatValue = System.Convert.ToSingle(valor); break;
            case SerializedPropertyType.Boolean: antes = p.boolValue.ToString(); p.boolValue = (bool)valor; break;
            case SerializedPropertyType.Enum: antes = p.intValue.ToString(); p.intValue = System.Convert.ToInt32(valor); break;
            default: antes = p.intValue.ToString(); p.intValue = System.Convert.ToInt32(valor); break;
        }
        log.AppendLine($"  {prop}: {antes} → {valor}");
    }

    // =============================== 3. Autoprueba ===============================

    [MenuItem("No te me colés/Héctor/3 - Autoprueba de la partida", priority = 3)]
    public static void Autoprueba()
    {
        var r = new StringBuilder();
        int ok = 0, mal = 0;
        void Revisar(string nombre, bool cond, string detalle = "")
        {
            if (cond) ok++; else mal++;
            r.AppendLine($"{(cond ? "OK  " : "FALLA")}  {nombre}{(string.IsNullOrEmpty(detalle) ? "" : "  → " + detalle)}");
        }
        bool Cerca(float a, float b) => Mathf.Abs(a - b) < 0.01f;

        // Fórmulas del GDD 10.2
        Revisar("Efectividad 9 frustrados, 1 infiltrado = 90", Cerca(Puntaje.Efectividad(9, 1, 0), 90f));
        Revisar("Efectividad resta 5 por falso positivo", Cerca(Puntaje.Efectividad(9, 1, 2), 80f));
        Revisar("Efectividad nunca baja de 0", Cerca(Puntaje.Efectividad(0, 5, 10), 0f));
        Revisar("Efectividad sin colados = 100", Cerca(Puntaje.Efectividad(0, 0, 0), 100f));
        Revisar("Rango S exige 0 falsos positivos", Puntaje.Rango(96f, 1, false) == "A" && Puntaje.Rango(96f, 0, false) == "S");
        Revisar("Rangos A/B/C/D por umbral", Puntaje.Rango(85f, 0, false) == "A" && Puntaje.Rango(70f, 0, false) == "B"
                                             && Puntaje.Rango(50f, 0, false) == "C" && Puntaje.Rango(49.9f, 0, false) == "D");
        Revisar("Derrota da rango Expulsado", Puntaje.Rango(100f, 0, true) == Puntaje.RangoDerrota);
        Revisar("Bonos de Dignidad 100/60/30/10/0", Puntaje.DignidadBonus("S") == 100 && Puntaje.DignidadBonus("A") == 60
                                                   && Puntaje.DignidadBonus("B") == 30 && Puntaje.DignidadBonus("C") == 10 && Puntaje.DignidadBonus("D") == 0);
        Revisar("Puesto #40 sin infiltrados → 12:20", Puntaje.Hora(720 + Puntaje.MinutosAlmuerzo(40, 0)) == "12:20 p. m.",
                Puntaje.Hora(720 + Puntaje.MinutosAlmuerzo(40, 0)));
        Revisar("Puesto #40 con 30 delante → 12:35", Puntaje.Hora(720 + Puntaje.MinutosAlmuerzo(40, 30)) == "12:35 p. m.");
        Revisar("Hora 10:00 a. m. y 1:10 p. m.", Puntaje.Hora(600) == "10:00 a. m." && Puntaje.Hora(790) == "1:10 p. m.");
        Revisar("Reloj: 120 s de 240 = 11:00", Cerca(Puntaje.MinutoDeJuego(120f, 240f, 600, 720), 660f));

        // Lógica del PartidaManager (sin escena: se llama a sus manejadores directamente).
        var go = new GameObject("Autoprueba Héctor") { hideFlags = HideFlags.HideAndDontSave };
        var cfg = ScriptableObject.CreateInstance<PartidaConfig>();
        PartidaManager pm = null;
        try
        {
            pm = go.AddComponent<PartidaManager>();
            pm.config = cfg;
            pm.Inicializar();                       // lo que hace Awake: Paciencia 100, tiempo detenido
            Revisar("Empieza en la intro con el tiempo detenido", pm.Estado == EstadoPartida.Intro && Time.timeScale == 0f);
            pm.AlExpulsado(null);
            Revisar("En la intro los eventos no cuentan", pm.Stats.expulsados == 0);
            pm.Empezar();
            Revisar("Empezar pasa a Jugando", pm.Estado == EstadoPartida.Jugando);
            pm.AlExpulsado(null); pm.AlExpulsado(null); pm.AlDesistido(null);
            Revisar("Expulsados y desistidos suman Dignidad 10+10+5", pm.Stats.Frustrados == 3 && pm.Stats.dignidad == 25, $"dignidad {pm.Stats.dignidad}");
            pm.AlAsegurado(null);
            Revisar("Un infiltrado sin zona caliente cuenta detrás", pm.Stats.infiltradosDetras == 1 && pm.Escandalo == 0f);
            pm.AlFalsoPositivo(null);
            Revisar("Falso positivo: Paciencia −15", Cerca(pm.Paciencia, 85f), pm.Paciencia.ToString());
            pm.AlSobreAsegurado(null);
            Revisar("Actuar sobre un Asegurado: Paciencia −5", Cerca(pm.Paciencia, 80f));
            pm.Pausar(true);
            pm.AlFalsoPositivo(null);
            Revisar("En pausa los eventos no cuentan", pm.Stats.falsosPositivos == 1 && Time.timeScale == 0f);
            pm.Pausar(false);
            pm.Terminar(false);
            var res = pm.Resultado;
            // 3 frustrados, 1 infiltrado, 1 falso positivo → 75 − 5 = 70 → B (+30 de Dignidad)
            Revisar("Resultado: efectividad 70 y rango B", res != null && Cerca(res.efectividad, 70f) && res.rango == "B",
                    res != null ? $"{res.efectividad:0.0} {res.rango}" : "sin resultado");
            Revisar("El bono del rango se suma a la Dignidad", pm.Stats.dignidad == 55, pm.Stats.dignidad.ToString());
            Revisar("Al terminar la acción se congela", pm.Estado == EstadoPartida.Terminada && Time.timeScale == 0f);
            Revisar("Puesto del jugador por defecto es el #40", pm.PuestoJugador == 40, pm.PuestoJugador.ToString());

            // Derrota: 7 falsos positivos (7 × 15 > 100) vacían la Paciencia.
            Object.DestroyImmediate(pm);
            pm = go.AddComponent<PartidaManager>();
            pm.config = cfg;
            pm.Inicializar();
            pm.Empezar();
            for (int i = 0; i < 7; i++) pm.AlFalsoPositivo(null);
            Revisar("Paciencia en 0 termina la partida en derrota", pm.Estado == EstadoPartida.Terminada && pm.Derrota
                    && pm.Resultado != null && pm.Resultado.rango == Puntaje.RangoDerrota, $"{pm.Estado} paciencia {pm.Paciencia}");
        }
        catch (System.Exception ex)
        {
            mal++;
            r.AppendLine("FALLA  Excepción: " + ex);
        }
        finally
        {
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(cfg);
            Time.timeScale = 1f;
        }

        string texto = $"[Héctor] Autoprueba: {ok} OK, {mal} fallas\n{r}";
        if (mal == 0) Debug.Log(texto); else Debug.LogError(texto);
        Escribir("autoprueba_hector.txt", texto);
    }

    // =============================== 4. Jugar ===============================

    [MenuItem("No te me colés/Héctor/4 - Jugar (abre Escena_Juego)", priority = 4)]
    public static void Jugar()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(RutaEscena) == null) CrearEscena();
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
        EditorApplication.isPlaying = true;
    }

    // =============================== 5. Build de prueba de rendimiento ===============================

    // Compila Escena_Juego para Windows en Builds/Windows (ignorada por Git), la ejecuta en modo
    // prueba (arranca sola, 240 s, guarda Builds/Windows/Logs/rendimiento_*.txt y se cierra).
    [MenuItem("No te me colés/Héctor/5 - Build de prueba de rendimiento (Windows)", priority = 5)]
    public static void BuildPruebaRendimiento()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(RutaEscena) == null) CrearEscena();
        string carpeta = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Builds", "Windows"));
        Directory.CreateDirectory(carpeta);
        File.WriteAllText(Path.Combine(carpeta, "prueba_rendimiento.txt"),
            "Si este archivo existe, el juego corre una partida sola y guarda el informe en Logs/. Bórralo para jugar normal.");

        // Ventana de 1920×1080 para medir igual que en el editor; se restaura al terminar.
        var modo = PlayerSettings.fullScreenMode;
        int ancho = PlayerSettings.defaultScreenWidth, alto = PlayerSettings.defaultScreenHeight;
        try
        {
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            var opciones = new BuildPlayerOptions
            {
                scenes = new[] { RutaEscena },
                locationPathName = Path.Combine(carpeta, "NoTeMeColes.exe"),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.AutoRunPlayer,
            };
            var informe = BuildPipeline.BuildPlayer(opciones);
            string texto = $"[Héctor] Build {informe.summary.result} en {informe.summary.totalTime.TotalSeconds:0} s, " +
                           $"{informe.summary.totalSize / (1024 * 1024)} MB → {opciones.locationPathName}";
            Debug.Log(texto);
            Escribir("build_hector.txt", texto);
        }
        finally
        {
            PlayerSettings.fullScreenMode = modo;
            PlayerSettings.defaultScreenWidth = ancho;
            PlayerSettings.defaultScreenHeight = alto;
            AssetDatabase.SaveAssets();   // que ProjectSettings quede como estaba
        }
    }

    // =============================== Utilidades ===============================

    static void Escribir(string archivo, string texto)
    {
        string carpeta = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs"));
        Directory.CreateDirectory(carpeta);
        File.WriteAllText(Path.Combine(carpeta, archivo), texto, Encoding.UTF8);
    }

    static T CargarOCrear<T>(string ruta) where T : ScriptableObject
    {
        var a = AssetDatabase.LoadAssetAtPath<T>(ruta);
        if (a != null) return a;
        a = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(a, ruta);
        return a;
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
