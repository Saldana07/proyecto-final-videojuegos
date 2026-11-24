using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public enum EstadoPartida { Intro, Jugando, Pausa, Terminada }

// Tipos de lo que pasa en la partida, para el HUD (avisos) y el audio.
public enum EventoPartida { Expulsado, Desistido, Infiltrado, FalsoPositivo, SobreAsegurado, CambioFase, Timbre, Derrota }

// Lo que pasó en una partida: alimenta el HUD y la pantalla de resultados (GDD 10.1).
[Serializable]
public class EstadisticasPartida
{
    public int expulsados, desistidos;
    public int infiltradosDelante, infiltradosDetras;
    public int falsosPositivos, sobreAsegurados;
    public int frustradosDelante;        // frustrados que iban a un hueco de la zona caliente
    public int frustradosSinPuesto;      // frustrados que aún no habían escogido hueco
    public int dignidad;
    public readonly Dictionary<string, int> infiltradosPorFacultad = new Dictionary<string, int>();

    public int Frustrados => expulsados + desistidos;
    public int Infiltrados => infiltradosDelante + infiltradosDetras;
}

// El GameManager de la partida (GDD 3, 5, 6 y 10). Dueño: Héctor.
// Lleva el reloj de 10:00 a 12:00, la Paciencia de la fila, el Escándalo, la Dignidad y las
// estadísticas; escucha los eventos del Estudiante (Saldaña) y termina la partida con el timbre
// o con la derrota (Paciencia en 0). El HUD y el audio solo leen esto y escuchan sus eventos.
[DefaultExecutionOrder(-200)]
public class PartidaManager : MonoBehaviour
{
    public static PartidaManager Instancia { get; private set; }

    public PartidaConfig config;
    [Tooltip("Si queda vacío se busca en la escena.")]
    public Fila fila;
    [Tooltip("Si queda vacío se busca en la escena.")]
    public SpawnerEstudiantes spawner;
    [Tooltip("Empieza jugando sin la pantalla de inicio (útil para probar).")]
    public bool saltarIntro;

    // Modo prueba de rendimiento (solo builds): si junto al .exe existe "prueba_rendimiento.txt",
    // la partida arranca sola, corre los 240 s, guarda el informe en Logs/ y el juego se cierra.
    public bool ModoPruebaRendimiento { get; private set; }

    public EstadoPartida Estado { get; private set; } = EstadoPartida.Intro;
    public float Segundos { get; private set; }
    public float Paciencia { get; private set; }
    public float Escandalo { get; private set; }
    public bool Derrota { get; private set; }
    public string FaseActual { get; private set; } = "";
    public EstadisticasPartida Stats { get; private set; } = new EstadisticasPartida();
    public ResultadoPartida Resultado { get; private set; }
    public float Velocidad { get; private set; } = 1f;   // F4 en depuración

    public PartidaConfig Cfg => config != null ? config : (configPorDefecto ??= ScriptableObject.CreateInstance<PartidaConfig>());
    public float Progreso => Mathf.Clamp01(Segundos / Mathf.Max(1f, Cfg.duracion));
    public float MinutoDeJuego => Puntaje.MinutoDeJuego(Segundos, Cfg.duracion, Cfg.minutoInicio, Cfg.minutoFin);
    public string HoraTexto => Puntaje.Hora(MinutoDeJuego);
    // Puesto del protagonista en la fila real (#40 del GDD con la fila de 20 de la escena).
    public int PuestoJugador => (fila != null ? fila.puestoJugador + 1 : 11) + Cfg.personasMasAdelante;

    public event Action<EstadoPartida> EstadoCambiado;
    public event Action<EventoPartida, Estudiante, string> Evento;   // texto corto para el aviso
    public event Action EstadisticasCambiadas;

    PartidaConfig configPorDefecto;
    JugadorIsometrico jugador;
    CinturonHerramientas cinturon;

    void Awake() => Inicializar();

    // Público para poder inicializarlo fuera de Play (pruebas).
    public void Inicializar()
    {
        Instancia = this;
        if (fila == null) fila = FindAnyObjectByType<Fila>();
        if (spawner == null) spawner = FindAnyObjectByType<SpawnerEstudiantes>();
        jugador = FindAnyObjectByType<JugadorIsometrico>();
        cinturon = FindAnyObjectByType<CinturonHerramientas>();
        Paciencia = Cfg.pacienciaInicial;
        if (!Application.isEditor && System.IO.File.Exists(System.IO.Path.Combine(Application.dataPath, "..", "prueba_rendimiento.txt")))
        {
            ModoPruebaRendimiento = true;
            saltarIntro = true;
            Application.runInBackground = true;   // que no se detenga si pierde el foco
        }
        // El spawner de Saldaña usa este reloj: así se detiene en la intro, la pausa y el final.
        SpawnerEstudiantes.RelojExterno = () => Segundos;
        Time.timeScale = 0f;
        PermitirControl(false);
    }

    void OnEnable()
    {
        Estudiante.OnExpulsado += AlExpulsado;
        Estudiante.OnDesistido += AlDesistido;
        Estudiante.OnAsegurado += AlAsegurado;
        Estudiante.OnFalsoPositivo += AlFalsoPositivo;
        Estudiante.OnActuoSobreAsegurado += AlSobreAsegurado;
        SpawnerEstudiantes.OnCambioFase += AlCambioFase;
    }

    void OnDisable()
    {
        Estudiante.OnExpulsado -= AlExpulsado;
        Estudiante.OnDesistido -= AlDesistido;
        Estudiante.OnAsegurado -= AlAsegurado;
        Estudiante.OnFalsoPositivo -= AlFalsoPositivo;
        Estudiante.OnActuoSobreAsegurado -= AlSobreAsegurado;
        SpawnerEstudiantes.OnCambioFase -= AlCambioFase;
    }

    void OnDestroy()
    {
        if (Instancia == this) Instancia = null;
        SpawnerEstudiantes.RelojExterno = null;
        Time.timeScale = 1f;
    }

    void Start()
    {
        if (saltarIntro) Empezar();
    }

    // =============================== Flujo ===============================

    void Update()
    {
        var kb = Keyboard.current;
        switch (Estado)
        {
            case EstadoPartida.Intro:
                if (Pulso(kb?.enterKey) || Pulso(kb?.spaceKey) || Pulso(kb?.numpadEnterKey) ||
                    (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame))
                    Empezar();
                break;

            case EstadoPartida.Jugando:
                if (Pulso(kb?.escapeKey) || Pulso(kb?.pKey)) { Pausar(true); break; }
                Depuracion(kb);
                Segundos += Time.deltaTime;
                if (Segundos >= Cfg.duracion) { Segundos = Cfg.duracion; Terminar(false); }
                break;

            case EstadoPartida.Pausa:
                if (Pulso(kb?.escapeKey) || Pulso(kb?.pKey) || Pulso(kb?.enterKey)) Pausar(false);
                else if (Pulso(kb?.rKey)) Reiniciar();
                break;

            case EstadoPartida.Terminada:
                if (Pulso(kb?.rKey) || Pulso(kb?.enterKey)) Reiniciar();
                break;
        }
    }

    static bool Pulso(UnityEngine.InputSystem.Controls.KeyControl k) => k != null && k.wasPressedThisFrame;

    public void Empezar()
    {
        if (Estado != EstadoPartida.Intro) return;
        CambiarEstado(EstadoPartida.Jugando);
        Time.timeScale = Velocidad;
        // Un cuadro después: el mismo clic que empieza la partida no debe disparar una herramienta.
        if (Application.isPlaying) StartCoroutine(PermitirControlLuego());
        else PermitirControl(true);
    }

    IEnumerator PermitirControlLuego()
    {
        yield return null;
        if (Estado == EstadoPartida.Jugando) PermitirControl(true);
    }

    public void Pausar(bool pausar)
    {
        if (pausar && Estado == EstadoPartida.Jugando)
        {
            CambiarEstado(EstadoPartida.Pausa);
            Time.timeScale = 0f;
            PermitirControl(false);
        }
        else if (!pausar && Estado == EstadoPartida.Pausa)
        {
            CambiarEstado(EstadoPartida.Jugando);
            Time.timeScale = Velocidad;
            if (Application.isPlaying) StartCoroutine(PermitirControlLuego());
            else PermitirControl(true);
        }
    }

    public void Reiniciar()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameObject.scene.buildIndex >= 0 ? gameObject.scene.buildIndex : 0);
    }

    public void Salir()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // El timbre de las 12:00 (derrota = false) o la fila que se harta (derrota = true).
    public void Terminar(bool derrota)
    {
        if (Estado == EstadoPartida.Terminada) return;
        Derrota = derrota;
        Time.timeScale = 0f;     // GDD 10: se congela la acción
        PermitirControl(false);
        Resultado = ResultadoPartida.Calcular(Stats, Cfg, PuestoJugador, derrota, fila);
        Stats.dignidad += Resultado.dignidadBonus;
        Evento?.Invoke(derrota ? EventoPartida.Derrota : EventoPartida.Timbre, null, derrota ? "¡La fila se hartó!" : "¡Sonó el timbre!");
        CambiarEstado(EstadoPartida.Terminada);
        EstadisticasCambiadas?.Invoke();
        if (ModoPruebaRendimiento && Application.isPlaying) StartCoroutine(CerrarLuego());
    }

    IEnumerator CerrarLuego()
    {
        yield return new WaitForSecondsRealtime(4f);
        Application.Quit();
    }

    void CambiarEstado(EstadoPartida nuevo)
    {
        Estado = nuevo;
        EstadoCambiado?.Invoke(nuevo);
    }

    void PermitirControl(bool si)
    {
        if (jugador != null) jugador.enabled = si;
        if (cinturon != null) cinturon.enabled = si;
    }

    void Depuracion(Keyboard kb)
    {
        if (kb == null || !Cfg.teclasDeDepuracion || !(Application.isEditor || Debug.isDebugBuild)) return;
        if (kb.f4Key.wasPressedThisFrame)
        {
            Velocidad = Velocidad > 1f ? 1f : 3f;
            Time.timeScale = Velocidad;
        }
        if (kb.f5Key.wasPressedThisFrame) Terminar(false);
        if (kb.f6Key.wasPressedThisFrame) UsarContraElMasCercano(false);
        if (kb.f7Key.wasPressedThisFrame) UsarContraElMasCercano(true);   // contra un inocente: falso positivo
    }

    // F6 (depuración): usa la herramienta actual hacia el colado vulnerable más cercano,
    // para probar herramientas, Vergüenza y expulsiones sin depender de la puntería.
    void UsarContraElMasCercano(bool inocente)
    {
        if (cinturon == null || cinturon.Actual == null || jugador == null) return;
        Estudiante mejor = null;
        float mejorD = float.MaxValue;
        foreach (var e in FindObjectsByType<Estudiante>())
        {
            if (e.EsInocente != inocente || e.Estado == EstadoEstudiante.Expulsado || e.Estado == EstadoEstudiante.Desistido) continue;
            if (!inocente && e.Estado == EstadoEstudiante.Asegurado) continue;
            float d = (e.transform.position - jugador.transform.position).sqrMagnitude;
            if (d < mejorD) { mejorD = d; mejor = e; }
        }
        if (mejor == null) { Debug.Log("[Héctor] F6/F7: no hay a quién apuntar."); return; }
        float alcance = cinturon.Actual.datos != null ? cinturon.Actual.datos.alcance : 3f;
        if (Mathf.Sqrt(mejorD) > alcance + 0.3f)
        {
            Debug.Log($"[Héctor] {(inocente ? "F7" : "F6")}: {mejor.name} está a {Mathf.Sqrt(mejorD):0.0} m (alcance {alcance:0.#} m): no se usa.");
            return;
        }
        Vector3 origen = jugador.OrigenHerramientas;
        Vector3 dir = mejor.transform.position + Vector3.up - origen;
        dir.y = 0f;
        bool ok = cinturon.Actual.IntentarUsar(origen, dir.normalized, cinturon.gameObject);
        Debug.Log($"[Héctor] {(inocente ? "F7" : "F6")}: {cinturon.Actual.Tipo} hacia {mejor.name} ({(mejor.facultad != null ? mejor.facultad.nombre : "?")}, " +
                  $"{mejor.Estado}, a {Mathf.Sqrt(mejorD):0.0} m) → {(ok ? "usada" : "no lista")}; ahora {mejor.Estado}, Vergüenza {mejor.Verguenza:0}");
    }

    // =============================== Eventos del Estudiante (contrato 14.4) ===============================

    bool Cuenta => Estado == EstadoPartida.Jugando;

    // Públicos para poder probarlos sin escena.
    public void AlExpulsado(Estudiante e)
    {
        if (!Cuenta) return;
        Stats.expulsados++;
        Stats.dignidad += Cfg.dignidadExpulsado;
        ContarFrustrado(e);
        string texto = $"¡Expulsado! +{Cfg.dignidadExpulsado}";
        if (e != null && e.EnZonaCaliente)
        {
            Paciencia = Mathf.Min(Cfg.pacienciaInicial, Paciencia + Cfg.bonoExpulsadoZonaCaliente);
            texto += "  Paciencia +" + Cfg.bonoExpulsadoZonaCaliente.ToString("0");
        }
        Avisar(EventoPartida.Expulsado, e, texto);
    }

    public void AlDesistido(Estudiante e)
    {
        if (!Cuenta) return;
        Stats.desistidos++;
        Stats.dignidad += Cfg.dignidadDesistido;
        ContarFrustrado(e);
        Avisar(EventoPartida.Desistido, e, $"Desistió +{Cfg.dignidadDesistido}");
    }

    public void AlAsegurado(Estudiante e)
    {
        if (!Cuenta) return;
        bool delante = e != null && e.EnZonaCaliente;
        if (delante)
        {
            Stats.infiltradosDelante++;
            Escandalo = Mathf.Min(100f, Escandalo + Cfg.escandaloPorInfiltrado);
        }
        else Stats.infiltradosDetras++;
        string fac = e != null && e.facultad != null ? e.facultad.nombre : "Sin facultad";
        Stats.infiltradosPorFacultad.TryGetValue(fac, out int n);
        Stats.infiltradosPorFacultad[fac] = n + 1;
        Avisar(EventoPartida.Infiltrado, e, delante ? "¡Se coló delante de ti!" : "Se coló detrás de ti");
    }

    public void AlFalsoPositivo(Estudiante e)
    {
        if (!Cuenta) return;
        Stats.falsosPositivos++;
        CambiarPaciencia(-Cfg.penalFalsoPositivo);
        Avisar(EventoPartida.FalsoPositivo, e, $"¡Era inocente! Paciencia −{Cfg.penalFalsoPositivo:0}");
    }

    public void AlSobreAsegurado(Estudiante e)
    {
        if (!Cuenta) return;
        Stats.sobreAsegurados++;
        CambiarPaciencia(-Cfg.penalSobreAsegurado);
        Avisar(EventoPartida.SobreAsegurado, e, $"Ya está adentro. Paciencia −{Cfg.penalSobreAsegurado:0}");
    }

    void AlCambioFase(FaseSpawn fase)
    {
        if (fase == null || fase.nombre == FaseActual) return;
        bool primera = string.IsNullOrEmpty(FaseActual);
        FaseActual = fase.nombre;
        if (!primera) Evento?.Invoke(EventoPartida.CambioFase, null, fase.nombre);
        EstadisticasCambiadas?.Invoke();
    }

    void ContarFrustrado(Estudiante e)
    {
        if (e == null || e.Puesto < 0) Stats.frustradosSinPuesto++;
        else if (e.EnZonaCaliente) Stats.frustradosDelante++;
    }

    void CambiarPaciencia(float delta)
    {
        Paciencia = Mathf.Clamp(Paciencia + delta, 0f, Cfg.pacienciaInicial);
        if (Paciencia <= 0f && Cuenta)
        {
            if (Application.isPlaying) StartCoroutine(TerminarAlFinalDelCuadro());
            else Terminar(true);
        }
    }

    // Se termina después de avisar del falso positivo, para que el HUD y el audio lo muestren.
    IEnumerator TerminarAlFinalDelCuadro()
    {
        yield return new WaitForEndOfFrame();
        Terminar(true);
    }

    void Avisar(EventoPartida tipo, Estudiante e, string texto)
    {
        Evento?.Invoke(tipo, e, texto);
        EstadisticasCambiadas?.Invoke();
    }
}

// Lo que muestra la pantalla de resultados (GDD 10).
public class ResultadoPartida
{
    public float efectividad;
    public string rango;
    public int dignidadBonus;
    public float minutosAlmuerzo;      // después de las 12:00
    public float minutosSinTi;         // si nadie hubiera defendido la fila
    public string facultadMasDificil;  // la que más infiltrados logró

    public static ResultadoPartida Calcular(EstadisticasPartida s, PartidaConfig c, int puesto, bool derrota, Fila fila)
    {
        var r = new ResultadoPartida();
        r.efectividad = Puntaje.Efectividad(s.Frustrados, s.Infiltrados, s.falsosPositivos, c.penalEfectividadPorFalso);
        r.rango = Puntaje.Rango(r.efectividad, s.falsosPositivos, derrota);
        r.dignidadBonus = Puntaje.DignidadBonus(r.rango);
        r.minutosAlmuerzo = Puntaje.MinutosAlmuerzo(puesto, s.infiltradosDelante, c.minutosPorPersona);

        // Sin ti: también habrían entrado delante los frustrados que iban a la zona caliente,
        // y la parte proporcional de los que aún no habían escogido hueco.
        float proporcion = fila != null && fila.Cantidad > 0 ? (float)fila.puestoJugador / fila.Cantidad : 0.5f;
        int extra = s.frustradosDelante + Mathf.RoundToInt(s.frustradosSinPuesto * proporcion);
        r.minutosSinTi = Puntaje.MinutosAlmuerzo(puesto, s.infiltradosDelante + extra, c.minutosPorPersona);

        int max = 0;
        foreach (var kv in s.infiltradosPorFacultad)
            if (kv.Value > max) { max = kv.Value; r.facultadMasDificil = kv.Key; }
        return r;
    }
}
