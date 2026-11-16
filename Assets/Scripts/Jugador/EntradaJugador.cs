using UnityEngine;
using UnityEngine.InputSystem;

// Acciones del Input System nuevo para el protagonista (GDD 12.1).
// Se crean en código para no pisar InputSystem_Actions.inputactions, que es compartido.
[DefaultExecutionOrder(-100)]
public class EntradaJugador : MonoBehaviour
{
    public const int CantidadRanuras = 5;

    public InputAction Mover { get; private set; }
    public InputAction Puntero { get; private set; }
    public InputAction Usar { get; private set; }
    public InputAction Rueda { get; private set; }
    public InputAction[] Ranuras { get; private set; }

    void Awake()
    {
        Mover = new InputAction("Mover", InputActionType.Value, expectedControlType: "Vector2");
        Mover.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
        Mover.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
        Mover.AddBinding("<Gamepad>/leftStick");

        Puntero = new InputAction("Puntero", InputActionType.Value, "<Pointer>/position", expectedControlType: "Vector2");

        Usar = new InputAction("Usar", InputActionType.Button, "<Mouse>/leftButton");
        Usar.AddBinding("<Gamepad>/rightTrigger");

        Rueda = new InputAction("Rueda", InputActionType.Value, "<Mouse>/scroll/y", expectedControlType: "Axis");

        Ranuras = new InputAction[CantidadRanuras];
        for (int i = 0; i < CantidadRanuras; i++)
            Ranuras[i] = new InputAction($"Ranura{i + 1}", InputActionType.Button, $"<Keyboard>/{i + 1}");
    }

    void OnEnable() => ParaCada(a => a.Enable());
    void OnDisable() => ParaCada(a => a.Disable());
    void OnDestroy() => ParaCada(a => a.Dispose());

    void ParaCada(System.Action<InputAction> f)
    {
        if (Mover == null) return;
        f(Mover); f(Puntero); f(Usar); f(Rueda);
        foreach (var r in Ranuras) f(r);
    }
}
