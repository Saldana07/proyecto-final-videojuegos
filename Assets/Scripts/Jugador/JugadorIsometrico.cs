using UnityEngine;

// Protagonista con cámara isométrica.
// WASD mueve relativo a la cámara (W = hacia arriba en pantalla) y el personaje
// mira al punto del piso que señala el mouse. Ese punto se calcula con un plano a
// la altura de los pies, no con física, para que techos y paredes (aunque estén
// ocultos por la cámara) no desvíen la puntería.
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(EntradaJugador))]
public class JugadorIsometrico : MonoBehaviour, IApuntadorJugador
{
    public JugadorConfig config;
    public Camera camara;
    [Tooltip("Opcional: un marcador que se pone en el punto apuntado.")]
    public Transform marcadorApuntado;

    public Vector3 PuntoApuntado { get; private set; }
    public Vector3 DireccionApuntado { get; private set; } = Vector3.forward;
    public Vector3 OrigenHerramientas => transform.position + Vector3.up * config.alturaManos;
    public bool PuedeUsarHerramientas => true;
    public Vector3 Velocidad => velPlano;

    CharacterController cc;
    EntradaJugador entrada;
    Vector3 velPlano;
    float velY;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        entrada = GetComponent<EntradaJugador>();
        if (config == null) config = ScriptableObject.CreateInstance<JugadorConfig>();
        if (camara == null) camara = Camera.main;
        DireccionApuntado = transform.forward;
        PuntoApuntado = transform.position + transform.forward;
    }

    void Start()
    {
        // En isométrica el cursor se necesita para apuntar.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Update()
    {
        Apuntar();
        Mover();
    }

    void Apuntar()
    {
        if (camara == null) return;
        Ray rayo = camara.ScreenPointToRay(entrada.Puntero.ReadValue<Vector2>());
        if (new Plane(Vector3.up, transform.position).Raycast(rayo, out float d))
            PuntoApuntado = rayo.GetPoint(d);

        Vector3 dir = PuntoApuntado - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.04f) DireccionApuntado = dir.normalized;

        if (marcadorApuntado != null) marcadorApuntado.position = PuntoApuntado + Vector3.up * 0.03f;
    }

    void Mover()
    {
        float dt = Time.deltaTime;
        Vector2 m = entrada.Mover.ReadValue<Vector2>();

        // Ejes de la cámara proyectados en el piso.
        Vector3 adelante = Vector3.forward, derecha = Vector3.right;
        if (camara != null)
        {
            derecha = Vector3.ProjectOnPlane(camara.transform.right, Vector3.up).normalized;
            adelante = Vector3.Cross(derecha, Vector3.up);
        }

        Vector3 deseada = Vector3.ClampMagnitude(adelante * m.y + derecha * m.x, 1f) * config.velocidad;
        velPlano = Vector3.MoveTowards(velPlano, deseada, config.aceleracion * dt);

        if (cc.isGrounded && velY < 0f) velY = -2f;
        velY += config.gravedad * dt;
        cc.Move((velPlano + Vector3.up * velY) * dt);

        Quaternion objetivo = Quaternion.LookRotation(DireccionApuntado, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, objetivo, config.velocidadGiro * dt);
    }
}
