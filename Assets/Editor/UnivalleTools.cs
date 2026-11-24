// Herramientas para quedarse solo con un edificio del modelo SketchUp de Univalle.
// Aparecen en el menu superior de Unity: Univalle > ...
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class UnivalleTools
{
    const string Carpeta = "Assets/Edificio";

    static string RutaProyecto => Directory.GetParent(Application.dataPath).FullName;

    // ---------------------------------------------------------------- 1. Analizar
    [MenuItem("Univalle/1 - Analizar pieza seleccionada")]
    static void Analizar()
    {
        var go = Selection.activeGameObject;
        if (go == null || !go.scene.IsValid())
        {
            EditorUtility.DisplayDialog("Univalle", "Primero haz clic sobre una parte del edificio en la escena (o en la Hierarchy).", "OK");
            return;
        }
        var raiz = PrefabUtility.GetOutermostPrefabInstanceRoot(go);
        var raizT = raiz != null ? raiz.transform : go.transform.root;

        var sb = new StringBuilder();
        sb.AppendLine("Modelo: " + Describir(raizT));
        var src = PrefabUtility.GetCorrespondingObjectFromOriginalSource(raizT.gameObject);
        if (src != null) sb.AppendLine("Asset: " + AssetDatabase.GetAssetPath(src));
        sb.AppendLine();

        var cadena = new List<Transform>();
        for (var t = go.transform; t != null; t = t.parent) { cadena.Add(t); if (t == raizT) break; }
        cadena.Reverse();

        sb.AppendLine("== Cadena desde la raiz hasta la pieza seleccionada ==");
        for (int i = 0; i < cadena.Count; i++)
            sb.AppendLine(new string(' ', i * 2) + "[" + i + "] " + Describir(cadena[i]));
        sb.AppendLine();

        for (int i = 0; i < cadena.Count; i++)
        {
            var t = cadena[i];
            if (t.childCount == 0) continue;
            sb.AppendLine("== Hijos de [" + i + "] " + t.name + " (total " + t.childCount + ", los 60 con mas piezas) ==");
            var hijos = new List<KeyValuePair<Transform, int>>();
            foreach (Transform h in t)
            {
                TryBounds(h, out _, out int n);
                hijos.Add(new KeyValuePair<Transform, int>(h, n));
            }
            foreach (var h in hijos.OrderByDescending(x => x.Value).Take(60))
                sb.AppendLine((cadena.Contains(h.Key) ? "  -> " : "     ") + "(" + h.Key.GetSiblingIndex() + ") " + Describir(h.Key));
            sb.AppendLine();
        }

        var ruta = Path.Combine(RutaProyecto, "univalle_analisis.txt");
        File.WriteAllText(ruta, sb.ToString());
        Debug.Log("[Univalle] Analisis guardado en " + ruta);

        // Deja las mallas legibles para poder copiarlas en el paso 2.
        HacerLegible(raizT.gameObject);

        EditorUtility.DisplayDialog("Univalle", "Listo. Ya puedes avisarle a Claude.", "OK");
    }

    // ---------------------------------------------------------------- 2. Crear edificio
    [MenuItem("Univalle/2 - Crear edificio con lo seleccionado")]
    static void Crear()
    {
        var sel = Selection.gameObjects.Where(g => g.scene.IsValid()).ToList();
        if (sel.Count == 0)
        {
            EditorUtility.DisplayDialog("Univalle", "Selecciona en la Hierarchy el/los objetos del edificio.", "OK");
            return;
        }
        // Quita los que ya estan dentro de otro seleccionado
        sel = sel.Where(g => !sel.Any(o => o != g && g.transform.IsChildOf(o.transform))).ToList();
        var raiz = PrefabUtility.GetOutermostPrefabInstanceRoot(sel[0]);

        CrearCarpeta(Carpeta);
        CrearCarpeta(Carpeta + "/Materiales");
        CrearCarpeta(Carpeta + "/Texturas");

        Bounds total = default; bool hay = false;
        foreach (var g in sel)
            if (TryBounds(g.transform, out var b, out _)) { if (!hay) { total = b; hay = true; } else total.Encapsulate(b); }

        var edificio = new GameObject("Edificio_Univalle");
        edificio.transform.position = hay ? new Vector3(total.center.x, total.min.y, total.center.z) : Vector3.zero;
        foreach (var g in sel)
        {
            var copia = Object.Instantiate(g, g.transform.position, g.transform.rotation);
            copia.name = g.name;
            copia.transform.localScale = g.transform.lossyScale;
            copia.transform.SetParent(edificio.transform, true);
        }

        var mallas = new Dictionary<Mesh, Mesh>();
        var mats = new Dictionary<Material, Material>();
        var texs = new Dictionary<Texture, Texture>();
        Object contenedor = null;
        var rutaMallas = AssetDatabase.GenerateUniqueAssetPath(Carpeta + "/Edificio_Mallas.asset");
        try
        {
            var filtros = edificio.GetComponentsInChildren<MeshFilter>(true);
            for (int i = 0; i < filtros.Length; i++)
            {
                if (i % 25 == 0) EditorUtility.DisplayProgressBar("Univalle", "Copiando mallas...", (float)i / filtros.Length);
                filtros[i].sharedMesh = CopiarMalla(filtros[i].sharedMesh, mallas, ref contenedor, rutaMallas);
            }
            foreach (var mc in edificio.GetComponentsInChildren<MeshCollider>(true))
                mc.sharedMesh = CopiarMalla(mc.sharedMesh, mallas, ref contenedor, rutaMallas);

            EditorUtility.DisplayProgressBar("Univalle", "Copiando materiales...", 1f);
            foreach (var r in edificio.GetComponentsInChildren<Renderer>(true))
            {
                var ms = r.sharedMaterials;
                for (int i = 0; i < ms.Length; i++) ms[i] = CopiarMaterial(ms[i], mats, texs);
                r.sharedMaterials = ms;
            }
        }
        finally { EditorUtility.ClearProgressBar(); }
        AssetDatabase.SaveAssets();

        var rutaPrefab = AssetDatabase.GenerateUniqueAssetPath(Carpeta + "/Edificio_Univalle.prefab");
        PrefabUtility.SaveAsPrefabAssetAndConnect(edificio, rutaPrefab, InteractionMode.UserAction);

        if (raiz != null)
        {
            Undo.RecordObject(raiz, "Ocultar campus");
            raiz.SetActive(false);
        }
        EditorSceneManager.MarkSceneDirty(edificio.scene);
        Selection.activeGameObject = edificio;
        SceneView.FrameLastActiveSceneView();

        EditorUtility.DisplayDialog("Univalle",
            "Edificio creado: " + rutaPrefab + "\n\nMallas: " + mallas.Count + "  Materiales: " + mats.Count +
            "\n\nEl campus completo quedo desactivado en la escena (no borrado).", "OK");
    }

    // ---------------------------------------------------------------- 3. Suelo
    [MenuItem("Univalle/3 - Poner suelo bajo el edificio")]
    static void PonerSuelo()
    {
        if (!BuscarEdificio(out var edificio, out var b)) return;
        var suelo = CrearSuelo(b);
        EditorSceneManager.MarkSceneDirty(suelo.scene);
        Selection.activeGameObject = suelo;
        EditorUtility.DisplayDialog("Univalle", "Suelo creado. Guarda con Ctrl+S.", "OK");
    }

    // ---------------------------------------------------------------- 4. Recorrido
    [MenuItem("Univalle/4 - Preparar recorrido (suelo, colisiones y jugador)")]
    static void PrepararRecorrido()
    {
        if (!BuscarEdificio(out var edificio, out var b)) return;

        // Suelo
        if (GameObject.Find("Suelo") == null) CrearSuelo(b);

        // Colisiones dentro del prefab del edificio
        var rutaPrefab = AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(edificio));
        int agregados = 0;
        if (!string.IsNullOrEmpty(rutaPrefab))
        {
            var contenido = PrefabUtility.LoadPrefabContents(rutaPrefab);
            try
            {
                var filtros = contenido.GetComponentsInChildren<MeshFilter>(true);
                for (int i = 0; i < filtros.Length; i++)
                {
                    if (i % 50 == 0) EditorUtility.DisplayProgressBar("Univalle", "Agregando colisiones...", (float)i / filtros.Length);
                    var mf = filtros[i];
                    if (mf.sharedMesh == null || mf.GetComponent<Collider>() != null) continue;
                    mf.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
                    agregados++;
                }
                foreach (var t in contenido.GetComponentsInChildren<Transform>(true)) t.gameObject.isStatic = true;
                PrefabUtility.SaveAsPrefabAsset(contenido, rutaPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contenido);
                EditorUtility.ClearProgressBar();
            }
        }

        // Jugador en primera persona, frente al edificio mirandolo
        var viejo = GameObject.Find("Jugador");
        if (viejo != null) Undo.DestroyObjectImmediate(viejo);
        var jugador = new GameObject("Jugador");
        var cc = jugador.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.35f;
        cc.center = new Vector3(0f, 0.9f, 0f);
        cc.stepOffset = 0.45f;
        cc.slopeLimit = 50f;
        cc.skinWidth = 0.05f;
        jugador.transform.position = new Vector3(b.center.x, b.min.y + 0.1f, b.min.z - 8f);

        var camGO = new GameObject("Camara");
        camGO.tag = "MainCamera";
        var cam = camGO.AddComponent<Camera>();
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 3000f;
        camGO.AddComponent<AudioListener>();
        camGO.transform.SetParent(jugador.transform, false);
        camGO.transform.localPosition = new Vector3(0f, 1.65f, 0f);

        var control = jugador.AddComponent<JugadorPrimeraPersona>();
        control.camara = camGO.transform;
        Undo.RegisterCreatedObjectUndo(jugador, "Crear jugador");

        // Apaga la camara original para que no haya dos
        foreach (var c in Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude))
            if (c != cam) { Undo.RecordObject(c.gameObject, "Apagar camara"); c.gameObject.SetActive(false); }

        var escena = jugador.scene;
        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);
        Selection.activeGameObject = jugador;

        EditorUtility.DisplayDialog("Univalle",
            "Listo y guardado.\n\nColisiones agregadas: " + agregados +
            "\n\nDale Play y usa:\nWASD caminar | Mouse mirar | Shift correr | Espacio saltar | Esc soltar el mouse", "OK");
    }

    static bool BuscarEdificio(out GameObject edificio, out Bounds b)
    {
        b = default;
        edificio = GameObject.Find("Edificio_Univalle");
        if (edificio == null || !TryBounds(edificio.transform, out b, out _))
        {
            EditorUtility.DisplayDialog("Univalle", "No encontre Edificio_Univalle activo en la escena.", "OK");
            return false;
        }
        return true;
    }

    static GameObject CrearSuelo(Bounds b)
    {
        var viejo = GameObject.Find("Suelo");
        if (viejo != null) Undo.DestroyObjectImmediate(viejo);

        CrearCarpeta(Carpeta + "/Materiales");
        var rutaMat = Carpeta + "/Materiales/Suelo_Cesped.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(rutaMat);
        if (mat == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            mat = new Material(shader) { name = "Suelo_Cesped" };
            var verde = new Color(0.30f, 0.50f, 0.20f);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", verde);
            mat.color = verde;
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.1f);
            AssetDatabase.CreateAsset(mat, rutaMat);
        }

        var suelo = GameObject.CreatePrimitive(PrimitiveType.Plane); // incluye MeshCollider
        suelo.name = "Suelo";
        float lado = Mathf.Max(b.size.x, b.size.z) * 3f;
        suelo.transform.position = new Vector3(b.center.x, b.min.y - 0.03f, b.center.z);
        suelo.transform.localScale = new Vector3(lado / 10f, 1f, lado / 10f); // el Plane mide 10x10
        suelo.GetComponent<Renderer>().sharedMaterial = mat;
        suelo.isStatic = true;
        Undo.RegisterCreatedObjectUndo(suelo, "Crear suelo");
        return suelo;
    }

    // ---------------------------------------------------------------- utilidades
    static bool EsDelSkp(Object o) =>
        o != null && AssetDatabase.GetAssetPath(o).EndsWith(".skp", System.StringComparison.OrdinalIgnoreCase);

    static Mesh CopiarMalla(Mesh m, Dictionary<Mesh, Mesh> mapa, ref Object contenedor, string ruta)
    {
        if (m == null || !EsDelSkp(m)) return m;
        if (mapa.TryGetValue(m, out var c)) return c;
        c = Object.Instantiate(m);
        c.name = m.name;
        if (contenedor == null) { AssetDatabase.CreateAsset(c, ruta); contenedor = c; }
        else AssetDatabase.AddObjectToAsset(c, contenedor);
        mapa[m] = c;
        return c;
    }

    static Material CopiarMaterial(Material m, Dictionary<Material, Material> mapa, Dictionary<Texture, Texture> texs)
    {
        if (m == null || !EsDelSkp(m)) return m;
        if (mapa.TryGetValue(m, out var c)) return c;
        c = new Material(m) { name = m.name };
        foreach (var prop in m.GetTexturePropertyNames())
        {
            var t = m.GetTexture(prop);
            if (t != null && EsDelSkp(t)) c.SetTexture(prop, CopiarTextura(t, texs));
        }
        var ruta = AssetDatabase.GenerateUniqueAssetPath(Carpeta + "/Materiales/" + Seguro(m.name) + ".mat");
        AssetDatabase.CreateAsset(c, ruta);
        mapa[m] = c;
        return c;
    }

    static Texture CopiarTextura(Texture t, Dictionary<Texture, Texture> mapa)
    {
        if (mapa.TryGetValue(t, out var c)) return c;
        var rt = RenderTexture.GetTemporary(t.width, t.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        Graphics.Blit(t, rt);
        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        var tex = new Texture2D(t.width, t.height, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, t.width, t.height), 0, 0);
        tex.Apply();
        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);
        var ruta = AssetDatabase.GenerateUniqueAssetPath(Carpeta + "/Texturas/" + Seguro(t.name) + ".png");
        File.WriteAllBytes(Path.Combine(RutaProyecto, ruta), tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(ruta);
        c = AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
        mapa[t] = c;
        return c;
    }

    static void HacerLegible(GameObject raiz)
    {
        var src = PrefabUtility.GetCorrespondingObjectFromOriginalSource(raiz);
        if (src == null) return;
        var imp = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(src)) as ModelImporter;
        if (imp != null && !imp.isReadable) { imp.isReadable = true; imp.SaveAndReimport(); }
    }

    static bool TryBounds(Transform t, out Bounds b, out int count)
    {
        var rs = t.GetComponentsInChildren<Renderer>();
        count = rs.Length;
        b = default;
        if (rs.Length == 0) return false;
        b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        return true;
    }

    static string Describir(Transform t)
    {
        if (!TryBounds(t, out var b, out int n)) return t.name + " | hijos=" + t.childCount + " | sin geometria";
        return t.name + " | hijos=" + t.childCount + " | piezas=" + n + " | tamano=" + F(b.size) + " | centro=" + F(b.center);
    }

    static string F(Vector3 v) =>
        string.Format(CultureInfo.InvariantCulture, "({0:F1}, {1:F1}, {2:F1})", v.x, v.y, v.z);

    static string Seguro(string s)
    {
        foreach (var ch in Path.GetInvalidFileNameChars()) s = s.Replace(ch, '_');
        return string.IsNullOrWhiteSpace(s) ? "sin_nombre" : s;
    }

    static void CrearCarpeta(string ruta)
    {
        if (AssetDatabase.IsValidFolder(ruta)) return;
        var padre = Path.GetDirectoryName(ruta).Replace('\\', '/');
        CrearCarpeta(padre);
        AssetDatabase.CreateFolder(padre, Path.GetFileName(ruta));
    }
}
