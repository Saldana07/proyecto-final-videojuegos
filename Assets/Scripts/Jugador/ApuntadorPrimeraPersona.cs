using UnityEngine;

// Apuntado en primera persona: las herramientas salen de la cámara hacia donde mira.
// Se monta junto a JugadorPrimeraPersona (de Saldaña), que sigue manejando
// el movimiento, la mirada con el mouse, correr y saltar.
// Dibuja una mira en el centro que se pone roja si hay un estudiante al alcance.
public class ApuntadorPrimeraPersona : MonoBehaviour, IApuntadorJugador
{
    public Transform camara;
    [Tooltip("Distancia a la que la mira detecta estudiantes (m).")]
    public float alcanceMira = 6f;
    public Color colorMira = Color.white;
    public Color colorMiraObjetivo = new Color(1f, 0.25f, 0.2f);

    public Vector3 OrigenHerramientas => camara.position;
    public Vector3 DireccionApuntado => camara.forward;
    public bool PuedeUsarHerramientas => Cursor.lockState == CursorLockMode.Locked;

    bool hayObjetivo;
    static Texture2D pixel;

    void Awake()
    {
        if (camara == null)
        {
            var cam = GetComponentInChildren<Camera>();
            camara = cam != null ? cam.transform : transform;
        }
    }

    void Update()
    {
        int mascara = ~Capas.MascaraJugador;
        hayObjetivo = Physics.Raycast(camara.position, camara.forward, out RaycastHit hit, alcanceMira, mascara, QueryTriggerInteraction.Ignore)
                      && hit.collider.GetComponentInParent<IReceptorHerramienta>() != null;
    }

    void OnGUI()
    {
        if (!PuedeUsarHerramientas) return;
        if (pixel == null) { pixel = new Texture2D(1, 1); pixel.SetPixel(0, 0, Color.white); pixel.Apply(); }

        float cx = Screen.width * 0.5f, cy = Screen.height * 0.5f, largo = 9f, grosor = 2f, hueco = 4f;
        GUI.color = hayObjetivo ? colorMiraObjetivo : colorMira;
        GUI.DrawTexture(new Rect(cx - hueco - largo, cy - grosor / 2, largo, grosor), pixel);
        GUI.DrawTexture(new Rect(cx + hueco, cy - grosor / 2, largo, grosor), pixel);
        GUI.DrawTexture(new Rect(cx - grosor / 2, cy - hueco - largo, grosor, largo), pixel);
        GUI.DrawTexture(new Rect(cx - grosor / 2, cy + hueco, grosor, largo), pixel);
        GUI.color = Color.white;
    }
}
