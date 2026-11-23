using UnityEngine;

// Lo que viaja de una herramienta a quien la recibe. Los números salen del
// HerramientaData de la herramienta, así Saldaña no tiene que copiarlos.
public struct ImpactoHerramienta
{
    public TipoHerramienta tipo;
    public float verguenza;       // cuánta Vergüenza suma (megáfono +60, cámara +30...)
    public float frenarSegundos;  // freno para quien NO es débil (agua: 1 s)
    public Vector3 origen;        // desde dónde se usó (para que el colado huya en sentido contrario)
    public GameObject usuario;    // el protagonista
}

// Todo lo que puede ser alcanzado por una herramienta implementa esta interfaz.
// El Estudiante de Saldaña solo necesita implementar RecibirHerramienta(TipoHerramienta),
// que es la firma del contrato 14.4; si quiere los números completos implementa también
// la versión con ImpactoHerramienta.
public interface IReceptorHerramienta
{
    void RecibirHerramienta(TipoHerramienta tipo);

    void RecibirHerramienta(ImpactoHerramienta impacto) { RecibirHerramienta(impacto.tipo); }
}

// Nombres de las capas físicas del proyecto (Project Settings > Tags and Layers).
public static class Capas
{
    public const string Estudiante = "Estudiante";
    public const string Jugador = "Jugador";

    public static int MascaraEstudiante => LayerMask.GetMask(Estudiante);
    public static int MascaraJugador => LayerMask.GetMask(Jugador);
}
