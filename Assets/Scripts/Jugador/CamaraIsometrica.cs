using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Cámara isométrica que sigue al jugador y "vuelve invisible" el edificio que lo tapa:
//  1. Lo que se cruza entre la cámara y el jugador (SphereCast hacia la cámara).
//  2. Techos y pisos de arriba que quedan sobre el jugador (cuando está bajo techo).
// Las piezas ocultas siguen teniendo colisión; solo dejan de dibujarse.
[RequireComponent(typeof(Camera))]
public class CamaraIsometrica : MonoBehaviour
{
    [Header("Seguimiento")]
    public Transform objetivo;
    [Tooltip("Isométrica clásica: 30° de inclinación y 45° de giro.")]
    [Range(10f, 90f)] public float inclinacion = 35f;
    public float giro = 45f;
    [Tooltip("Tamaño ortográfico: cuánto mundo se ve (más = más lejos).")]
    public float tamano = 10f;
    [Tooltip("Distancia de la cámara al jugador (solo afecta el recorte, no el zoom).")]
    public float distancia = 60f;
    public float suavizado = 0.12f;

    [Header("Edificio invisible")]
    [Tooltip("Raíz del edificio (si queda vacío se busca 'Edificio_Univalle').")]
    public Transform edificio;
    [Tooltip("Radio del tubo entre cámara y jugador que se vuelve invisible.")]
    public float radioVision = 0.9f;
    [Tooltip("Lo que esté más arriba de esta altura sobre los pies del jugador y cerca de él se oculta.")]
    public float alturaCorte = 2.6f;
    [Tooltip("Radio alrededor del jugador donde se ocultan techos y pisos de arriba.")]
    public float radioCorte = 14f;
    [Tooltip("Si está activo las piezas ocultas siguen proyectando sombra.")]
    public bool conservarSombras = false;
    public float intervalo = 0.08f;

    readonly List<Renderer> piezas = new();
    readonly HashSet<Renderer> ocultas = new();
    readonly HashSet<Renderer> nuevas = new();
    readonly Dictionary<Renderer, ShadowCastingMode> sombraOriginal = new();
    Vector3 vel;
    float siguienteRevision;

    void Start() => Inicializar();

    // Público para poder previsualizar desde el editor (captura de la escena).
    public void Inicializar()
    {
        piezas.Clear();
        if (objetivo == null)
        {
            var j = FindAnyObjectByType<JugadorIsometrico>();
            if (j != null) objetivo = j.transform;
        }
        if (edificio == null)
        {
            var e = GameObject.Find("Edificio_Univalle");
            if (e != null) edificio = e.transform;
        }
        if (edificio != null) piezas.AddRange(edificio.GetComponentsInChildren<Renderer>());

        var cam = GetComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = tamano;
        ConfigurarCalidad(cam);
        Colocar(true);
    }

    public void ActualizarAhora()
    {
        Colocar(true);
        ActualizarOcultas();
    }

    void LateUpdate()
    {
        Colocar(false);
        if (Time.time >= siguienteRevision)
        {
            siguienteRevision = Time.time + intervalo;
            ActualizarOcultas();
        }
    }

    void Colocar(bool inmediato)
    {
        if (objetivo == null) return;
        Quaternion rot = Quaternion.Euler(inclinacion, giro, 0f);
        Vector3 pos = objetivo.position + Vector3.up - rot * Vector3.forward * distancia;
        transform.SetPositionAndRotation(
            inmediato ? pos : Vector3.SmoothDamp(transform.position, pos, ref vel, suavizado), rot);
        GetComponent<Camera>().orthographicSize = tamano;
    }

    void ActualizarOcultas()
    {
        if (objetivo == null || piezas.Count == 0) return;
        nuevas.Clear();
        Vector3 pies = objetivo.position;

        // 1. Todo lo que tapa la línea de visión (cuerpo y cabeza del jugador).
        Vector3 haciaCamara = -transform.forward;
        int mascara = ~(Capas.MascaraJugador | Capas.MascaraEstudiante);
        foreach (var altura in new[] { 1.0f, 1.8f })
        {
            var hits = Physics.SphereCastAll(pies + Vector3.up * altura, radioVision, haciaCamara, distancia, mascara, QueryTriggerInteraction.Ignore);
            foreach (var h in hits)
            {
                if (h.distance <= 0f) continue;                    // ya tocaba al empezar (piso bajo los pies)
                var r = h.collider.GetComponent<Renderer>();
                if (r == null || !r.transform.IsChildOf(edificio)) continue;
                if (r.bounds.max.y < pies.y + 0.3f) continue;      // pisos y andenes: nunca se ocultan
                nuevas.Add(r);
            }
        }

        // 2. Techos y pisos superiores cercanos.
        float corteY = pies.y + alturaCorte;
        float r2 = radioCorte * radioCorte;
        foreach (var r in piezas)
        {
            if (r == null) continue;
            Bounds b = r.bounds;
            if (b.min.y < corteY) continue;
            float dx = Mathf.Max(b.min.x - pies.x, 0f, pies.x - b.max.x);
            float dz = Mathf.Max(b.min.z - pies.z, 0f, pies.z - b.max.z);
            if (dx * dx + dz * dz <= r2) nuevas.Add(r);
        }

        foreach (var r in ocultas)
            if (r != null && !nuevas.Contains(r)) Mostrar(r);
        foreach (var r in nuevas)
            if (!ocultas.Contains(r)) Ocultar(r);

        ocultas.Clear();
        ocultas.UnionWith(nuevas);
    }

    void Ocultar(Renderer r)
    {
        if (conservarSombras)
        {
            sombraOriginal[r] = r.shadowCastingMode;
            r.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
        }
        else r.forceRenderingOff = true;
    }

    void Mostrar(Renderer r)
    {
        if (sombraOriginal.TryGetValue(r, out var modo))
        {
            r.shadowCastingMode = modo;
            sombraOriginal.Remove(r);
        }
        r.forceRenderingOff = false;
    }

    void OnDisable()
    {
        foreach (var r in ocultas) if (r != null) Mostrar(r);
        ocultas.Clear();
    }

    static void ConfigurarCalidad(Camera cam)
    {
        var datos = cam.GetUniversalAdditionalCameraData();
        datos.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        datos.antialiasingQuality = AntialiasingQuality.High;
        datos.renderPostProcessing = true;
        cam.allowMSAA = true;
    }
}
