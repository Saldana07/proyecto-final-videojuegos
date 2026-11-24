using UnityEngine;

// Números de balance de la partida (GDD 5, 6 y 10). Dueño: Héctor.
// Como todo el balance del equipo, vive en un ScriptableObject y no dentro del código.
[CreateAssetMenu(menuName = "No te me colés/Config partida", fileName = "PartidaConfig")]
public class PartidaConfig : ScriptableObject
{
    [Header("Tiempo (GDD 5)")]
    [Tooltip("Segundos reales que dura la partida: 240 s = de 10:00 a 12:00.")]
    public float duracion = 240f;
    [Tooltip("Hora de juego al empezar, en minutos desde la medianoche (10:00 = 600).")]
    public int minutoInicio = 600;
    [Tooltip("Hora de juego al sonar el timbre (12:00 = 720).")]
    public int minutoFin = 720;

    [Header("Paciencia de la fila (GDD 6)")]
    public float pacienciaInicial = 100f;
    [Tooltip("Lo que cuesta atacar a un inocente.")]
    public float penalFalsoPositivo = 15f;
    [Tooltip("Lo que cuesta usar una herramienta sobre alguien que ya está Asegurado.")]
    public float penalSobreAsegurado = 5f;
    [Tooltip("Lo que se recupera al expulsar a un colado que iba a la zona caliente.")]
    public float bonoExpulsadoZonaCaliente = 2f;

    [Header("Escándalo (GDD 9)")]
    [Tooltip("Sube cada vez que un colado se asegura en la zona caliente.")]
    public float escandaloPorInfiltrado = 10f;

    [Header("Dignidad (GDD 6)")]
    public int dignidadExpulsado = 10;
    public int dignidadDesistido = 5;

    [Header("Resultados (GDD 10)")]
    [Tooltip("Puntos de Efectividad que resta cada falso positivo.")]
    public float penalEfectividadPorFalso = 5f;
    [Tooltip("Minutos que tarda cada persona en entrar y servirse.")]
    public float minutosPorPersona = 0.5f;
    [Tooltip("Personas que ya van más adelante, dentro del edificio, y no se ven en la fila. " +
             "Con 29 y el puesto 11 de la fila, el protagonista es el #40 del GDD.")]
    public int personasMasAdelante = 29;

    [Header("Depuración")]
    [Tooltip("F4 acelera el tiempo x3 y F5 termina la partida (solo en el editor o en builds de desarrollo).")]
    public bool teclasDeDepuracion = true;
}
