using UnityEngine;
using UnityEngine.UI;

// Ayudas para armar la interfaz desde código (uGUI con la fuente incluida en Unity),
// sin prefabs ni escenas que puedan chocar en Git. Dueño: Héctor.
public static class UIFabrica
{
    static Font fuente;
    static Sprite blanco, circulo, flecha;

    public static Font Fuente => fuente != null ? fuente : (fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));

    public static Sprite Blanco => blanco != null ? blanco : (blanco = CrearSprite(8, (x, y) => 1f));

    public static Sprite Circulo => circulo != null ? circulo : (circulo = CrearSprite(128, (x, y) =>
        Mathf.Clamp01((1f - Mathf.Sqrt(x * x + y * y)) * 40f)));

    // Triángulo que apunta a la derecha (+x).
    public static Sprite Flecha => flecha != null ? flecha : (flecha = CrearSprite(64, (x, y) =>
    {
        float ancho = (1f - x) * 0.5f;              // 1 en la base (x = −1), 0 en la punta (x = 1)
        return Mathf.Clamp01((ancho - Mathf.Abs(y)) * 30f) * Mathf.Clamp01((x + 0.9f) * 30f);
    }));

    static Sprite CrearSprite(int n, System.Func<float, float, float> alfa)
    {
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "UI (Héctor)" };
        var px = new Color32[n * n];
        for (int j = 0; j < n; j++)
            for (int i = 0; i < n; i++)
            {
                float x = (i + 0.5f) / n * 2f - 1f, y = (j + 0.5f) / n * 2f - 1f;
                px[j * n + i] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(alfa(x, y)) * 255));
            }
        tex.SetPixels32(px);
        tex.Apply(false, true);
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
    }

    public static RectTransform Nodo(string nombre, Transform padre, Vector2 ancla, Vector2 pivote, Vector2 pos, Vector2 tam)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(padre, false);
        rt.anchorMin = rt.anchorMax = ancla;
        rt.pivot = pivote;
        rt.anchoredPosition = pos;
        rt.sizeDelta = tam;
        return rt;
    }

    // Ocupa todo el padre (con un margen opcional).
    public static RectTransform Estirar(RectTransform rt, float margen = 0f)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(margen, margen);
        rt.offsetMax = new Vector2(-margen, -margen);
        return rt;
    }

    public static Image Imagen(RectTransform rt, Color color, Sprite sprite = null)
    {
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite != null ? sprite : Blanco;
        img.color = color;
        img.raycastTarget = false;   // la interfaz no debe tapar el clic de las herramientas
        return img;
    }

    public static Image Barra(RectTransform rt, Color color)
    {
        var img = Imagen(rt, color);
        img.type = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Horizontal;
        img.fillOrigin = 0;
        return img;
    }

    public static Text Texto(RectTransform rt, string s, int tam, TextAnchor alineacion, Color color,
                             FontStyle estilo = FontStyle.Normal, bool sombra = true)
    {
        var t = rt.gameObject.AddComponent<Text>();
        t.font = Fuente;
        t.text = s;
        t.fontSize = tam;
        t.alignment = alineacion;
        t.color = color;
        t.fontStyle = estilo;
        t.supportRichText = true;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        if (sombra)
        {
            var s2 = rt.gameObject.AddComponent<Shadow>();
            s2.effectColor = new Color(0f, 0f, 0f, 0.75f);
            s2.effectDistance = new Vector2(2f, -2f);
        }
        return t;
    }

    public static Button Boton(Transform padre, string texto, Vector2 pos, Vector2 tam, Color color, System.Action alPulsar)
    {
        var rt = Nodo("Botón " + texto, padre, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, tam);
        var img = Imagen(rt, color);
        img.raycastTarget = true;
        var b = rt.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        var colores = b.colors;
        colores.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
        colores.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        b.colors = colores;
        b.onClick.AddListener(() => alPulsar());
        Texto(Estirar(Nodo("Texto", rt, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero)), texto, 30,
              TextAnchor.MiddleCenter, Color.white, FontStyle.Bold);
        return b;
    }

    public static string Hex(Color c) => ColorUtility.ToHtmlStringRGB(c);
}
