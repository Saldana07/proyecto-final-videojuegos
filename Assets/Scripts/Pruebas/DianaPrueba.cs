using UnityEngine;

// SOLO PARA PROBAR LAS HERRAMIENTAS mientras llega el Estudiante de Saldaña.
// Cápsula que recibe herramientas, acumula Vergüenza y "se va" si la expulsan.
// No es el Estudiante real: no camina ni tiene máquina de estados.
public class DianaPrueba : MonoBehaviour, IReceptorHerramienta
{
    public string etiqueta = "Sistemas";
    public bool esInocente;
    [Tooltip("Herramienta que la expulsa al instante (el counter de su facultad).")]
    public TipoHerramienta counter = TipoHerramienta.Megafono;
    public float decaimientoVerguenza = 10f;
    public float segundosParaVolver = 3f;

    public float Verguenza { get; private set; }
    public int Impactos { get; private set; }
    public bool Expulsada { get; private set; }

    Renderer rend;
    TextMesh texto;
    Color colorBase;
    float destello;
    float frenadaHasta;
    float volverEn;

    void Awake()
    {
        rend = GetComponentInChildren<Renderer>();
        if (rend != null) colorBase = rend.material.color;
        texto = GetComponentInChildren<TextMesh>();
    }

    public void RecibirHerramienta(TipoHerramienta tipo) =>
        RecibirHerramienta(new ImpactoHerramienta { tipo = tipo });

    public void RecibirHerramienta(ImpactoHerramienta impacto)
    {
        if (Expulsada) return;
        Impactos++;
        destello = 0.3f;

        if (esInocente)
        {
            Debug.Log($"[Prueba] FALSO POSITIVO: {impacto.tipo} sobre inocente '{name}'", this);
            return;
        }

        Verguenza = Mathf.Min(100f, Verguenza + impacto.verguenza);
        if (impacto.tipo == counter || Verguenza >= 100f)
        {
            Debug.Log($"[Prueba] EXPULSADO '{name}' ({etiqueta}) con {impacto.tipo}", this);
            Expulsar();
        }
        else
        {
            frenadaHasta = Time.time + impacto.frenarSegundos;
            Debug.Log($"[Prueba] {impacto.tipo} sobre '{name}' ({etiqueta}): Vergüenza {Verguenza:0}, frenado {impacto.frenarSegundos:0.#} s", this);
        }
    }

    void Expulsar()
    {
        Expulsada = true;
        volverEn = Time.time + segundosParaVolver;
        foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;
        foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
    }

    void Update()
    {
        if (Expulsada)
        {
            if (Time.time < volverEn) return;
            Expulsada = false;
            Verguenza = 0f;
            foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = true;
            foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = true;
        }

        Verguenza = Mathf.Max(0f, Verguenza - decaimientoVerguenza * Time.deltaTime);
        destello -= Time.deltaTime;

        if (rend != null)
        {
            Color c = destello > 0f ? Color.white : colorBase;
            if (Time.time < frenadaHasta) c = Color.Lerp(colorBase, Color.cyan, 0.6f);
            rend.material.color = c;
        }
        if (texto != null)
        {
            texto.text = esInocente ? $"{etiqueta}\n(inocente)" : $"{etiqueta}\nV {Verguenza:0}";
            var cam = Camera.main;
            if (cam != null) texto.transform.rotation = cam.transform.rotation;
        }
    }
}
