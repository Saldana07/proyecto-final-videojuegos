using System.Collections.Generic;
using System.IO;
using System.Text;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;

// Mide los FPS durante la partida y deja un informe en Logs/rendimiento_*.txt (meta: ≥ 60 FPS).
// F3 lo muestra en pantalla. Dueño: Héctor.
public class MedidorRendimiento : MonoBehaviour
{
    public PartidaManager partida;
    [Tooltip("Escribe el informe al terminar la partida.")]
    public bool guardarInforme = true;

    public float FpsActual { get; private set; }
    public string Resumen { get; private set; } = "";

    readonly List<float> cuadros = new List<float>(60000);   // ms de cada cuadro jugando
    float ventana;
    int cuadrosVentana;
    int maxColados, coladosAhora;
    ProfilerRecorder batches, setPass, tris, sombras, hiloPrincipal;
    readonly FrameTiming[] tiempos = new FrameTiming[1];
    double gpuMs, cpuMs;
    readonly StringBuilder sb = new StringBuilder(256);

    void Awake()
    {
        if (partida == null) partida = FindAnyObjectByType<PartidaManager>();
    }

    void OnEnable()
    {
        batches = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count");
        setPass = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
        tris = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
        sombras = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Shadow Casters Count");
        hiloPrincipal = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 15);
        Estudiante.OnCambioEstado += AlCambioEstado;
        if (partida != null) partida.EstadoCambiado += AlEstado;
    }

    void OnDisable()
    {
        batches.Dispose(); setPass.Dispose(); tris.Dispose(); sombras.Dispose(); hiloPrincipal.Dispose();
        Estudiante.OnCambioEstado -= AlCambioEstado;
        if (partida != null) partida.EstadoCambiado -= AlEstado;
    }

    void AlCambioEstado(Estudiante e, EstadoEstudiante s)
    {
        if (e == null || e.EsInocente) return;
        if (s == EstadoEstudiante.Merodeo && e.Puesto < 0) coladosAhora++;
        else if (s == EstadoEstudiante.Asegurado || s == EstadoEstudiante.Expulsado || s == EstadoEstudiante.Desistido)
            coladosAhora = Mathf.Max(0, coladosAhora - 1);
        maxColados = Mathf.Max(maxColados, coladosAhora);
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        bool jugando = partida == null || partida.Estado == EstadoPartida.Jugando;
        if (jugando && Time.frameCount > 5) cuadros.Add(dt * 1000f);

        ventana += dt;
        cuadrosVentana++;
        if (ventana >= 0.5f)
        {
            FpsActual = cuadrosVentana / ventana;
            ventana = 0f;
            cuadrosVentana = 0;
            FrameTimingManager.CaptureFrameTimings();
            if (FrameTimingManager.GetLatestTimings(1, tiempos) > 0)
            {
                gpuMs = tiempos[0].gpuFrameTime;
                cpuMs = tiempos[0].cpuMainThreadFrameTime;
            }
            ArmarResumen();
        }
    }

    void ArmarResumen()
    {
        sb.Clear();
        sb.Append("<b>").Append(FpsActual.ToString("0")).Append(" FPS</b>  (").Append((1000f / Mathf.Max(1f, FpsActual)).ToString("0.0")).Append(" ms)\n");
        if (hiloPrincipal.Valid && hiloPrincipal.Count > 0) sb.Append("CPU ").Append((Promedio(hiloPrincipal) * 1e-6).ToString("0.0")).Append(" ms  ");
        if (gpuMs > 0) sb.Append("GPU ").Append(gpuMs.ToString("0.0")).Append(" ms");
        sb.Append('\n');
        if (batches.Valid) sb.Append("Batches ").Append(batches.LastValue).Append("  SetPass ").Append(setPass.LastValue)
                             .Append("  Sombras ").Append(sombras.LastValue).Append("  Tris ").Append((tris.LastValue / 1000).ToString()).Append("k");
        Resumen = sb.ToString();
    }

    static double Promedio(ProfilerRecorder r)
    {
        int n = r.Capacity;
        double s = 0; int k = 0;
        for (int i = 0; i < n && i < r.Count; i++) { s += r.GetSample(i).Value; k++; }
        return k > 0 ? s / k : 0;
    }

    void AlEstado(EstadoPartida e)
    {
        if (e == EstadoPartida.Terminada && guardarInforme) Guardar();
    }

    public void Guardar()
    {
        if (cuadros.Count < 10) return;
        var orden = new List<float>(cuadros);
        orden.Sort();
        float total = 0f;
        int bajo60 = 0;
        foreach (var c in cuadros) { total += c; if (c > 1000f / 60f) bajo60++; }
        float prom = total / cuadros.Count;
        float p99 = orden[Mathf.Min(orden.Count - 1, Mathf.FloorToInt(orden.Count * 0.99f))];
        float p95 = orden[Mathf.Min(orden.Count - 1, Mathf.FloorToInt(orden.Count * 0.95f))];

        var t = new StringBuilder();
        t.AppendLine("¡No te me colés! — informe de rendimiento");
        t.AppendLine($"Fecha: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        t.AppendLine($"Escena: {gameObject.scene.name}   Editor: {Application.isEditor}   Unity {Application.unityVersion}");
        t.AppendLine($"Resolución: {Screen.width}x{Screen.height}   Calidad: {QualitySettings.names[QualitySettings.GetQualityLevel()]}   VSync: {QualitySettings.vSyncCount}");
        t.AppendLine($"GPU: {SystemInfo.graphicsDeviceName} ({SystemInfo.graphicsDeviceType})   CPU: {SystemInfo.processorType}");
        t.AppendLine($"Render pipeline: {(GraphicsSettings.currentRenderPipeline != null ? GraphicsSettings.currentRenderPipeline.name : "Built-in")}");
        t.AppendLine();
        t.AppendLine($"Cuadros medidos: {cuadros.Count}   Tiempo: {total / 1000f:0.0} s");
        t.AppendLine($"FPS promedio: {1000f / prom:0.0}   ({prom:0.00} ms)");
        t.AppendLine($"FPS 1 % más bajo: {1000f / p99:0.0}   ({p99:0.00} ms)");
        t.AppendLine($"FPS 5 % más bajo: {1000f / p95:0.0}   ({p95:0.00} ms)");
        t.AppendLine($"Peor cuadro: {orden[orden.Count - 1]:0.0} ms");
        t.AppendLine($"Cuadros por debajo de 60 FPS: {100f * bajo60 / cuadros.Count:0.0} %");
        t.AppendLine($"Máximo de colados a la vez: {maxColados}");
        t.AppendLine($"Último muestreo: {Resumen.Replace("<b>", "").Replace("</b>", "").Replace("\n", " | ")}");
        string ruta = Carpeta();
        try
        {
            Directory.CreateDirectory(ruta);
            string archivo = Path.Combine(ruta, $"rendimiento_{System.DateTime.Now:yyyyMMdd_HHmmss}.txt");
            File.WriteAllText(archivo, t.ToString(), Encoding.UTF8);
            Debug.Log($"[Héctor] Rendimiento: {1000f / prom:0} FPS promedio, {1000f / p99:0} FPS en el 1 % más bajo. Informe: {archivo}");
        }
        catch (System.Exception ex) { Debug.LogWarning("[Héctor] No pude guardar el informe de rendimiento: " + ex.Message); }
    }

    // En el editor: la carpeta Logs del proyecto (fuera de Assets, ignorada por Git).
    // En una build: Logs junto al .exe; si no se puede escribir ahí, persistentDataPath.
    public static string Carpeta()
    {
        string junto = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs"));
        if (Application.isEditor) return junto;
        try
        {
            Directory.CreateDirectory(junto);
            string prueba = Path.Combine(junto, ".escritura");
            File.WriteAllText(prueba, "");
            File.Delete(prueba);
            return junto;
        }
        catch { return Application.persistentDataPath; }
    }
}
