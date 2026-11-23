using UnityEngine;

// Le dice al CinturonHerramientas desde dónde y hacia dónde se usan las herramientas.
// Así las herramientas no dependen de la cámara (primera persona hoy, otra mañana).
public interface IApuntadorJugador
{
    Vector3 OrigenHerramientas { get; }
    Vector3 DireccionApuntado { get; }
    // Falso cuando el mouse no está capturado (el clic es para capturarlo, no para disparar).
    bool PuedeUsarHerramientas { get; }
}
