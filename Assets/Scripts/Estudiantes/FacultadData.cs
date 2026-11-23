using UnityEngine;

// Una facultad = un acertijo (GDD 7). Todos los números se ajustan desde el Inspector.
[CreateAssetMenu(menuName = "No te me colés/Facultad", fileName = "Facultad")]
public class FacultadData : ScriptableObject
{
    public string nombre = "Facultad";
    [TextArea] public string tell;
    [TextArea] public string tactica;
    [Tooltip("Color del objeto del tell (el cuerpo de todos es apagado: la facultad se reconoce por el tell).")]
    public Color colorAcento = Color.white;
    [Tooltip("Objeto que aparece en Aproximación: celular y audífonos, maqueta...")]
    public GameObject tellPrefab;

    [Header("Herramientas (de Carlos)")]
    [Tooltip("La herramienta que lo expulsa al instante.")]
    public TipoHerramienta counter = TipoHerramienta.Megafono;
    [Tooltip("La herramienta que no le hace nada.")]
    public TipoHerramienta resistencia = TipoHerramienta.Agua;

    [Header("Ritmo")]
    [Tooltip("Segundos que tarda en meterse al hueco (la última oportunidad). Sistemas 2, Artes 2,5.")]
    public float tiempoIntento = 2f;
    [Tooltip("Metros por segundo caminando.")]
    public float velocidad = 1.8f;
    [Tooltip("Día de la semana en que aparece (1 = lunes).")]
    public int diaAparicion = 1;
}
