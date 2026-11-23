using UnityEngine;

// Números de balance del protagonista (GDD 14.4: nunca dentro del código).
[CreateAssetMenu(menuName = "No te me colés/Configuración del jugador", fileName = "JugadorConfig")]
public class JugadorConfig : ScriptableObject
{
    [Tooltip("m/s. GDD 15.1: 4 m/s, rango a probar 3–5.")]
    public float velocidad = 4f;
    [Tooltip("m/s² para arrancar y frenar.")]
    public float aceleracion = 30f;
    [Tooltip("Grados por segundo al girar hacia donde apunta el mouse.")]
    public float velocidadGiro = 900f;
    public float gravedad = -20f;
    [Tooltip("Altura desde los pies a la que salen las herramientas.")]
    public float alturaManos = 1f;
}
