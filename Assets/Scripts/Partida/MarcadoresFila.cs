using UnityEngine;

// Arte de la fila (GDD 4.1 y 12.3), para que se lea a simple vista desde la cámara isométrica:
// la zona caliente (los puestos delante del protagonista) pintada en el piso, el letrero
// "Tu puesto" y el del restaurante. Lee la Fila de Saldaña; no la modifica. Dueño: Héctor.
public class MarcadoresFila : MonoBehaviour
{
    public Fila fila;
    public PartidaManager partida;
    [Tooltip("Material transparente para la franja de la zona caliente.")]
    public Material materialZona;
    public Color colorTexto = new Color(1f, 0.85f, 0.3f);
    public float anchoFranja = 1.4f;

    void Start()
    {
        if (fila == null) fila = Fila.Instancia != null ? Fila.Instancia : FindAnyObjectByType<Fila>();
        if (partida == null) partida = FindAnyObjectByType<PartidaManager>();
        if (fila == null) return;

        Vector3 dir = fila.Direccion, lado = fila.LadoPlazoleta;
        float sep = fila.separacion;
        int puesto = Mathf.Clamp(fila.puestoJugador, 0, fila.Cantidad - 1);

        // Franja de la zona caliente: del frente de la fila hasta justo antes del puesto del jugador.
        if (puesto > 0)
        {
            Vector3 a = fila.Posicion(0) - dir * sep * 0.5f;
            Vector3 b = fila.Posicion(puesto) - dir * sep * 0.5f;
            var franja = GameObject.CreatePrimitive(PrimitiveType.Quad);
            franja.name = "Zona caliente";
            Destroy(franja.GetComponent<Collider>());
            franja.transform.SetParent(transform, false);
            franja.transform.position = (a + b) * 0.5f + Vector3.up * 0.02f;
            franja.transform.rotation = Quaternion.LookRotation(Vector3.down, dir);   // acostada en el piso
            franja.transform.localScale = new Vector3(anchoFranja, Vector3.Distance(a, b), 1f);
            var r = franja.GetComponent<Renderer>();
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            if (materialZona != null) r.sharedMaterial = materialZona;

            Letrero("ZONA CALIENTE", (a + b) * 0.5f + lado * (anchoFranja * 0.5f + 0.6f), 0.11f);
            Letrero("si se cuelan aquí, almuerzas más tarde", (a + b) * 0.5f + lado * (anchoFranja * 0.5f + 1.25f), 0.06f);
        }

        int numero = partida != null ? partida.PuestoJugador : puesto + 1;
        Letrero($"TU PUESTO #{numero}", fila.Posicion(puesto) + lado * (anchoFranja * 0.5f + 0.6f), 0.08f);
        Letrero("PUERTA DEL RESTAURANTE", fila.Posicion(0) - dir * sep * 1.8f + lado * 0.3f, 0.07f);
    }

    // Texto acostado en el piso, girado para leerse derecho desde la cámara isométrica.
    void Letrero(string texto, Vector3 pos, float tam)
    {
        var go = new GameObject("Letrero " + texto);
        go.transform.SetParent(transform, false);
        go.transform.position = pos + Vector3.up * 0.04f;
        var cam = FindAnyObjectByType<CamaraIsometrica>();
        float giro = cam != null ? cam.giro : 45f;
        Vector3 arribaPantalla = Quaternion.Euler(0f, giro, 0f) * Vector3.forward;
        go.transform.rotation = Quaternion.LookRotation(Vector3.down, arribaPantalla);

        var tm = go.AddComponent<TextMesh>();
        tm.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        go.GetComponent<MeshRenderer>().sharedMaterial = tm.font.material;
        tm.text = texto;
        tm.fontSize = 64;
        tm.characterSize = tam;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.fontStyle = FontStyle.Bold;
        tm.color = colorTexto;
    }
}
