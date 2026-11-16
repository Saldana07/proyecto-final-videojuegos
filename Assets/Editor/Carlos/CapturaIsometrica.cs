using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Saca capturas de la SampleScene con la cámara isométrica (con el edificio oculto
// alrededor del jugador) para revisar cómo se ve sin dar Play. Se guardan en Logs/.
public static class CapturaIsometrica
{
    [MenuItem("No te me colés/Carlos/Capturas de la vista isométrica")]
    public static void Capturar()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);

        var iso = Object.FindAnyObjectByType<CamaraIsometrica>();
        var jugador = Object.FindAnyObjectByType<JugadorIsometrico>();
        if (iso == null || jugador == null) { Debug.LogError("[Carlos] Falta integrar la vista isométrica."); return; }

        Vector3 inicio = jugador.transform.position;
        // Inicio, en la fila del vestíbulo y dentro del corredor.
        var puntos = new[] { inicio, new Vector3(227f, inicio.y, 57f), new Vector3(233f, inicio.y, 70f) };
        string carpeta = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Logs");
        var cam = iso.GetComponent<Camera>();

        for (int i = 0; i < puntos.Length; i++)
        {
            jugador.transform.position = puntos[i];
            Physics.SyncTransforms();
            iso.Inicializar();
            iso.ActualizarAhora();

            var rt = new RenderTexture(1280, 720, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            cam.targetTexture = null;
            RenderTexture.active = null;
            File.WriteAllBytes(Path.Combine(carpeta, $"captura_iso_{i}.png"), tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            rt.Release();
            iso.enabled = false;   // OnDisable vuelve a mostrar todo antes de la siguiente
            iso.enabled = true;
        }
        Debug.Log("[Carlos] Capturas en " + carpeta);
        // No se guarda la escena: las posiciones de prueba se descartan.
    }

    public static void CapturarDesdeConsola()
    {
        CarlosHerramientasEditor.CrearTodoDesdeConsolaSinSalir();
        Capturar();
        EditorApplication.Exit(0);
    }
}
