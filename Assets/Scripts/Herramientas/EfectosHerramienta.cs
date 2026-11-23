using UnityEngine;

// Feedback visual provisional (prototipo gris): líneas que duran un instante.
// Héctor lo reemplaza por partículas con HerramientaData.efectoPrefab.
public static class EfectosHerramienta
{
    static Material porDefecto;
    static MaterialPropertyBlock bloque;
    static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    static readonly int ColorId = Shader.PropertyToID("_Color");

    public static void Linea(Vector3[] puntos, Color color, float ancho, float duracion, Material material = null)
    {
        var go = new GameObject("Efecto herramienta");
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.positionCount = puntos.Length;
        lr.SetPositions(puntos);
        lr.widthMultiplier = ancho;
        lr.numCapVertices = 2;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.sharedMaterial = material != null ? material : MaterialPorDefecto();
        lr.startColor = lr.endColor = color;

        bloque ??= new MaterialPropertyBlock();
        bloque.SetColor(BaseColor, color);
        bloque.SetColor(ColorId, color);
        lr.SetPropertyBlock(bloque);

        Object.Destroy(go, duracion);
    }

    // Abanico (contorno del cono del megáfono) sobre el piso.
    public static void Cono(Vector3 origen, Vector3 direccion, float alcance, float apertura, Color color, float duracion, Material material = null)
    {
        const int segmentos = 12;
        var puntos = new Vector3[segmentos + 3];
        Vector3 piso = origen;
        puntos[0] = piso;
        for (int i = 0; i <= segmentos; i++)
        {
            float a = Mathf.Lerp(-apertura * 0.5f, apertura * 0.5f, i / (float)segmentos);
            puntos[i + 1] = piso + Quaternion.AngleAxis(a, Vector3.up) * direccion * alcance;
        }
        puntos[segmentos + 2] = piso;
        Linea(puntos, color, 0.08f, duracion, material);
    }

    static Material MaterialPorDefecto()
    {
        if (porDefecto != null) return porDefecto;
        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        porDefecto = new Material(shader) { name = "Efecto herramienta (auto)" };
        return porDefecto;
    }
}
