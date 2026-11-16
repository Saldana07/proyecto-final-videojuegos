using UnityEngine;

// HUD PROVISIONAL para probar las herramientas mientras llega el HUD de Héctor.
// Muestra las herramientas del cinturón, la elegida, la munición y la recarga.
[RequireComponent(typeof(CinturonHerramientas))]
public class HUDHerramientasPrueba : MonoBehaviour
{
    CinturonHerramientas cinturon;
    GUIStyle estilo;

    void Awake() => cinturon = GetComponent<CinturonHerramientas>();

    void OnGUI()
    {
        estilo ??= new GUIStyle(GUI.skin.box) { fontSize = 16, alignment = TextAnchor.MiddleLeft, richText = true };

        float ancho = 260f, alto = 30f, x = 16f, y = Screen.height - 16f - alto * (cinturon.Herramientas.Count + 1);
        GUI.Box(new Rect(x, y, ancho, alto), "<b>Herramientas</b>  (1–5 / rueda, clic)", estilo);

        for (int i = 0; i < cinturon.Herramientas.Count; i++)
        {
            var h = cinturon.Herramientas[i];
            if (h == null || h.datos == null) continue;
            string marca = h == cinturon.Actual ? "▶ " : "   ";
            string municion = h.UsaMunicion ? $"  {h.Municion}/{h.datos.municionMax}" : "";
            string estado = h.Congelada ? "  <color=#7cf>congelada</color>"
                          : h.EnRecarga ? $"  <color=#fc6>{h.SegundosRestantes:0.0} s</color>"
                          : !h.TieneMunicion ? "  <color=#f66>vacía: ve al bebedero</color>"
                          : "  <color=#8f8>lista</color>";
            GUI.Box(new Rect(x, y + alto * (i + 1), ancho, alto), $"{marca}{i + 1}. {h.datos.nombre}{municion}{estado}", estilo);
        }
    }
}
