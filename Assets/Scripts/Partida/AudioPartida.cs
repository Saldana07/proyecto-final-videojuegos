using System.Collections.Generic;
using UnityEngine;

// Sonidos de la partida (GDD 12.4). Dueño: Héctor.
// Todo en 2D: con la cámara isométrica a 35 m del jugador, un sonido 3D casi no se oye.
// Las herramientas de Carlos avisan con eventos (Usada, SinMunicion, MunicionCambiada) y aquí
// suena el clip según el tipo, así no hace falta tocar los HerramientaData.
[RequireComponent(typeof(AudioSource))]
public class AudioPartida : MonoBehaviour
{
    public PartidaManager partida;
    public CinturonHerramientas cinturon;

    [Header("Clips (Assets/Audio)")]
    public AudioClip timbre;
    public AudioClip agua;
    public AudioClip megafono;
    public AudioClip relleno;
    public AudioClip vacio;
    public AudioClip expulsado;
    public AudioClip falsoPositivo;
    public AudioClip colado;
    public AudioClip clickUI;

    [Range(0f, 1f)] public float volumen = 0.9f;

    AudioSource fuente;
    readonly Dictionary<Herramienta, int> municionAnterior = new Dictionary<Herramienta, int>();
    readonly List<Herramienta> suscritas = new List<Herramienta>();

    void Awake()
    {
        if (partida == null) partida = FindAnyObjectByType<PartidaManager>();
        if (cinturon == null) cinturon = FindAnyObjectByType<CinturonHerramientas>();
        fuente = GetComponent<AudioSource>();
        fuente.playOnAwake = false;
        fuente.spatialBlend = 0f;
        AsegurarListener();
    }

    static void AsegurarListener()
    {
        foreach (var l in FindObjectsByType<AudioListener>())
            if (l.isActiveAndEnabled) return;
        var cam = Camera.main;
        if (cam != null) cam.gameObject.AddComponent<AudioListener>();
    }

    void Start()
    {
        if (cinturon == null) return;
        foreach (var h in cinturon.Herramientas)
        {
            if (h == null) continue;
            h.Usada += AlUsar;
            h.SinMunicion += AlVacio;
            h.MunicionCambiada += AlCambiarMunicion;
            municionAnterior[h] = h.Municion;
            suscritas.Add(h);
        }
    }

    void OnEnable()
    {
        if (partida != null) partida.Evento += AlEvento;
    }

    void OnDisable()
    {
        if (partida != null) partida.Evento -= AlEvento;
    }

    void OnDestroy()
    {
        foreach (var h in suscritas)
        {
            if (h == null) continue;
            h.Usada -= AlUsar;
            h.SinMunicion -= AlVacio;
            h.MunicionCambiada -= AlCambiarMunicion;
        }
    }

    public void Sonar(AudioClip clip, float vol = 1f, float tono = 1f)
    {
        if (clip == null || fuente == null) return;
        fuente.pitch = tono;
        fuente.PlayOneShot(clip, vol * volumen);
    }

    void AlUsar(Herramienta h)
    {
        var clip = h.Tipo == TipoHerramienta.Agua ? agua : h.Tipo == TipoHerramienta.Megafono ? megafono : clickUI;
        Sonar(clip, 1f, Random.Range(0.95f, 1.05f));
    }

    void AlVacio(Herramienta h) => Sonar(vacio, 0.8f);

    void AlCambiarMunicion(Herramienta h)
    {
        municionAnterior.TryGetValue(h, out int antes);
        if (h.Municion > antes) Sonar(relleno, 0.7f, 0.9f + 0.05f * h.Municion);
        municionAnterior[h] = h.Municion;
    }

    void AlEvento(EventoPartida tipo, Estudiante e, string texto)
    {
        switch (tipo)
        {
            case EventoPartida.Expulsado: Sonar(expulsado, 0.8f, Random.Range(0.95f, 1.1f)); break;
            case EventoPartida.Desistido: Sonar(expulsado, 0.45f, 1.3f); break;
            case EventoPartida.Infiltrado: Sonar(colado, e != null && e.EnZonaCaliente ? 1f : 0.6f); break;
            case EventoPartida.FalsoPositivo: Sonar(falsoPositivo, 0.9f); break;
            case EventoPartida.SobreAsegurado: Sonar(falsoPositivo, 0.5f, 1.3f); break;
            case EventoPartida.CambioFase: Sonar(clickUI, 1f, 0.8f); break;
            case EventoPartida.Timbre: Sonar(timbre, 1f); break;
            case EventoPartida.Derrota: Sonar(falsoPositivo, 1f, 0.7f); break;
        }
    }
}
