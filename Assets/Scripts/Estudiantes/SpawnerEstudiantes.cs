using System.Collections.Generic;
using UnityEngine;

// Una fase de la partida (GDD 5).
[System.Serializable]
public class FaseSpawn
{
    public string nombre = "Fase";
    [Tooltip("Segundo de partida en que empieza (0–240).")]
    public float desde;
    public float hasta = 240f;
    [Tooltip("Segundos entre llegadas.")]
    public float intervalo = 10f;
    [Tooltip("Cuántos llegan juntos en cada llegada (mín, máx).")]
    public int grupoMin = 1;
    public int grupoMax = 1;
    [Tooltip("Oleada que llega de golpe al empezar la fase.")]
    public int oleadaAlEmpezar;
    [Tooltip("Cuántas facultades de la lista pueden aparecer en esta fase.")]
    public int facultadesActivas = 1;
    [Tooltip("Multiplica la Aproximación y el Intento. Estampida: 0,7 (ventanas 30 % más cortas).")]
    public float multiplicadorVentana = 1f;

    public FaseSpawn() { }

    public FaseSpawn(string nombre, float desde, float hasta, float intervalo, int grupoMin, int grupoMax,
                     int oleadaAlEmpezar, int facultadesActivas, float multiplicadorVentana)
    {
        this.nombre = nombre;
        this.desde = desde;
        this.hasta = hasta;
        this.intervalo = intervalo;
        this.grupoMin = grupoMin;
        this.grupoMax = grupoMax;
        this.oleadaAlEmpezar = oleadaAlEmpezar;
        this.facultadesActivas = facultadesActivas;
        this.multiplicadorVentana = multiplicadorVentana;
    }
}

// Hace llegar estudiantes por fases (GDD 5). Dueño: Saldaña.
// Usa su propio reloj; Héctor puede conectar el del GameManager con:
//   SpawnerEstudiantes.RelojExterno = () => segundosDePartida;
public class SpawnerEstudiantes : MonoBehaviour
{
    public static System.Func<float> RelojExterno;
    public static event System.Action<FaseSpawn> OnCambioFase;

    public Estudiante prefab;
    public ConfigEstudiantes config;
    [Tooltip("En orden de aparición: las primeras se usan en las fases con menos facultades.")]
    public FacultadData[] facultades;
    public Transform[] puntosAparicion;
    [Tooltip("GDD 4.3: entre 30 % y 50 % de los que llegan son inocentes.")]
    [Range(0f, 1f)] public float proporcionInocentes = 0.4f;
    public float duracionPartida = 240f;
    [Tooltip("Segundos hasta el primer colado (para no esperar 12 s al probar).")]
    public float primeraLlegada = 3f;
    public List<FaseSpawn> fases = FasesGDD();

    public FaseSpawn FaseActual { get; private set; }
    public float Tiempo => RelojExterno != null ? RelojExterno() : Time.time - inicio;
    public int Aparecidos { get; private set; }

    float inicio;
    float proxima;

    // Tabla de la sección 5 del GDD.
    public static List<FaseSpawn> FasesGDD() => new List<FaseSpawn>
    {
        new FaseSpawn("Calma",            0f,   80f, 12f, 1, 1, 0, 1, 1f),
        new FaseSpawn("Salida de clases", 80f, 120f, 40f, 1, 1, 5, 2, 1f),
        new FaseSpawn("Media mañana",     120f, 180f, 7f, 1, 1, 0, 2, 1f),
        new FaseSpawn("Hora pico",        180f, 230f, 4f, 1, 2, 6, 2, 1f),
        new FaseSpawn("Estampida",        230f, 240f, 3f, 1, 2, 0, 2, 0.7f),
    };

    void Reset() => fases = FasesGDD();

    void Start()
    {
        inicio = Time.time;
        proxima = primeraLlegada;
    }

    void Update()
    {
        if (prefab == null || puntosAparicion == null || puntosAparicion.Length == 0) return;
        float t = Tiempo;
        if (t >= duracionPartida) return;
        var fase = FaseEn(t);
        if (fase == null) return;

        if (fase != FaseActual)
        {
            bool primera = FaseActual == null;
            FaseActual = fase;
            OnCambioFase?.Invoke(fase);
            if (fase.oleadaAlEmpezar > 0) LlegarGrupo(fase, fase.oleadaAlEmpezar);
            if (!primera) proxima = t + fase.intervalo * Random.Range(0.8f, 1.2f);
        }
        if (t >= proxima)
        {
            LlegarGrupo(fase, Random.Range(fase.grupoMin, fase.grupoMax + 1));
            proxima = t + fase.intervalo * Random.Range(0.8f, 1.2f);
        }
    }

    FaseSpawn FaseEn(float t)
    {
        foreach (var f in fases)
            if (t >= f.desde && t < f.hasta) return f;
        return null;
    }

    void LlegarGrupo(FaseSpawn fase, int cuantos)
    {
        int a = Random.Range(0, puntosAparicion.Length);
        int b = puntosAparicion.Length > 1 ? (a + Random.Range(1, puntosAparicion.Length)) % puntosAparicion.Length : a;
        Vector3 desde = puntosAparicion[a].position;
        Vector3 salida = puntosAparicion[b].position;
        for (int k = 0; k < cuantos; k++)
        {
            Vector2 r = Random.insideUnitCircle * 1.5f;
            Aparecer(fase, desde + new Vector3(r.x, 0f, r.y), salida);
        }
    }

    void Aparecer(FaseSpawn fase, Vector3 punto, Vector3 salida)
    {
        var e = Instantiate(prefab, punto, Quaternion.identity, transform);
        Aparecidos++;
        e.name = $"Estudiante {Aparecidos}";
        e.config = config;
        e.MultiplicadorVentana = fase.multiplicadorVentana;
        bool inocente = facultades == null || facultades.Length == 0 || Random.value < proporcionInocentes;
        if (inocente)
        {
            e.Llegar(punto, null, Random.value < 0.5f ? TipoInocente.Legal : TipoInocente.PasaDeLargo, salida);
        }
        else
        {
            int n = Mathf.Clamp(fase.facultadesActivas, 1, facultades.Length);
            e.Llegar(punto, facultades[Random.Range(0, n)], TipoInocente.Ninguno, salida);
        }
    }

    void OnDrawGizmos()
    {
        if (puntosAparicion == null) return;
        Gizmos.color = Color.cyan;
        foreach (var p in puntosAparicion)
            if (p != null) Gizmos.DrawWireSphere(p.position + Vector3.up * 0.5f, 1.5f);
    }
}
