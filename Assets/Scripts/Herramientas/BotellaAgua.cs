using UnityEngine;

// Botella de agua (GDD 8, tecla 1): chorro en línea recta de 3 m, 6 cargas que se
// rellenan en el bebedero. Expulsa al instante a quien sea débil al agua y al
// resto lo frena 1 s (eso lo decide el Estudiante según su FacultadData).
// Detección: SphereCast hacia donde apunta; el primer obstáculo sólido corta el chorro,
// así la maqueta de Artes puede bloquear el tiro.
public class BotellaAgua : Herramienta
{
    [Tooltip("Qué detiene el chorro además de los estudiantes. Se ignora siempre la capa Jugador.")]
    public LayerMask obstaculos = ~0;

    // Devuelve el collider alcanzado (o null) y el punto donde termina el chorro.
    public static Collider Chorro(Vector3 origen, Vector3 direccion, float alcance, float radio,
                                  int mascaraObjetivos, int mascaraObstaculos, out Vector3 fin)
    {
        fin = origen + direccion * alcance;
        int mascara = (mascaraObjetivos | mascaraObstaculos) & ~Capas.MascaraJugador;
        var hits = Physics.SphereCastAll(origen, radio, direccion, alcance, mascara, QueryTriggerInteraction.Collide);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var h in hits)
        {
            bool esObjetivo = ((1 << h.collider.gameObject.layer) & mascaraObjetivos) != 0
                              && h.collider.GetComponentInParent<IReceptorHerramienta>() != null;
            if (esObjetivo)
            {
                fin = h.distance > 0f ? h.point : h.collider.bounds.center;
                return h.collider;
            }
            if (h.collider.isTrigger) continue;   // bebederos, zonas: el agua pasa
            fin = h.distance > 0f ? h.point : origen;
            return null;                          // pared, columna, maqueta...
        }
        return null;
    }

    protected override void Ejecutar(Vector3 origen, Vector3 direccion, GameObject usuario)
    {
        var col = Chorro(origen, direccion, datos.alcance, datos.radioRayo, datos.MascaraObjetivos, obstaculos.value, out Vector3 fin);
        if (col != null) Impactar(col, origen, usuario);

        // En primera persona el origen es el ojo: el chorro visible sale un poco más abajo, "de la mano".
        Vector3 mano = origen + direccion * 0.4f + Vector3.down * 0.3f;
        EfectosHerramienta.Linea(new[] { mano, fin }, datos.color, datos.radioRayo * 0.6f, 0.15f, datos.materialEfecto);
    }

    void OnDrawGizmosSelected()
    {
        if (datos == null) return;
        Gizmos.color = datos.color;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * datos.alcance);
    }
}
