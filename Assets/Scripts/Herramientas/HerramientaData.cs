using UnityEngine;

// Todos los números de una herramienta (GDD secciones 8 y 15.1).
// Se ajustan entre playtests desde el Inspector sin recompilar.
[CreateAssetMenu(menuName = "No te me colés/Herramienta", fileName = "Herramienta")]
public class HerramientaData : ScriptableObject
{
    public TipoHerramienta tipo;
    public string nombre = "Herramienta";
    [Tooltip("Para el HUD de Héctor.")]
    public Sprite icono;
    public Color color = Color.white;

    [Header("Alcance")]
    [Tooltip("Metros. Agua: 3, Megáfono: 5.")]
    public float alcance = 3f;
    [Tooltip("Apertura TOTAL del cono en grados (solo megáfono: 45).")]
    [Range(0f, 360f)] public float aperturaCono = 45f;
    [Tooltip("Grosor del chorro para el SphereCast (solo agua).")]
    public float radioRayo = 0.35f;
    [Tooltip("Capas que puede alcanzar. Si queda en Nothing se usa la capa Estudiante.")]
    public LayerMask capaObjetivos;

    [Header("Recarga y munición")]
    [Tooltip("Segundos entre usos. Megáfono: 10.")]
    public float recarga = 0.5f;
    [Tooltip("Cargas máximas. 0 = munición infinita. Agua: 6.")]
    public int municionMax = 0;
    [Tooltip("Segundos parado en el bebedero por cada carga. 0 = no se rellena ahí.")]
    public float segundosPorRelleno = 0f;

    [Header("Efecto sobre el estudiante")]
    [Tooltip("Vergüenza que suma (0–100). Megáfono: 60.")]
    public float verguenza = 0f;
    [Tooltip("Segundos que frena a quien no es débil. Agua: 1.")]
    public float frenarSegundos = 0f;

    [Header("Feedback")]
    public AudioClip sonidoUso;
    public AudioClip sonidoVacio;
    public AudioClip sonidoRelleno;
    [Range(0f, 1f)] public float volumen = 1f;
    [Tooltip("Material del efecto provisional (línea del chorro / cono).")]
    public Material materialEfecto;
    [Tooltip("Opcional: prefab de partículas que se crea al usarla.")]
    public GameObject efectoPrefab;

    public int MascaraObjetivos => capaObjetivos.value != 0 ? capaObjetivos.value : Capas.MascaraEstudiante;
}
