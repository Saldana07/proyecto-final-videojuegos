using System.Collections.Generic;
using UnityEngine;

// Bebedero de la plazoleta: mientras el jugador está dentro de su zona (trigger)
// rellena una carga cada 'segundosPorRelleno' de cada herramienta que lo permita (el agua).
[RequireComponent(typeof(Collider))]
public class Bebedero : MonoBehaviour
{
    readonly Dictionary<Herramienta, float> acumulado = new();

    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void OnTriggerStay(Collider other)
    {
        var cinturon = other.GetComponentInParent<CinturonHerramientas>();
        if (cinturon != null) Rellenar(cinturon, Time.deltaTime);
    }

    void OnTriggerExit(Collider other)
    {
        var cinturon = other.GetComponentInParent<CinturonHerramientas>();
        if (cinturon == null) return;
        foreach (var h in cinturon.Herramientas) acumulado.Remove(h);
    }

    // Público para poder probarlo sin física.
    public void Rellenar(CinturonHerramientas cinturon, float segundos)
    {
        foreach (var h in cinturon.Herramientas)
        {
            if (h == null || h.datos == null || h.datos.segundosPorRelleno <= 0f || !h.UsaMunicion) continue;
            if (h.Municion >= h.datos.municionMax) { acumulado.Remove(h); continue; }

            acumulado.TryGetValue(h, out float t);
            t += segundos;
            int cargas = Mathf.FloorToInt(t / h.datos.segundosPorRelleno);
            if (cargas > 0)
            {
                t -= cargas * h.datos.segundosPorRelleno;
                if (h.Rellenar(cargas) > 0 && h.datos.sonidoRelleno != null)
                    AudioSource.PlayClipAtPoint(h.datos.sonidoRelleno, transform.position, h.datos.volumen);
            }
            acumulado[h] = t;
        }
    }
}
