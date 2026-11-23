using UnityEngine;

// Números de balance comunes a todos los estudiantes (GDD 6 y 15.1).
[CreateAssetMenu(menuName = "No te me colés/Config estudiantes", fileName = "ConfigEstudiantes")]
public class ConfigEstudiantes : ScriptableObject
{
    [Header("Vergüenza (GDD 6)")]
    [Tooltip("Cuánto baja por segundo si dejas de presionar.")]
    public float decaimientoVerguenza = 10f;

    [Header("Merodeo")]
    [Tooltip("Segundos (mín, máx) que da vueltas antes de buscar un hueco.")]
    public float merodeoMin = 1.5f;
    public float merodeoMax = 4f;
    [Tooltip("Si no encuentra hueco en este tiempo, desiste.")]
    public float esperaMaxSinHueco = 8f;
    [Tooltip("Si el protagonista está a menos de esto en Merodeo, desiste. 0 = nunca.")]
    public float radioDisuasionMerodeo = 2.5f;

    [Header("Aproximación (GDD 15.1)")]
    [Tooltip("Segundos mínimos en Aproximación mostrando el tell antes del Intento (rango a probar 2–5).")]
    public float tiempoAproximacion = 3f;
    [Tooltip("A cuántos metros del hueco se detiene antes de meterse.")]
    public float distanciaIntento = 2f;
    [Tooltip("Si el protagonista está encima (menos de esto) en Aproximación, desiste. 0 = nunca.")]
    public float radioDisuasionAproximacion = 1f;

    [Header("Salida")]
    public float velocidadHuida = 5f;
    [Tooltip("Segundos hasta desaparecer después de ser expulsado o desistir.")]
    public float segundosParaDesaparecer = 4f;

    [Header("Inocentes")]
    public float velocidadInocente = 1.6f;

    [Header("Depuración")]
    [Tooltip("Muestra facultad, estado y Vergüenza encima de cada uno. Apágalo para el playtest real.")]
    public bool mostrarEtiqueta = true;
}
