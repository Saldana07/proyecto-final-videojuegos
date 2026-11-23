using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Analiza el Edificio_Univalle de la SampleScene para ubicar cosas donde se puede caminar.
// Recorre una cuadrícula sobre el edificio, lanza rayos hacia abajo y guarda cada
// superficie plana con espacio libre para una persona (un "punto caminable").
public static class AnalisisEdificio
{
    public struct Punto
    {
        public Vector3 pos;
        public bool techado;   // tiene algo encima (está dentro del edificio)
    }

    public const float Paso = 1f;
    const float AlturaPersona = 1.8f;
    const float Radio = 0.35f;

    public static Bounds LimitesEdificio()
    {
        var edificio = GameObject.Find("Edificio_Univalle");
        var b = new Bounds();
        bool primero = true;
        if (edificio == null) return b;
        foreach (var r in edificio.GetComponentsInChildren<Renderer>())
        {
            if (primero) { b = r.bounds; primero = false; } else b.Encapsulate(r.bounds);
        }
        return b;
    }

    // Puntos caminables en todos los pisos.
    public static List<Punto> PuntosCaminables(Bounds zona, float margen = 6f)
    {
        Physics.SyncTransforms();
        var puntos = new List<Punto>();
        int mascara = ~(Capas.MascaraEstudiante | Capas.MascaraJugador);
        float techoY = zona.max.y + 5f;

        for (float x = zona.min.x - margen; x <= zona.max.x + margen; x += Paso)
        for (float z = zona.min.z - margen; z <= zona.max.z + margen; z += Paso)
        {
            var hits = Physics.RaycastAll(new Vector3(x, techoY, z), Vector3.down, techoY - zona.min.y + 10f, mascara, QueryTriggerInteraction.Ignore);
            if (hits.Length == 0) continue;
            System.Array.Sort(hits, (a, c) => a.distance.CompareTo(c.distance));
            for (int i = 0; i < hits.Length; i++)
            {
                var h = hits[i];
                if (h.normal.y < 0.9f) continue;
                Vector3 p = h.point;
                Vector3 abajo = p + Vector3.up * (Radio + 0.05f);
                Vector3 arriba = p + Vector3.up * (AlturaPersona - Radio);
                if (Physics.CheckCapsule(abajo, arriba, Radio, mascara, QueryTriggerInteraction.Ignore)) continue;
                puntos.Add(new Punto { pos = p, techado = i > 0 });
            }
        }
        return puntos;
    }

    [MenuItem("No te me colés/Carlos/Analizar estructura del edificio")]
    public static void Analizar()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);

        var b = LimitesEdificio();
        var jugador = Object.FindAnyObjectByType<CharacterController>();
        var puntos = PuntosCaminables(b);

        var sb = new StringBuilder();
        sb.AppendLine($"Edificio: centro {b.center} tamaño {b.size} min {b.min} max {b.max}");
        if (jugador != null) sb.AppendLine($"Jugador: {jugador.transform.position} forward {jugador.transform.forward}");
        sb.AppendLine($"Puntos caminables: {puntos.Count} (techados {puntos.Count(p => p.techado)})");

        sb.AppendLine("Niveles (altura redondeada a 0,5 m: cantidad / techados):");
        foreach (var g in puntos.GroupBy(p => Mathf.Round(p.pos.y * 2f) / 2f).OrderBy(g => g.Key))
            sb.AppendLine($"  y={g.Key,6:0.0}: {g.Count(),5} / {g.Count(p => p.techado),5}");

        // Mapa del nivel más bajo: '.' caminable al aire libre, 'o' caminable techado, '#' bloqueado/sin piso
        float suelo = puntos.Count > 0 ? puntos.Min(p => p.pos.y) : 0f;
        var bajos = puntos.Where(p => p.pos.y < suelo + 0.8f).ToList();
        var celdas = new Dictionary<(int, int), char>();
        foreach (var p in bajos)
            celdas[((int)Mathf.Round(p.pos.x), (int)Mathf.Round(p.pos.z))] = p.techado ? 'o' : '.';
        int x0 = Mathf.FloorToInt(b.min.x - 6), x1 = Mathf.CeilToInt(b.max.x + 6);
        int z0 = Mathf.FloorToInt(b.min.z - 6), z1 = Mathf.CeilToInt(b.max.z + 6);
        sb.AppendLine($"Mapa planta baja (y≈{suelo:0.0}). Columnas x {x0}..{x1}, filas z {z1}..{z0} (norte arriba). J = jugador");
        int jx = jugador != null ? Mathf.RoundToInt(jugador.transform.position.x) : int.MinValue;
        int jz = jugador != null ? Mathf.RoundToInt(jugador.transform.position.z) : int.MinValue;
        for (int z = z1; z >= z0; z--)
        {
            var linea = new StringBuilder();
            linea.Append($"{z,5} ");
            for (int x = x0; x <= x1; x++)
                linea.Append(x == jx && z == jz ? 'J' : celdas.TryGetValue((x, z), out char ch) ? ch : '#');
            sb.AppendLine(linea.ToString());
        }

        string ruta = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Logs", "analisis_edificio.txt");
        File.WriteAllText(ruta, sb.ToString());
        Debug.Log("[Carlos] Análisis del edificio en " + ruta);
    }

    public static void AnalizarDesdeConsola()
    {
        Analizar();
        EditorApplication.Exit(0);
    }
}
