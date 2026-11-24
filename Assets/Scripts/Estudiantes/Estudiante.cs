using System.Collections;
using UnityEngine;
using UnityEngine.AI;

// Estudiante que llega a la fila (GDD 4.2). Dueño: Saldaña.
// Máquina de estados: Merodeo → Aproximación → Intento → Asegurado / Expulsado / Desistido.
// Recibe las herramientas de Carlos (IReceptorHerramienta) y avisa a Héctor con eventos.
// Regla de oro: un colado solo es vulnerable en Merodeo, Aproximación o Intento;
// cuando llega a Asegurado es intocable.
[RequireComponent(typeof(NavMeshAgent))]
public class Estudiante : MonoBehaviour, IReceptorHerramienta
{
    // ---------------- Contrato 14.4: los dispara Saldaña, los escucha Héctor ----------------
    public static event System.Action<Estudiante> OnExpulsado;            // colado expulsado (+10 Dignidad; +2 Paciencia si EnZonaCaliente)
    public static event System.Action<Estudiante> OnAsegurado;            // colado infiltrado (nunca se dispara por inocentes)
    public static event System.Action<Estudiante> OnDesistido;            // colado que se rindió (+5 Dignidad)
    public static event System.Action<Estudiante> OnFalsoPositivo;        // herramienta usada sobre un inocente (−15 Paciencia)
    public static event System.Action<Estudiante> OnActuoSobreAsegurado;  // herramienta sobre un colado ya Asegurado (−5 Paciencia)
    public static event System.Action<Estudiante, EstadoEstudiante> OnCambioEstado; // telemetría / depuración

    [Header("Datos")]
    public FacultadData facultad;
    public ConfigEstudiantes config;
    public TipoInocente tipoInocente = TipoInocente.Ninguno;

    [Header("Partes del prefab")]
    public Renderer cuerpo;
    public Renderer anillo;
    public Transform puntoTell;
    public TextMesh etiqueta;

    public bool EsInocente => tipoInocente != TipoInocente.Ninguno;
    public EstadoEstudiante Estado { get; private set; } = EstadoEstudiante.Merodeo;
    public float Verguenza { get; private set; }         // 0–100: la barra que dibuja Héctor
    public float ProgresoIntento { get; private set; }   // 0–1 mientras se mete al hueco
    public int Puesto { get; private set; } = -1;        // puesto reservado u ocupado en la fila
    public bool EnZonaCaliente => Fila.Instancia != null && Puesto >= 0 && Fila.Instancia.EsZonaCaliente(Puesto);
    public float MultiplicadorVentana { get; set; } = 1f; // Estampida: 0,7 (ventanas 30 % más cortas)

    static readonly int IdBaseColor = Shader.PropertyToID("_BaseColor");
    static readonly int IdColor = Shader.PropertyToID("_Color");
    static ConfigEstudiantes configPorDefecto;
    static Transform jugador;

    NavMeshAgent agente;
    NavMeshObstacle obstaculo;
    Collider colision;
    GameObject tell;
    MaterialPropertyBlock bloque;
    Color colorCuerpo;
    float destello;
    float reloj;            // segundos en el estado actual (sin contar los frenados)
    float esperaMerodeo;
    float frenadoHasta;
    bool llegoAlMerodeo;
    bool entrando;
    Vector3 inicioIntento;
    Vector3 salida;
    // Optimización (Héctor): solo se toca el material y la etiqueta cuando algo cambia.
    Color colorPintado = new Color(-1f, -1f, -1f, -1f);
    int claveEtiqueta = int.MinValue;

    ConfigEstudiantes Cfg
    {
        get
        {
            if (config != null) return config;
            if (configPorDefecto == null) configPorDefecto = ScriptableObject.CreateInstance<ConfigEstudiantes>();
            return configPorDefecto;
        }
    }

    void Awake()
    {
        agente = GetComponent<NavMeshAgent>();
        obstaculo = GetComponent<NavMeshObstacle>();
        colision = GetComponent<Collider>();
        bloque = new MaterialPropertyBlock();
        // Todos con colores apagados: la facultad se reconoce por el tell, no por el color (GDD 7.3 y 12.4).
        colorCuerpo = Color.HSVToRGB(Random.value, 0.2f, Random.Range(0.55f, 0.8f));
        if (anillo != null) anillo.enabled = false;
    }

    void OnDestroy()
    {
        if (Fila.Instancia != null) Fila.Instancia.Liberar(this);
    }

    // =============================== Cómo entra al juego ===============================

    // Lo llama el Spawner: un colado (inocente = Ninguno) o un inocente que llega caminando.
    public void Llegar(Vector3 desde, FacultadData fac, TipoInocente inocente, Vector3 puntoSalida)
    {
        facultad = fac;
        tipoInocente = inocente;
        salida = puntoSalida;
        CrearTell();
        PrepararAgente(desde);
        agente.speed = EsInocente || facultad == null ? Cfg.velocidadInocente : facultad.velocidad;

        switch (tipoInocente)
        {
            case TipoInocente.Ninguno:
                EntrarMerodeo();
                break;
            case TipoInocente.PasaDeLargo:
                CambiarEstado(EstadoEstudiante.Merodeo);
                IrA(salida);
                break;
            default:
                IrAlFinalDeLaFila();
                break;
        }
    }

    // Lo llama la Fila al empezar: un inocente que ya estaba haciendo fila.
    public void ColocarEnFila(int puesto)
    {
        if (tipoInocente == TipoInocente.Ninguno) tipoInocente = TipoInocente.Legal;
        facultad = null;
        Puesto = puesto;
        transform.position = Fila.Instancia.Posicion(puesto);
        Asegurar();
    }

    // La Fila lo corre hacia adelante cuando se compacta.
    public void MoverAPuesto(int puesto)
    {
        Puesto = puesto;
        StartCoroutine(Deslizar(Fila.Instancia.Posicion(puesto), 0.6f, false));
    }

    // Hueco temporal (GDD 4.1): va al baño y vuelve a SU puesto. Al volver parece un colado,
    // pero es inocente (GDD 4.3): atacarlo es falso positivo.
    public void IrAlBano(Vector3 puntoBano, float segundos)
    {
        if (Estado != EstadoEstudiante.Asegurado) return;
        StartCoroutine(RutinaBano(puntoBano, segundos));
    }

    // Se cansó y se va para siempre: deja un hueco que luego se compacta.
    public void Abandonar()
    {
        if (Estado != EstadoEstudiante.Asegurado || Fila.Instancia == null) return;
        Vector3 desde = Fila.Instancia.Posicion(Puesto) - Fila.Instancia.LadoPlazoleta;
        CambiarEstado(EstadoEstudiante.Desistido); // sin evento: es un inocente, no cuenta para Héctor
        Salir(desde, Cfg.velocidadInocente);
    }

    // =============================== Herramientas de Carlos ===============================

    public void RecibirHerramienta(TipoHerramienta tipo) =>
        RecibirHerramienta(new ImpactoHerramienta { tipo = tipo, origen = transform.position - transform.forward });

    public void RecibirHerramienta(ImpactoHerramienta impacto)
    {
        if (Estado == EstadoEstudiante.Expulsado || Estado == EstadoEstudiante.Desistido) return;
        destello = 0.25f;

        if (EsInocente) { OnFalsoPositivo?.Invoke(this); return; }
        if (Estado == EstadoEstudiante.Asegurado) { OnActuoSobreAsegurado?.Invoke(this); return; }

        // Colado en Merodeo, Aproximación o Intento: aquí sí se puede actuar.
        if (facultad != null && impacto.tipo == facultad.resistencia) return;                  // no le hace nada
        if (facultad != null && impacto.tipo == facultad.counter) { Expulsar(impacto.origen); return; }

        Verguenza = Mathf.Min(100f, Verguenza + impacto.verguenza);
        if (Verguenza >= 100f) { Expulsar(impacto.origen); return; }
        if (impacto.frenarSegundos > 0f) frenadoHasta = Mathf.Max(frenadoHasta, Time.time + impacto.frenarSegundos);
    }

    // =============================== Máquina de estados ===============================

    void Update()
    {
        float dt = Time.deltaTime;
        if (destello > 0f) destello -= dt;
        Pintar();
        ActualizarEtiqueta();

        if (Estado == EstadoEstudiante.Expulsado || Estado == EstadoEstudiante.Desistido) return;

        Verguenza = Mathf.Max(0f, Verguenza - Cfg.decaimientoVerguenza * dt);
        bool frenado = Time.time < frenadoHasta;
        if (agente.enabled && agente.isOnNavMesh) agente.isStopped = frenado;
        if (!frenado) reloj += dt;

        switch (Estado)
        {
            case EstadoEstudiante.Merodeo: ActualizarMerodeo(); break;
            case EstadoEstudiante.Aproximacion: ActualizarAproximacion(); break;
            case EstadoEstudiante.Intento: if (!frenado) ActualizarIntento(dt); break;
        }
    }

    void EntrarMerodeo()
    {
        CambiarEstado(EstadoEstudiante.Merodeo);
        llegoAlMerodeo = false;
        esperaMerodeo = Random.Range(Cfg.merodeoMin, Cfg.merodeoMax);
        IrA(Fila.Instancia != null ? Fila.Instancia.PuntoMerodeo() : transform.position);
    }

    void ActualizarMerodeo()
    {
        if (EsInocente)
        {
            if (tipoInocente == TipoInocente.PasaDeLargo)
            {
                if (Llego()) Destroy(gameObject);
                return;
            }
            // Legal o volviendo del baño: camina a su puesto y entra.
            if (Puesto >= 0 && !entrando && Fila.Instancia != null &&
                (Llego() || DistanciaPlana(transform.position, Fila.Instancia.Posicion(Puesto)) < 0.8f))
            {
                entrando = true;
                StartCoroutine(Deslizar(Fila.Instancia.Posicion(Puesto), 0.4f, true));
            }
            return;
        }

        if (JugadorCerca(Cfg.radioDisuasionMerodeo)) { Desistir(); return; }
        if (!llegoAlMerodeo && Llego()) { llegoAlMerodeo = true; reloj = 0f; }
        if (!llegoAlMerodeo)
        {
            if (reloj > 25f) Desistir(); // se quedó atascado
            return;
        }
        if (reloj < esperaMerodeo) return;

        int p = Fila.Instancia != null ? Fila.Instancia.ReservarHueco(this, transform.position) : -1;
        if (p >= 0)
        {
            Puesto = p;
            EntrarAproximacion();
        }
        else if (reloj > esperaMerodeo + Cfg.esperaMaxSinHueco)
        {
            Desistir();
        }
    }

    void EntrarAproximacion()
    {
        CambiarEstado(EstadoEstudiante.Aproximacion);
        MostrarTell(true);
        IrA(PuntoDeEspera());
    }

    Vector3 PuntoDeEspera() => Fila.Instancia.Posicion(Puesto) + Fila.Instancia.LadoPlazoleta * Cfg.distanciaIntento;

    void ActualizarAproximacion()
    {
        if (JugadorCerca(Cfg.radioDisuasionAproximacion)) { Desistir(); return; }
        bool cerca = Llego() || DistanciaPlana(transform.position, PuntoDeEspera()) < 0.6f;
        if (!cerca)
        {
            if (reloj > 25f) Desistir();
            return;
        }
        if (agente.enabled && agente.hasPath) agente.ResetPath();
        Mirar(Fila.Instancia.Posicion(Puesto));
        if (reloj >= Cfg.tiempoAproximacion * MultiplicadorVentana) EntrarIntento();
    }

    void EntrarIntento()
    {
        CambiarEstado(EstadoEstudiante.Intento);
        ProgresoIntento = 0f;
        inicioIntento = transform.position;
        agente.enabled = false;
    }

    void ActualizarIntento(float dt)
    {
        float duracion = Mathf.Max(0.1f, (facultad != null ? facultad.tiempoIntento : 2f) * MultiplicadorVentana);
        ProgresoIntento = Mathf.Min(1f, ProgresoIntento + dt / duracion);
        Vector3 destino = Fila.Instancia.Posicion(Puesto);
        float s = Mathf.SmoothStep(0f, 1f, ProgresoIntento);
        Vector3 p = Vector3.Lerp(inicioIntento, destino, s);
        // Se mete meneándose, para que se note que se está colando.
        Vector3 lado = Vector3.Cross(Vector3.up, destino - inicioIntento);
        if (lado.sqrMagnitude > 1e-4f) p += lado.normalized * Mathf.Sin(ProgresoIntento * Mathf.PI * 6f) * 0.12f * (1f - s);
        transform.position = p;
        if (ProgresoIntento >= 1f) Asegurar();
    }

    void Asegurar()
    {
        entrando = false;
        CambiarEstado(EstadoEstudiante.Asegurado);
        MostrarTell(false);
        agente.enabled = false;
        if (obstaculo != null) obstaculo.enabled = true;
        transform.position = Fila.Instancia.Posicion(Puesto);
        transform.rotation = Quaternion.LookRotation(-Fila.Instancia.Direccion);
        Fila.Instancia.Ocupar(Puesto, this);
        if (!EsInocente) OnAsegurado?.Invoke(this);
    }

    void Expulsar(Vector3 origen)
    {
        CambiarEstado(EstadoEstudiante.Expulsado);
        OnExpulsado?.Invoke(this); // antes de Salir: Héctor aún puede leer Puesto y EnZonaCaliente
        Salir(origen, Cfg.velocidadHuida);
    }

    void Desistir()
    {
        CambiarEstado(EstadoEstudiante.Desistido);
        OnDesistido?.Invoke(this);
        Vector3 desde = jugador != null ? jugador.position : transform.position + transform.forward;
        Salir(desde, agente.speed);
    }

    void Salir(Vector3 lejosDe, float velocidad)
    {
        MostrarTell(false);
        if (colision != null) colision.enabled = false;
        if (Fila.Instancia != null) Fila.Instancia.Liberar(this);
        Puesto = -1;
        PrepararAgente(transform.position);
        if (agente.enabled) agente.speed = velocidad;
        Vector3 dir = transform.position - lejosDe;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) dir = -transform.forward;
        IrA(transform.position + dir.normalized * 15f);
        Destroy(gameObject, Cfg.segundosParaDesaparecer);
    }

    void IrAlFinalDeLaFila()
    {
        CambiarEstado(EstadoEstudiante.Merodeo);
        int p = Fila.Instancia != null ? Fila.Instancia.ReservarFinal(this) : -1;
        if (p < 0)
        {
            tipoInocente = TipoInocente.PasaDeLargo; // la fila está llena: sigue de largo
            IrA(salida);
            return;
        }
        Puesto = p;
        IrA(Fila.Instancia.Posicion(p));
    }

    IEnumerator RutinaBano(Vector3 bano, float segundos)
    {
        int miPuesto = Puesto;
        tipoInocente = TipoInocente.VuelveDelBano;
        entrando = false;
        Fila.Instancia.SalirTemporal(this, miPuesto);
        Puesto = -1;
        CambiarEstado(EstadoEstudiante.Merodeo);
        PrepararAgente(transform.position);
        agente.speed = Cfg.velocidadInocente;
        IrA(bano);
        yield return null;
        float t = 0f;
        while (!Llego() && t < 20f)
        {
            t += Time.deltaTime;
            yield return null;
        }
        yield return new WaitForSeconds(segundos);
        if (Fila.Instancia == null) yield break;
        int p = Fila.Instancia.Regresar(this, miPuesto); // su puesto, o el final si un colado se lo quitó
        if (p < 0) { Destroy(gameObject); yield break; }
        Puesto = p;
        IrA(Fila.Instancia.Posicion(p));
    }

    IEnumerator Deslizar(Vector3 destino, float segundos, bool asegurarAlFinal)
    {
        if (agente.enabled) agente.enabled = false;
        Vector3 desde = transform.position;
        for (float t = 0f; t < 1f; t += Time.deltaTime / Mathf.Max(0.01f, segundos))
        {
            transform.position = Vector3.Lerp(desde, destino, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }
        transform.position = destino;
        if (asegurarAlFinal) Asegurar();
    }

    // =============================== Utilidades ===============================

    void CambiarEstado(EstadoEstudiante nuevo)
    {
        Estado = nuevo;
        reloj = 0f;
        ActualizarAnillo();
        OnCambioEstado?.Invoke(this, nuevo);
    }

    void PrepararAgente(Vector3 cerca)
    {
        if (obstaculo != null) obstaculo.enabled = false;
        Vector3 pos = cerca;
        if (NavMesh.SamplePosition(cerca, out NavMeshHit hit, 4f, NavMesh.AllAreas)) pos = hit.position;
        transform.position = pos;
        agente.enabled = true;
        if (agente.isOnNavMesh) agente.Warp(pos);
        else Debug.LogWarning($"[Saldaña] '{name}' quedó fuera del NavMesh en {pos}. ¿Horneaste el NavMesh? (No te me colés → Saldaña → 3)", this);
    }

    void IrA(Vector3 destino)
    {
        if (!agente.enabled || !agente.isOnNavMesh) return;
        if (NavMesh.SamplePosition(destino, out NavMeshHit hit, 4f, NavMesh.AllAreas)) destino = hit.position;
        agente.isStopped = false;
        agente.SetDestination(destino);
    }

    bool Llego() =>
        agente.enabled && agente.isOnNavMesh && !agente.pathPending &&
        agente.remainingDistance <= agente.stoppingDistance + 0.3f;

    void Mirar(Vector3 punto)
    {
        Vector3 d = punto - transform.position;
        d.y = 0f;
        if (d.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), 10f * Time.deltaTime);
    }

    bool JugadorCerca(float radio)
    {
        if (radio <= 0f) return false;
        if (jugador == null)
        {
            var cc = FindAnyObjectByType<CharacterController>();
            if (cc == null) return false;
            jugador = cc.transform;
        }
        return DistanciaPlana(jugador.position, transform.position) < radio;
    }

    static float DistanciaPlana(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    void CrearTell()
    {
        if (tell != null) Destroy(tell);
        tell = null;
        if (facultad == null || facultad.tellPrefab == null) return;
        tell = Instantiate(facultad.tellPrefab, puntoTell != null ? puntoTell : transform, false);
        tell.SetActive(false);
    }

    void MostrarTell(bool ver)
    {
        if (tell != null) tell.SetActive(ver);
    }

    void Pintar()
    {
        if (cuerpo == null) return;
        Color c = colorCuerpo;
        if (Time.time < frenadoHasta) c = Color.Lerp(c, Color.cyan, 0.6f);
        if (destello > 0f) c = Color.white;
        if (c == colorPintado) return;
        colorPintado = c;
        cuerpo.GetPropertyBlock(bloque);
        bloque.SetColor(IdBaseColor, c);
        bloque.SetColor(IdColor, c);
        cuerpo.SetPropertyBlock(bloque);
    }

    // Anillo en el piso (GDD 12.3): amarillo = Aproximación, rojo = Intento.
    void ActualizarAnillo()
    {
        if (anillo == null) return;
        bool ver = !EsInocente && (Estado == EstadoEstudiante.Aproximacion || Estado == EstadoEstudiante.Intento);
        anillo.enabled = ver;
        if (!ver) return;
        Color c = Estado == EstadoEstudiante.Intento ? new Color(1f, 0.2f, 0.15f) : new Color(1f, 0.85f, 0.1f);
        anillo.GetPropertyBlock(bloque);
        bloque.SetColor(IdBaseColor, c);
        bloque.SetColor(IdColor, c);
        anillo.SetPropertyBlock(bloque);
    }

    void ActualizarEtiqueta()
    {
        if (etiqueta == null) return;
        bool ver = Cfg.mostrarEtiqueta;
        if (etiqueta.gameObject.activeSelf != ver) etiqueta.gameObject.SetActive(ver);
        if (!ver) return;
        int clave = ((int)Estado * 1000 + Mathf.RoundToInt(Verguenza)) * 8 + (int)tipoInocente;
        if (clave != claveEtiqueta)
        {
            claveEtiqueta = clave;
            etiqueta.text = EsInocente
            ? $"Inocente\n{tipoInocente}"
            : $"{(facultad != null ? facultad.nombre : "Colado")}\n{Estado}  V {Verguenza:0}";
        }
        var cam = Camera.main;
        if (cam != null) etiqueta.transform.rotation = cam.transform.rotation;
    }
}
