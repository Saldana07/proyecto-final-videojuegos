using UnityEngine;

// Fórmulas de la pantalla de resultados (GDD 10). Dueño: Héctor.
// Son funciones puras para poder probarlas sin escena.
public static class Puntaje
{
    public const string RangoDerrota = "Expulsado";

    // E = frustrados / (frustrados + infiltrados) × 100 − 5 · falsos positivos, entre 0 y 100.
    // Si no llegó ningún colado no hubo nada que defender: base 100.
    public static float Efectividad(int frustrados, int infiltrados, int falsosPositivos, float penalPorFalso = 5f)
    {
        int total = frustrados + infiltrados;
        float baseE = total <= 0 ? 100f : 100f * frustrados / total;
        return Mathf.Clamp(baseE - penalPorFalso * falsosPositivos, 0f, 100f);
    }

    // GDD 10.3: S ≥ 95 % y 0 falsos positivos; A ≥ 85; B ≥ 70; C ≥ 50; D < 50; Expulsado si la Paciencia llegó a 0.
    public static string Rango(float efectividad, int falsosPositivos, bool derrota)
    {
        if (derrota) return RangoDerrota;
        if (efectividad >= 95f && falsosPositivos == 0) return "S";
        if (efectividad >= 85f) return "A";
        if (efectividad >= 70f) return "B";
        if (efectividad >= 50f) return "C";
        return "D";
    }

    public static int DignidadBonus(string rango)
    {
        switch (rango)
        {
            case "S": return 100;
            case "A": return 60;
            case "B": return 30;
            case "C": return 10;
            default: return 0;
        }
    }

    // T = minutosPorPersona · (puesto + infiltrados delante): minutos después de las 12:00.
    public static float MinutosAlmuerzo(int puesto, int infiltradosDelante, float minutosPorPersona = 0.5f) =>
        minutosPorPersona * (Mathf.Max(0, puesto) + Mathf.Max(0, infiltradosDelante));

    // 600 → "10:00 a. m.", 750 → "12:30 p. m.", 790 → "1:10 p. m."
    public static string Hora(float minutosDelDia)
    {
        int total = Mathf.FloorToInt(minutosDelDia + 0.0001f);
        int h24 = (total / 60) % 24, m = total % 60;
        int h12 = h24 % 12 == 0 ? 12 : h24 % 12;
        return $"{h12}:{m:00} {(h24 < 12 ? "a. m." : "p. m.")}";
    }

    // Hora de juego durante la partida: 0 s → 10:00, 240 s → 12:00.
    public static float MinutoDeJuego(float segundos, float duracion, int minutoInicio, int minutoFin)
    {
        float t = duracion > 0f ? Mathf.Clamp01(segundos / duracion) : 1f;
        return Mathf.Lerp(minutoInicio, minutoFin, t);
    }

    public static Color ColorRango(string rango)
    {
        switch (rango)
        {
            case "S": return new Color(1f, 0.82f, 0.2f);
            case "A": return new Color(0.35f, 0.85f, 0.4f);
            case "B": return new Color(0.35f, 0.7f, 1f);
            case "C": return new Color(0.95f, 0.6f, 0.25f);
            default: return new Color(0.95f, 0.3f, 0.3f);
        }
    }
}
