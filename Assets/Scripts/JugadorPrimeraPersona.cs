using UnityEngine;
using UnityEngine.Rendering.Universal;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Personaje en primera persona: WASD para caminar, mouse para mirar,
// Shift para correr, Espacio para saltar, Esc para soltar el mouse (clic para volver).
[RequireComponent(typeof(CharacterController))]
public class JugadorPrimeraPersona : MonoBehaviour
{
    public float velocidad = 4f;
    public float velocidadCorrer = 8f;
    public float sensibilidad = 0.12f;
    public float alturaSalto = 1.2f;
    public float gravedad = -20f;
    public Transform camara;

    CharacterController cc;
    float pitch;
    float velY;

    void Start()
    {
        cc = GetComponent<CharacterController>();
        if (camara == null) camara = GetComponentInChildren<Camera>().transform;
        ConfigurarCalidad(camara.GetComponent<Camera>());
        BloquearMouse(true);
    }

    // Suaviza los bordes (anti-aliasing) y activa los efectos de imagen de la escena.
    void ConfigurarCalidad(Camera cam)
    {
        if (cam == null) return;
        var datos = cam.GetUniversalAdditionalCameraData();
        datos.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        datos.antialiasingQuality = AntialiasingQuality.High;
        datos.renderPostProcessing = true;
        cam.allowMSAA = true;
    }

    void BloquearMouse(bool bloquear)
    {
        Cursor.lockState = bloquear ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !bloquear;
    }

    void Update()
    {
        Vector2 mouse = Vector2.zero, mover = Vector2.zero;
        bool correr = false, saltar = false, esc = false, clic = false;
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        var ms = Mouse.current;
        if (ms != null) { mouse = ms.delta.ReadValue(); clic = ms.leftButton.wasPressedThisFrame; }
        if (kb != null)
        {
            mover.x = (kb.dKey.isPressed ? 1 : 0) - (kb.aKey.isPressed ? 1 : 0);
            mover.y = (kb.wKey.isPressed ? 1 : 0) - (kb.sKey.isPressed ? 1 : 0);
            correr = kb.leftShiftKey.isPressed;
            saltar = kb.spaceKey.wasPressedThisFrame;
            esc = kb.escapeKey.wasPressedThisFrame;
        }
#else
        mouse = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * 10f;
        mover = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        correr = Input.GetKey(KeyCode.LeftShift);
        saltar = Input.GetKeyDown(KeyCode.Space);
        esc = Input.GetKeyDown(KeyCode.Escape);
        clic = Input.GetMouseButtonDown(0);
#endif
        if (esc) BloquearMouse(false);
        if (clic) BloquearMouse(true);

        if (Cursor.lockState == CursorLockMode.Locked)
        {
            transform.Rotate(0f, mouse.x * sensibilidad, 0f);
            pitch = Mathf.Clamp(pitch - mouse.y * sensibilidad, -85f, 85f);
            camara.localEulerAngles = new Vector3(pitch, 0f, 0f);
        }

        Vector3 dir = transform.right * mover.x + transform.forward * mover.y;
        if (dir.sqrMagnitude > 1f) dir.Normalize();
        float v = correr ? velocidadCorrer : velocidad;

        if (cc.isGrounded)
        {
            velY = -2f;
            if (saltar) velY = Mathf.Sqrt(alturaSalto * -2f * gravedad);
        }
        velY += gravedad * Time.deltaTime;
        cc.Move((dir * v + Vector3.up * velY) * Time.deltaTime);
    }
}
