using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// HUD de la partida (GDD 12.2) y pantallas de inicio, pausa y resultados (GDD 10). Dueño: Héctor.
//  · Arriba al centro: reloj 10:00 → 12:00 y fase.        · Arriba a la izquierda: Paciencia y Escándalo.
//  · Arriba a la derecha: frustrados, infiltrados, Dignidad. · Abajo: herramientas con recarga circular.
//  · Sobre cada colado: barra de Vergüenza y de Intento.   · Bordes: flechas hacia colados fuera de vista.
// Todo se arma en código para no depender de prefabs. Solo lee al PartidaManager y al CinturonHerramientas.
public class HUDPartida : MonoBehaviour
{
    public PartidaManager partida;
    public CinturonHerramientas cinturon;
    public MedidorRendimiento medidor;

    static readonly Color Panel = new Color(0.07f, 0.08f, 0.11f, 0.78f);
    static readonly Color Tenue = new Color(1f, 1f, 1f, 0.7f);
    static readonly Color Amarillo = new Color(1f, 0.85f, 0.1f);
    static readonly Color Rojo = new Color(1f, 0.25f, 0.2f);
    static readonly Color Verde = new Color(0.35f, 0.85f, 0.4f);
    static readonly Color Morado = new Color(0.75f, 0.35f, 1f);
    static readonly Color Rosa = new Color(1f, 0.35f, 0.65f);

    RectTransform raiz;
    Camera cam;

    // Arriba
    Text reloj, fase, txtPaciencia, txtEscandalo, txtStats;
    Image barraPaciencia, barraEscandalo, barraDia, destelloPaciencia;
    float pacienciaMostrada = -1f;

    // Herramientas
    class Ranura { public Herramienta h; public RectTransform rt; public Image fondo, acento, recarga; public Text nombre, estado; }
    readonly List<Ranura> ranuras = new List<Ranura>();
    RectTransform barraHerramientas;

    // Sobre los colados
    class Marca { public RectTransform rt; public Image verguenza, intento; public GameObject filaIntento; }
    readonly List<Marca> marcas = new List<Marca>();
    readonly List<RectTransform> flechas = new List<RectTransform>();
    readonly List<Estudiante> colados = new List<Estudiante>();
    RectTransform capaMundo;

    // Avisos
    class Aviso { public RectTransform rt; public Text t; public Vector3 mundo; public float vida; }
    readonly List<Aviso> avisos = new List<Aviso>();
    Text banner;
    float bannerVida;

    // Pantallas
    GameObject panelIntro, panelPausa, panelFin;
    Text finTitulo, finRango, finEfectividad, finCuerpo, finAlmuerzo;
    Text txtFps;
    bool verFps;
    readonly StringBuilder sb = new StringBuilder(256);

    void Awake()
    {
        if (partida == null) partida = FindAnyObjectByType<PartidaManager>();
        if (cinturon == null) cinturon = FindAnyObjectByType<CinturonHerramientas>();
        if (medidor == null) medidor = FindAnyObjectByType<MedidorRendimiento>();
        AsegurarEventSystem();
        Construir();
    }

    void OnEnable()
    {
        Estudiante.OnCambioEstado += AlCambioEstado;
        if (partida != null)
        {
            partida.EstadoCambiado += AlEstado;
            partida.Evento += AlEvento;
        }
    }

    void OnDisable()
    {
        Estudiante.OnCambioEstado -= AlCambioEstado;
        if (partida != null)
        {
            partida.EstadoCambiado -= AlEstado;
            partida.Evento -= AlEvento;
        }
    }

    void Start() => AlEstado(partida != null ? partida.Estado : EstadoPartida.Jugando);

    // =============================== Construcción ===============================

    static void AsegurarEventSystem()
    {
        if (EventSystem.current != null || FindAnyObjectByType<EventSystem>() != null) return;
        var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        DontDestroyOnLoad(go);
    }

    void Construir()
    {
        var canvasGo = new GameObject("HUD (Héctor)", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var escala = canvasGo.GetComponent<CanvasScaler>();
        escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = new Vector2(1920f, 1080f);
        escala.matchWidthOrHeight = 0.5f;
        raiz = (RectTransform)canvasGo.transform;

        // Lo que sigue a los colados va debajo de todo lo demás.
        capaMundo = UIFabrica.Estirar(UIFabrica.Nodo("Sobre los colados", raiz, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero));

        ConstruirReloj();
        ConstruirBarras();
        ConstruirStats();
        barraHerramientas = UIFabrica.Nodo("Herramientas", raiz, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(600f, 120f));

        var b = UIFabrica.Nodo("Banner", raiz, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 220f), new Vector2(1400f, 120f));
        banner = UIFabrica.Texto(b, "", 64, TextAnchor.MiddleCenter, Amarillo, FontStyle.Bold);
        banner.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.8f);

        var f = UIFabrica.Nodo("FPS", raiz, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-16f, 12f), new Vector2(520f, 120f));
        txtFps = UIFabrica.Texto(f, "", 22, TextAnchor.LowerRight, Color.white);
        txtFps.gameObject.SetActive(false);

        ConstruirIntro();
        ConstruirPausa();
        ConstruirFin();
    }

    void ConstruirReloj()
    {
        var p = UIFabrica.Nodo("Reloj", raiz, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(360f, 118f));
        UIFabrica.Imagen(p, Panel);
        reloj = UIFabrica.Texto(UIFabrica.Nodo("Hora", p, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -6f), new Vector2(360f, 64f)),
            "10:00 a. m.", 52, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold);
        fase = UIFabrica.Texto(UIFabrica.Nodo("Fase", p, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -68f), new Vector2(360f, 30f)),
            "", 24, TextAnchor.MiddleCenter, Amarillo);
        var fondo = UIFabrica.Nodo("Día", p, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(320f, 8f));
        UIFabrica.Imagen(fondo, new Color(1f, 1f, 1f, 0.15f));
        barraDia = UIFabrica.Barra(UIFabrica.Estirar(UIFabrica.Nodo("Avance", fondo, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero)), Amarillo);
    }

    void ConstruirBarras()
    {
        var p = UIFabrica.Nodo("Barras", raiz, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -16f), new Vector2(420f, 132f));
        UIFabrica.Imagen(p, Panel);
        txtPaciencia = UIFabrica.Texto(UIFabrica.Nodo("Paciencia", p, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -8f), new Vector2(390f, 30f)),
            "Paciencia de la fila", 22, TextAnchor.MiddleLeft, Color.white);
        var f1 = UIFabrica.Nodo("Fondo paciencia", p, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -40f), new Vector2(388f, 22f));
        UIFabrica.Imagen(f1, new Color(1f, 1f, 1f, 0.12f));
        barraPaciencia = UIFabrica.Barra(UIFabrica.Estirar(UIFabrica.Nodo("Paciencia", f1, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero)), Verde);
        destelloPaciencia = UIFabrica.Imagen(UIFabrica.Estirar(UIFabrica.Nodo("Destello", f1, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero), -3f), new Color(1f, 0.2f, 0.2f, 0f));

        txtEscandalo = UIFabrica.Texto(UIFabrica.Nodo("Escándalo", p, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -70f), new Vector2(390f, 30f)),
            "Escándalo", 22, TextAnchor.MiddleLeft, Color.white);
        var f2 = UIFabrica.Nodo("Fondo escándalo", p, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -102f), new Vector2(388f, 14f));
        UIFabrica.Imagen(f2, new Color(1f, 1f, 1f, 0.12f));
        barraEscandalo = UIFabrica.Barra(UIFabrica.Estirar(UIFabrica.Nodo("Escándalo", f2, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero)), Morado);
    }

    void ConstruirStats()
    {
        var p = UIFabrica.Nodo("Estadísticas", raiz, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-16f, -16f), new Vector2(330f, 132f));
        UIFabrica.Imagen(p, Panel);
        txtStats = UIFabrica.Texto(UIFabrica.Estirar(UIFabrica.Nodo("Texto", p, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero), 14f),
            "", 24, TextAnchor.MiddleLeft, Color.white);
        txtStats.lineSpacing = 1.1f;
    }

    GameObject Pantalla(string nombre, float alfa)
    {
        var rt = UIFabrica.Estirar(UIFabrica.Nodo(nombre, raiz, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero));
        var img = UIFabrica.Imagen(rt, new Color(0.03f, 0.04f, 0.06f, alfa));
        img.raycastTarget = true;   // mientras se ve, bloquea el clic del juego
        rt.gameObject.SetActive(false);
        return rt.gameObject;
    }

    void ConstruirIntro()
    {
        panelIntro = Pantalla("Pantalla de inicio", 0.82f);
        var t = panelIntro.transform;
        UIFabrica.Texto(UIFabrica.Nodo("Título", t, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -90f), new Vector2(1400f, 120f)),
            "¡No te me colés!", 96, TextAnchor.MiddleCenter, Amarillo, FontStyle.Bold);
        UIFabrica.Texto(UIFabrica.Nodo("Subtítulo", t, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -195f), new Vector2(1400f, 50f)),
            "De 10:00 a 12:00 la fila del restaurante es un campo de batalla. Que se cuele la menor cantidad de gente.",
            28, TextAnchor.MiddleCenter, Tenue);

        sb.Clear();
        sb.Append("<b>La regla de oro</b>\n");
        sb.Append("Un colado solo es vulnerable mientras se mete: anillo <color=#").Append(UIFabrica.Hex(Amarillo)).Append(">amarillo</color> (se acerca) o ");
        sb.Append("<color=#").Append(UIFabrica.Hex(Rojo)).Append(">rojo</color> (se está metiendo). Cuando ya quedó adentro, es intocable.\n\n");
        sb.Append("<b>Cada facultad tiene su señal y su herramienta</b>\n");
        var spawner = partida != null ? partida.spawner : null;
        if (spawner != null && spawner.facultades != null)
            foreach (var fac in spawner.facultades)
            {
                if (fac == null) continue;
                sb.Append("• <color=#").Append(UIFabrica.Hex(fac.colorAcento)).Append("><b>").Append(fac.nombre).Append("</b></color>: ")
                  .Append(fac.tell).Append(". Lo saca: <b>").Append(NombreHerramienta(fac.counter)).Append("</b>")
                  .Append(" (no le hace nada: ").Append(NombreHerramienta(fac.resistencia)).Append(")\n");
            }
        sb.Append("• <b>Inocentes</b>: no tienen anillo. Si los atacas, la fila pierde la paciencia y te saca a ti.\n\n");
        sb.Append("<b>Controles</b>   WASD moverse · Mouse apuntar · Clic usar · 1 agua · 2 megáfono · Esc pausa · F3 FPS");
        UIFabrica.Texto(UIFabrica.Nodo("Reglas", t, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(1300f, 480f)),
            sb.ToString(), 28, TextAnchor.MiddleLeft, Color.white);

        var empezar = UIFabrica.Texto(UIFabrica.Nodo("Empezar", t, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 190f), new Vector2(1400f, 60f)),
            "Clic o Enter para empezar", 40, TextAnchor.MiddleCenter, Amarillo, FontStyle.Bold);
        empezar.gameObject.AddComponent<Parpadeo>();
    }

    void ConstruirPausa()
    {
        panelPausa = Pantalla("Pausa", 0.6f);
        var t = panelPausa.transform;
        UIFabrica.Texto(UIFabrica.Nodo("Título", t, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(900f, 120f)),
            "Pausa", 90, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold);
        UIFabrica.Boton(t, "Seguir (Esc)", new Vector2(0f, -10f), new Vector2(420f, 80f), new Color(0.2f, 0.55f, 0.3f), () => partida.Pausar(false));
        UIFabrica.Boton(t, "Reiniciar (R)", new Vector2(0f, -110f), new Vector2(420f, 80f), new Color(0.3f, 0.35f, 0.45f), () => partida.Reiniciar());
        UIFabrica.Boton(t, "Salir", new Vector2(0f, -210f), new Vector2(420f, 80f), new Color(0.5f, 0.2f, 0.2f), () => partida.Salir());
    }

    void ConstruirFin()
    {
        panelFin = Pantalla("Resultados", 0.85f);
        var t = panelFin.transform;
        finTitulo = UIFabrica.Texto(UIFabrica.Nodo("Título", t, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(1600f, 110f)),
            "", 80, TextAnchor.MiddleCenter, Amarillo, FontStyle.Bold);

        var izq = UIFabrica.Nodo("Rango", t, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-430f, 40f), new Vector2(420f, 420f));
        UIFabrica.Imagen(izq, Panel);
        UIFabrica.Texto(UIFabrica.Nodo("Etiqueta", izq, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(400f, 40f)),
            "Rango", 30, TextAnchor.MiddleCenter, Tenue);
        finRango = UIFabrica.Texto(UIFabrica.Nodo("Letra", izq, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(400f, 240f)),
            "", 200, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold);
        finEfectividad = UIFabrica.Texto(UIFabrica.Nodo("Efectividad", izq, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(400f, 50f)),
            "", 34, TextAnchor.MiddleCenter, Color.white);

        var der = UIFabrica.Nodo("Estadísticas", t, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(230f, 40f), new Vector2(860f, 420f));
        UIFabrica.Imagen(der, Panel);
        finCuerpo = UIFabrica.Texto(UIFabrica.Estirar(UIFabrica.Nodo("Texto", der, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero), 28f),
            "", 30, TextAnchor.MiddleLeft, Color.white);
        finCuerpo.lineSpacing = 1.15f;

        finAlmuerzo = UIFabrica.Texto(UIFabrica.Nodo("Almuerzo", t, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -240f), new Vector2(1500f, 90f)),
            "", 34, TextAnchor.MiddleCenter, Amarillo, FontStyle.Italic);

        UIFabrica.Boton(t, "Jugar otra vez (R)", new Vector2(-230f, -370f), new Vector2(420f, 80f), new Color(0.2f, 0.55f, 0.3f), () => partida.Reiniciar());
        UIFabrica.Boton(t, "Salir", new Vector2(230f, -370f), new Vector2(420f, 80f), new Color(0.5f, 0.2f, 0.2f), () => partida.Salir());
    }

    void ConstruirRanuras()
    {
        foreach (var r in ranuras) if (r.rt != null) Destroy(r.rt.gameObject);
        ranuras.Clear();
        if (cinturon == null) return;
        var lista = cinturon.Herramientas;
        const float ancho = 190f, alto = 110f, sep = 16f;
        float total = lista.Count * ancho + (lista.Count - 1) * sep;
        barraHerramientas.sizeDelta = new Vector2(total, alto);
        for (int i = 0; i < lista.Count; i++)
        {
            var h = lista[i];
            var r = new Ranura { h = h };
            r.rt = UIFabrica.Nodo("Ranura " + (i + 1), barraHerramientas, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(i * (ancho + sep), 0f), new Vector2(ancho, alto));
            r.fondo = UIFabrica.Imagen(r.rt, Panel);
            Color c = h != null && h.datos != null ? h.datos.color : Color.white;
            c.a = 1f;
            r.acento = UIFabrica.Imagen(UIFabrica.Nodo("Acento", r.rt, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(ancho, 6f)), c);
            r.recarga = UIFabrica.Imagen(UIFabrica.Nodo("Recarga", r.rt, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-10f, -10f), new Vector2(46f, 46f)),
                new Color(1f, 1f, 1f, 0.9f), UIFabrica.Circulo);
            r.recarga.type = Image.Type.Filled;
            r.recarga.fillMethod = Image.FillMethod.Radial360;
            r.recarga.fillOrigin = (int)Image.Origin360.Top;
            r.recarga.fillClockwise = true;
            UIFabrica.Texto(UIFabrica.Nodo("Tecla", r.rt, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(10f, -6f), new Vector2(40f, 40f)),
                (i + 1).ToString(), 30, TextAnchor.UpperLeft, Tenue, FontStyle.Bold);
            r.nombre = UIFabrica.Texto(UIFabrica.Nodo("Nombre", r.rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 2f), new Vector2(ancho - 12f, 34f)),
                h != null && h.datos != null ? h.datos.nombre : "?", 24, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold);
            r.nombre.resizeTextForBestFit = true;      // "Botella de agua" en una sola línea
            r.nombre.resizeTextMinSize = 14;
            r.nombre.resizeTextMaxSize = 24;
            r.nombre.horizontalOverflow = HorizontalWrapMode.Overflow;
            r.estado = UIFabrica.Texto(UIFabrica.Nodo("Estado", r.rt, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 12f), new Vector2(ancho - 12f, 30f)),
                "", 20, TextAnchor.MiddleCenter, Tenue);
            ranuras.Add(r);
        }
    }

    static string NombreHerramienta(TipoHerramienta t)
    {
        switch (t)
        {
            case TipoHerramienta.Agua: return "agua [1]";
            case TipoHerramienta.Megafono: return "megáfono [2]";
            case TipoHerramienta.Silbato: return "silbato";
            case TipoHerramienta.Camara: return "cámara";
            default: return "conos";
        }
    }

    // =============================== Cada cuadro ===============================

    void LateUpdate()
    {
        if (cam == null) cam = Camera.main;
        var kb = Keyboard.current;
        if (kb != null && kb.f3Key.wasPressedThisFrame)
        {
            verFps = !verFps;
            txtFps.gameObject.SetActive(verFps);
        }
        if (partida == null) return;

        float dt = Time.unscaledDeltaTime;
        ActualizarArriba(dt);
        ActualizarHerramientas();
        ActualizarColados();
        ActualizarAvisos(dt);
        if (verFps && medidor != null) txtFps.text = medidor.Resumen;
    }

    void ActualizarArriba(float dt)
    {
        reloj.text = partida.HoraTexto;
        fase.text = partida.FaseActual + (partida.Velocidad > 1f ? $"   (x{partida.Velocidad:0})" : "");
        barraDia.fillAmount = partida.Progreso;

        float max = Mathf.Max(1f, partida.Cfg.pacienciaInicial);
        float p = partida.Paciencia / max;
        if (pacienciaMostrada >= 0f && p < pacienciaMostrada - 0.001f) destelloPaciencia.color = new Color(1f, 0.2f, 0.2f, 0.9f);
        pacienciaMostrada = pacienciaMostrada < 0f ? p : Mathf.MoveTowards(pacienciaMostrada, p, dt * 0.8f);
        barraPaciencia.fillAmount = pacienciaMostrada;
        barraPaciencia.color = Color.Lerp(Rojo, Verde, Mathf.InverseLerp(0.15f, 0.7f, pacienciaMostrada));
        var d = destelloPaciencia.color;
        d.a = Mathf.MoveTowards(d.a, 0f, dt * 2f);
        destelloPaciencia.color = d;
        txtPaciencia.text = $"Paciencia de la fila  <b>{partida.Paciencia:0}</b>";
        barraEscandalo.fillAmount = partida.Escandalo / 100f;
        txtEscandalo.text = $"Escándalo  <b>{partida.Escandalo:0}</b>";

        var s = partida.Stats;
        sb.Clear();
        sb.Append("Frustrados  <b><color=#").Append(UIFabrica.Hex(Verde)).Append('>').Append(s.Frustrados).Append("</color></b>\n");
        sb.Append("Infiltrados  <b><color=#").Append(UIFabrica.Hex(Rojo)).Append('>').Append(s.Infiltrados).Append("</color></b>");
        if (s.infiltradosDelante > 0) sb.Append("  (").Append(s.infiltradosDelante).Append(" delante)");
        sb.Append("\nDignidad  <b><color=#").Append(UIFabrica.Hex(Amarillo)).Append('>').Append(s.dignidad).Append("</color></b>");
        txtStats.text = sb.ToString();
    }

    void ActualizarHerramientas()
    {
        if (cinturon == null) return;
        if (ranuras.Count != cinturon.Herramientas.Count) ConstruirRanuras();
        for (int i = 0; i < ranuras.Count; i++)
        {
            var r = ranuras[i];
            var h = r.h;
            if (h == null || h.datos == null) continue;
            bool actual = h == cinturon.Actual;
            r.rt.localScale = Vector3.one * (actual ? 1.08f : 0.94f);
            r.fondo.color = actual ? new Color(0.16f, 0.18f, 0.24f, 0.95f) : Panel;
            r.recarga.fillAmount = h.Congelada ? 1f : 1f - h.ProgresoRecarga;
            r.recarga.color = h.Congelada ? new Color(0.5f, 0.8f, 1f, 0.9f) : new Color(1f, 1f, 1f, 0.85f);

            string estado;
            Color ce = Tenue;
            if (h.Congelada) { estado = "congelada"; ce = new Color(0.5f, 0.8f, 1f); }
            else if (h.EnRecarga) { estado = $"{h.SegundosRestantes:0.0} s"; ce = Amarillo; }
            else if (!h.TieneMunicion) { estado = "vacía: ve al bebedero"; ce = Rojo; }
            else estado = h.UsaMunicion ? $"{h.Municion}/{h.datos.municionMax}  lista" : "lista";
            if (h.UsaMunicion && h.EnRecarga) estado += $"  ({h.Municion}/{h.datos.municionMax})";
            r.estado.text = estado;
            r.estado.color = ce;
        }
    }

    void AlCambioEstado(Estudiante e, EstadoEstudiante s)
    {
        if (e == null || e.EsInocente) return;
        bool activo = s == EstadoEstudiante.Merodeo || s == EstadoEstudiante.Aproximacion || s == EstadoEstudiante.Intento;
        if (activo) { if (!colados.Contains(e)) colados.Add(e); }
        else colados.Remove(e);
    }

    void ActualizarColados()
    {
        colados.RemoveAll(e => e == null);
        int nMarcas = 0, nFlechas = 0;
        bool jugando = partida.Estado == EstadoPartida.Jugando || partida.Estado == EstadoPartida.Pausa;
        if (cam != null && jugando)
        {
            Vector2 tam = raiz.rect.size;
            foreach (var e in colados)
            {
                bool amenaza = e.Estado == EstadoEstudiante.Aproximacion || e.Estado == EstadoEstudiante.Intento;
                Vector3 sp = cam.WorldToScreenPoint(e.transform.position + Vector3.up * 2.4f);
                bool dentro = sp.z > 0f && sp.x > 0f && sp.y > 0f && sp.x < Screen.width && sp.y < Screen.height;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(capaMundo, sp, null, out Vector2 local);

                if (dentro && (e.Verguenza > 0.5f || e.Estado == EstadoEstudiante.Intento))
                {
                    var m = MarcaN(nMarcas++);
                    m.rt.anchoredPosition = local;
                    m.verguenza.fillAmount = e.Verguenza / 100f;
                    bool intento = e.Estado == EstadoEstudiante.Intento;
                    if (m.filaIntento.activeSelf != intento) m.filaIntento.SetActive(intento);
                    m.intento.fillAmount = e.ProgresoIntento;
                }
                else if (!dentro && amenaza)
                {
                    // Flecha en el borde, apuntando hacia el colado (GDD 12.2).
                    var f = FlechaN(nFlechas++);
                    Vector2 dir = local;                       // relativo al centro de la pantalla
                    if (sp.z < 0f) dir = -dir;
                    if (dir.sqrMagnitude < 1f) dir = Vector2.up;
                    Vector2 mitad = tam * 0.5f - new Vector2(60f, 60f);
                    float k = Mathf.Min(mitad.x / Mathf.Max(0.001f, Mathf.Abs(dir.x)), mitad.y / Mathf.Max(0.001f, Mathf.Abs(dir.y)));
                    f.anchoredPosition = dir * k;
                    f.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
                    f.GetComponent<Image>().color = e.Estado == EstadoEstudiante.Intento ? Rojo : Amarillo;
                }
            }
        }
        for (int i = nMarcas; i < marcas.Count; i++) if (marcas[i].rt.gameObject.activeSelf) marcas[i].rt.gameObject.SetActive(false);
        for (int i = nFlechas; i < flechas.Count; i++) if (flechas[i].gameObject.activeSelf) flechas[i].gameObject.SetActive(false);
    }

    Marca MarcaN(int i)
    {
        while (marcas.Count <= i)
        {
            var m = new Marca();
            m.rt = UIFabrica.Nodo("Marca", capaMundo, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(90f, 26f));
            var f1 = UIFabrica.Nodo("Fondo vergüenza", m.rt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(90f, 12f));
            UIFabrica.Imagen(f1, new Color(0f, 0f, 0f, 0.6f));
            m.verguenza = UIFabrica.Barra(UIFabrica.Estirar(UIFabrica.Nodo("Vergüenza", f1, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero), 2f), Rosa);
            var f2 = UIFabrica.Nodo("Fondo intento", m.rt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(90f, 8f));
            UIFabrica.Imagen(f2, new Color(0f, 0f, 0f, 0.6f));
            m.intento = UIFabrica.Barra(UIFabrica.Estirar(UIFabrica.Nodo("Intento", f2, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero), 1f), Rojo);
            m.filaIntento = f2.gameObject;
            marcas.Add(m);
        }
        var r = marcas[i];
        if (!r.rt.gameObject.activeSelf) r.rt.gameObject.SetActive(true);
        return r;
    }

    RectTransform FlechaN(int i)
    {
        while (flechas.Count <= i)
            flechas.Add(UIFabrica.Imagen(UIFabrica.Nodo("Flecha", capaMundo, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(54f, 54f)),
                Amarillo, UIFabrica.Flecha).rectTransform);
        var f = flechas[i];
        if (!f.gameObject.activeSelf) f.gameObject.SetActive(true);
        return f;
    }

    // =============================== Avisos ===============================

    void AlEvento(EventoPartida tipo, Estudiante e, string texto)
    {
        switch (tipo)
        {
            case EventoPartida.CambioFase:
                Banner($"{partida.HoraTexto} · ¡{texto}!", Amarillo);
                return;
            case EventoPartida.Timbre:
            case EventoPartida.Derrota:
                Banner(texto, tipo == EventoPartida.Derrota ? Rojo : Amarillo);
                return;
        }
        if (e == null) return;
        Color c = tipo == EventoPartida.Expulsado ? Verde
                : tipo == EventoPartida.Desistido ? new Color(0.6f, 0.9f, 0.6f)
                : tipo == EventoPartida.Infiltrado ? Rojo
                : tipo == EventoPartida.FalsoPositivo ? new Color(1f, 0.5f, 0.2f)
                : Amarillo;
        Avisar(e.transform.position + Vector3.up * 2.6f, texto, c, tipo == EventoPartida.FalsoPositivo || tipo == EventoPartida.Infiltrado ? 32 : 28);
        if (tipo == EventoPartida.Infiltrado && e.EnZonaCaliente) Banner("¡Se te coló uno delante!", Rojo, 1.4f);
        if (tipo == EventoPartida.FalsoPositivo) Banner("¡Era inocente!", new Color(1f, 0.5f, 0.2f), 1.2f);
    }

    void Banner(string texto, Color c, float segundos = 2.4f)
    {
        banner.text = texto;
        banner.color = c;
        bannerVida = segundos;
    }

    void Avisar(Vector3 mundo, string texto, Color c, int tam)
    {
        Aviso a = null;
        foreach (var x in avisos) if (x.vida <= 0f) { a = x; break; }
        if (a == null)
        {
            a = new Aviso();
            a.rt = UIFabrica.Nodo("Aviso", capaMundo, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(520f, 50f));
            a.t = UIFabrica.Texto(a.rt, "", tam, TextAnchor.LowerCenter, c, FontStyle.Bold);
            a.t.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.8f);
            avisos.Add(a);
        }
        a.mundo = mundo;
        a.vida = 1.6f;
        a.t.text = texto;
        a.t.fontSize = tam;
        a.t.color = c;
        a.rt.gameObject.SetActive(true);
    }

    void ActualizarAvisos(float dt)
    {
        foreach (var a in avisos)
        {
            if (a.vida <= 0f) continue;
            a.vida -= dt;
            if (a.vida <= 0f || cam == null) { a.rt.gameObject.SetActive(false); continue; }
            Vector3 sp = cam.WorldToScreenPoint(a.mundo);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(capaMundo, sp, null, out Vector2 local);
            a.rt.anchoredPosition = local + Vector2.up * (1.6f - a.vida) * 60f;
            var c = a.t.color;
            c.a = Mathf.Clamp01(a.vida / 0.5f);
            a.t.color = c;
        }
        if (bannerVida > 0f)
        {
            bannerVida -= dt;
            var c = banner.color;
            c.a = Mathf.Clamp01(bannerVida / 0.4f);
            banner.color = c;
            if (bannerVida <= 0f) banner.text = "";
        }
    }

    // =============================== Pantallas ===============================

    void AlEstado(EstadoPartida estado)
    {
        panelIntro.SetActive(estado == EstadoPartida.Intro);
        panelPausa.SetActive(estado == EstadoPartida.Pausa);
        panelFin.SetActive(estado == EstadoPartida.Terminada);
        // Las herramientas solo estorban detrás de la intro y de los resultados.
        barraHerramientas.gameObject.SetActive(estado == EstadoPartida.Jugando || estado == EstadoPartida.Pausa);
        if (estado == EstadoPartida.Terminada) LlenarResultados();
    }

    void LlenarResultados()
    {
        var r = partida.Resultado;
        var s = partida.Stats;
        if (r == null) return;
        finTitulo.text = partida.Derrota ? "¡La fila se hartó y te sacaron!" : "¡Sonó el timbre de las 12:00!";
        finTitulo.color = partida.Derrota ? Rojo : Amarillo;
        finRango.text = r.rango == Puntaje.RangoDerrota ? "X" : r.rango;
        finRango.color = Puntaje.ColorRango(r.rango);
        finEfectividad.text = partida.Derrota ? "Expulsado" : $"Efectividad {r.efectividad:0} %";

        sb.Clear();
        sb.Append("Colados frustrados  <b>").Append(s.Frustrados).Append("</b>  (").Append(s.expulsados).Append(" expulsados, ")
          .Append(s.desistidos).Append(" desistieron)\n");
        sb.Append("Infiltrados  <b><color=#").Append(UIFabrica.Hex(Rojo)).Append('>').Append(s.Infiltrados).Append("</color></b>  (")
          .Append(s.infiltradosDelante).Append(" delante de ti, ").Append(s.infiltradosDetras).Append(" detrás)\n");
        sb.Append("Falsos positivos (pena ajena)  <b>").Append(s.falsosPositivos).Append("</b>\n");
        if (s.sobreAsegurados > 0) sb.Append("Ataques a quien ya estaba adentro  <b>").Append(s.sobreAsegurados).Append("</b>\n");
        sb.Append("Facultad más difícil del día  <b>").Append(string.IsNullOrEmpty(r.facultadMasDificil) ? "ninguna" : r.facultadMasDificil).Append("</b>\n");
        sb.Append("Dignidad  <b><color=#").Append(UIFabrica.Hex(Amarillo)).Append('>').Append(s.dignidad).Append("</color></b>");
        if (r.dignidadBonus > 0) sb.Append("  (incluye +").Append(r.dignidadBonus).Append(" por el rango)");
        finCuerpo.text = sb.ToString();

        string hora = Puntaje.Hora(partida.Cfg.minutoFin + r.minutosAlmuerzo);
        string sinTi = Puntaje.Hora(partida.Cfg.minutoFin + r.minutosSinTi);
        finAlmuerzo.text = partida.Derrota
            ? $"Te sacaron de la fila: hoy almuerzas de último. Tu puesto era el #{partida.PuestoJugador}."
            : r.minutosSinTi > r.minutosAlmuerzo + 0.01f
                ? $"Gracias a tu vigilancia almorzaste a las {hora} Sin ti habría sido a las {sinTi}"
                : $"Eras el #{partida.PuestoJugador} de la fila: almorzaste a las {hora}";
    }
}

// Hace titilar un texto (el "Clic para empezar"). Usa tiempo real porque la intro tiene el tiempo en pausa.
public class Parpadeo : MonoBehaviour
{
    Graphic g;
    void Awake() => g = GetComponent<Graphic>();
    void Update()
    {
        if (g == null) return;
        var c = g.color;
        c.a = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 4f);
        g.color = c;
    }
}
