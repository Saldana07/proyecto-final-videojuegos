using System.Collections.Generic;
using UnityEngine;

// Megáfono (GDD 8, tecla 2): cono medio de 5 m y 45°, +60 Vergüenza a todos
// los que queden dentro, recarga de 10 s. Un cono también puede atrapar inocentes.
// Detección: Physics.OverlapSphere del alcance + filtro por ángulo en el plano.
public class Megafono : Herramienta
{
    public static List<Collider> EnCono(Vector3 origen, Vector3 direccion, float alcance, float apertura, int mascara)
    {
        var dentro = new List<Collider>();
        float mitad = apertura * 0.5f;
        foreach (var c in Physics.OverlapSphere(origen, alcance, mascara, QueryTriggerInteraction.Collide))
        {
            Vector3 hacia = c.bounds.center - origen;
            hacia.y = 0f;
            // Pegado al jugador cuenta siempre; si no, debe estar dentro del ángulo.
            if (hacia.sqrMagnitude < 0.25f || Vector3.Angle(direccion, hacia) <= mitad)
                dentro.Add(c);
        }
        return dentro;
    }

    protected override void Ejecutar(Vector3 origen, Vector3 direccion, GameObject usuario)
    {
        // El cono es horizontal: no importa si se mira un poco arriba o abajo.
        direccion.y = 0f;
        direccion = direccion.sqrMagnitude > 1e-4f ? direccion.normalized : (usuario != null ? usuario.transform.forward : Vector3.forward);
        var alcanzados = new HashSet<IReceptorHerramienta>();
        foreach (var c in EnCono(origen, direccion, datos.alcance, datos.aperturaCono, datos.MascaraObjetivos))
            Impactar(c, origen, usuario, alcanzados);

        Vector3 piso = new Vector3(origen.x, usuario != null ? usuario.transform.position.y + 0.05f : origen.y, origen.z);
        EfectosHerramienta.Cono(piso, direccion, datos.alcance, datos.aperturaCono, datos.color, 0.25f, datos.materialEfecto);
    }

    void OnDrawGizmosSelected()
    {
        if (datos == null) return;
        Gizmos.color = datos.color;
        Vector3 o = transform.position, f = transform.forward;
        Vector3 izq = Quaternion.AngleAxis(-datos.aperturaCono * 0.5f, Vector3.up) * f * datos.alcance;
        Vector3 der = Quaternion.AngleAxis(datos.aperturaCono * 0.5f, Vector3.up) * f * datos.alcance;
        Gizmos.DrawLine(o, o + izq);
        Gizmos.DrawLine(o, o + der);
        Gizmos.DrawLine(o + izq, o + der);
    }
}
