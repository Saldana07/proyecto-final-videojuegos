using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// La fila del restaurante (GDD 4.1). Dueño: Saldaña.
// Lista de puestos frente a la puerta. Un hueco = puesto vacío con alguien detrás: es lo que
// buscan los colados. Cada cierto tiempo alguien va al baño (hueco temporal) o se cansa y se
// va; si un hueco queda solo mucho rato, la fila se compacta y lo cierra.
public class Fila : MonoBehaviour
{
    public static Fila Instancia { get; private set; }

    [Header("Forma (GDD 4.1)")]
    public int cantidadPuestos = 20;
    [Tooltip("Metros entre un puesto y el siguiente.")]
    public float separacion = 1.3f;
    [Tooltip("Hacia dónde crece la fila desde el primer puesto (el frente, junto a la puerta).")]
    public Vector3 direccion = Vector3.right;
    [Tooltip("Lado por donde llegan los colados (la plazoleta).")]
    public Vector3 ladoPlazoleta = Vector3.back;
    [Tooltip("Puesto reservado del protagonista (0 = el primero). Los de adelante son la zona caliente.")]
    public int puestoJugador = 10;

    [Header("Al empezar")]
    public Estudiante prefabEstudiante;
    public ConfigEstudiantes config;
    [Tooltip("Cuántos puestos arrancan ocupados por inocentes, desde el frente.")]
    public int ocupadosAlEmpezar = 14;
    [Tooltip("Puestos que arrancan vacíos: los primeros huecos.")]
    public int[] huecosIniciales = { 6 };

    [Header("Huecos temporales")]
    public Transform puntoBano;
    [Tooltip("Cada cuántos segundos (mín, máx) alguien deja su puesto.")]
    public Vector2 intervaloHuecos = new Vector2(10f, 18f);
    public float segundosEnBano = 6f;
    [Tooltip("Probabilidad de que el que sale se vaya para siempre en vez de ir al baño.")]
    [Range(0f, 1f)] public float probabilidadAbandono = 0.3f;
    [Tooltip("Segundos que un hueco sin dueño queda abierto antes de que la fila se compacte.")]
    public float segundosAntesDeCompactar = 10f;

    [Header("Marcadores")]
    [Tooltip("Material del disco que marca los huecos (GDD 4.1: los huecos son visibles).")]
    public Material materialHueco;

    class Puesto
    {
        public Vector3 pos;
        public Estudiante ocupante;   // quien está parado ahí
        public Estudiante reserva;    // quien va camino a ocuparlo
        public Estudiante ausente;    // su dueño fue al baño
        public float vacioDesde = -1f;
        public GameObject marcador;
    }

    readonly List<Puesto> puestos = new List<Puesto>();

    public int Cantidad => puestos.Count;
    public Vector3 Direccion => Plano(direccion, Vector3.right);
    public Vector3 LadoPlazoleta => Plano(ladoPlazoleta, Vector3.back);

    void Awake()
    {
        Instancia = this;
        for (int i = 0; i < Mathf.Max(1, cantidadPuestos); i++)
        {
            var p = new Puesto { pos = PosicionTeorica(i) };
            if (materialHueco != null)
            {
                var m = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                m.name = $"Hueco {i + 1}";
                Destroy(m.GetComponent<Collider>());
                m.transform.SetParent(transform, true);
                m.transform.position = p.pos + Vector3.up * 0.03f;
                m.transform.localScale = new Vector3(0.9f, 0.01f, 0.9f);
                m.GetComponent<Renderer>().sharedMaterial = materialHueco;
                m.SetActive(false);
                p.marcador = m;
            }
            puestos.Add(p);
        }
    }

    void OnDestroy()
    {
        if (Instancia == this) Instancia = null;
    }

    void Start()
    {
        int n = Mathf.Min(ocupadosAlEmpezar, puestos.Count);
        for (int i = 0; i < n; i++)
        {
            if (System.Array.IndexOf(huecosIniciales, i) >= 0)
            {
                puestos[i].vacioDesde = Time.time;
                continue;
            }
            if (prefabEstudiante == null) break;
            var e = Instantiate(prefabEstudiante, puestos[i].pos, Quaternion.LookRotation(-Direccion), transform);
            e.name = $"En fila {i + 1}";
            e.config = config;
            e.ColocarEnFila(i);
        }
        StartCoroutine(RutinaHuecos());
    }

    void Update()
    {
        int ultimo = UltimoOcupado();
        for (int i = 0; i < puestos.Count; i++)
        {
            var p = puestos[i];
            bool vacio = p.ocupante == null && i < ultimo;
            if (p.marcador != null && p.marcador.activeSelf != vacio) p.marcador.SetActive(vacio);
        }
        for (int i = 0; i < ultimo; i++)
        {
            var p = puestos[i];
            if (p.ocupante == null && p.reserva == null && p.ausente == null && p.vacioDesde >= 0f &&
                Time.time - p.vacioDesde > segundosAntesDeCompactar)
            {
                Compactar(i);
                break;
            }
        }
    }

    // =============================== Lo que usan los estudiantes ===============================

    public Vector3 Posicion(int i) => puestos[Mathf.Clamp(i, 0, puestos.Count - 1)].pos;
    public bool EsZonaCaliente(int i) => i >= 0 && i < puestoJugador;
    public bool EsHueco(int i) =>
        i >= 0 && i < puestos.Count && puestos[i].ocupante == null && puestos[i].reserva == null && i < UltimoOcupado();

    // El colado elige el hueco libre más cercano y lo aparta. -1 = no hay huecos.
    public int ReservarHueco(Estudiante e, Vector3 desde)
    {
        int mejor = -1;
        float mejorDist = float.MaxValue;
        int ultimo = UltimoOcupado();
        for (int i = 0; i < ultimo; i++)
        {
            var p = puestos[i];
            if (p.ocupante != null || p.reserva != null) continue;
            float d = (p.pos - desde).sqrMagnitude;
            if (d < mejorDist) { mejorDist = d; mejor = i; }
        }
        if (mejor >= 0) puestos[mejor].reserva = e;
        return mejor;
    }

    // El que llega legal va al final. -1 = la fila está llena.
    public int ReservarFinal(Estudiante e)
    {
        int i = UltimoOcupado() + 1;
        if (i >= puestos.Count) return -1;
        puestos[i].reserva = e;
        return i;
    }

    public void Ocupar(int i, Estudiante e)
    {
        var p = puestos[i];
        p.ocupante = e;
        if (p.reserva == e) p.reserva = null;
        if (p.ausente == e) p.ausente = null;
        p.vacioDesde = -1f;
    }

    public void Liberar(Estudiante e)
    {
        foreach (var p in puestos)
        {
            if (p.ocupante == e) { p.ocupante = null; p.vacioDesde = Time.time; }
            if (p.reserva == e) { p.reserva = null; p.vacioDesde = Time.time; }
            if (p.ausente == e) p.ausente = null;
        }
    }

    public void SalirTemporal(Estudiante e, int i)
    {
        var p = puestos[i];
        if (p.ocupante == e) p.ocupante = null;
        p.ausente = e;
        p.vacioDesde = Time.time;
    }

    // Vuelve del baño: su puesto si sigue libre; si un colado se lo quitó, el final.
    public int Regresar(Estudiante e, int i)
    {
        var p = puestos[i];
        if (p.ausente == e) p.ausente = null;
        if (p.ocupante == null && p.reserva == null)
        {
            p.reserva = e;
            return i;
        }
        return ReservarFinal(e);
    }

    // Un punto en la plazoleta, frente a la fila, para dar vueltas antes de colarse.
    public Vector3 PuntoMerodeo()
    {
        int ultimo = Mathf.Max(0, UltimoOcupado());
        Vector3 baseFila = Posicion(Random.Range(0, ultimo + 1));
        return baseFila + LadoPlazoleta * Random.Range(3f, 6f) + Direccion * Random.Range(-2f, 2f);
    }

    // =============================== Interno ===============================

    int UltimoOcupado()
    {
        for (int i = puestos.Count - 1; i >= 0; i--)
        {
            var p = puestos[i];
            if (p.ocupante != null || p.reserva != null || p.ausente != null) return i;
        }
        return -1;
    }

    void Compactar(int hueco)
    {
        // Si alguien de atrás va en camino o está en el baño, se espera para no moverle el puesto.
        for (int j = hueco + 1; j < puestos.Count; j++)
        {
            if (puestos[j].reserva != null || puestos[j].ausente != null)
            {
                puestos[hueco].vacioDesde = Time.time;
                return;
            }
        }
        for (int j = hueco; j < puestos.Count - 1; j++)
        {
            var siguiente = puestos[j + 1].ocupante;
            puestos[j].ocupante = siguiente;
            puestos[j].vacioDesde = siguiente == null ? Time.time : -1f;
            if (siguiente != null) siguiente.MoverAPuesto(j);
        }
        var ultimo = puestos[puestos.Count - 1];
        ultimo.ocupante = null;
        ultimo.vacioDesde = -1f;
    }

    IEnumerator RutinaHuecos()
    {
        var candidatos = new List<Estudiante>();
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(intervaloHuecos.x, intervaloHuecos.y));
            int ultimo = UltimoOcupado();
            candidatos.Clear();
            for (int i = 0; i < ultimo; i++)
            {
                var o = puestos[i].ocupante;
                if (o != null && o.EsInocente && o.Estado == EstadoEstudiante.Asegurado) candidatos.Add(o);
            }
            if (candidatos.Count == 0) continue;
            var elegido = candidatos[Random.Range(0, candidatos.Count)];
            if (Random.value < probabilidadAbandono) elegido.Abandonar();
            else elegido.IrAlBano(puntoBano != null ? puntoBano.position : transform.position + LadoPlazoleta * 8f, segundosEnBano);
        }
    }

    Vector3 PosicionTeorica(int i) => transform.position + Direccion * separacion * i;

    static Vector3 Plano(Vector3 v, Vector3 siNo)
    {
        v.y = 0f;
        return v.sqrMagnitude > 1e-4f ? v.normalized : siNo;
    }

    void OnDrawGizmos()
    {
        for (int i = 0; i < cantidadPuestos; i++)
        {
            Gizmos.color = i < puestoJugador ? new Color(1f, 0.3f, 0.2f) : Color.white;
            Gizmos.DrawWireSphere(PosicionTeorica(i) + Vector3.up * 0.9f, 0.35f);
        }
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(PosicionTeorica(0), PosicionTeorica(0) + LadoPlazoleta * 3f);
    }
}
