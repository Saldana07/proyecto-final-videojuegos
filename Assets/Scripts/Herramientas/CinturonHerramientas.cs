using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

// Las herramientas que lleva el protagonista en la partida (máximo 3, GDD 8).
// Teclas 1–5 eligen la ranura, la rueda del mouse pasa a la siguiente/anterior
// y el clic izquierdo usa la herramienta actual hacia donde apunta el IApuntadorJugador.
// Corre antes que JugadorPrimeraPersona: así el clic que captura el mouse no dispara.
[DefaultExecutionOrder(-50)]
[RequireComponent(typeof(EntradaJugador))]
public class CinturonHerramientas : MonoBehaviour
{
    public const int MaximoPorPartida = 3;

    [Tooltip("Si queda vacío se toman las herramientas hijas del jugador, en orden.")]
    [SerializeField] List<Herramienta> herramientas = new();

    public IReadOnlyList<Herramienta> Herramientas => herramientas;
    public int Indice { get; private set; } = -1;
    public Herramienta Actual => Indice >= 0 && Indice < herramientas.Count ? herramientas[Indice] : null;

    // Para el HUD de Héctor: ranura elegida y resultado de cada clic.
    public event Action<int, Herramienta> Cambiada;
    public event Action<Herramienta, bool> UsoIntentado;

    EntradaJugador entrada;
    IApuntadorJugador apuntador;

    void Awake()
    {
        entrada = GetComponent<EntradaJugador>();
        apuntador = GetComponent<IApuntadorJugador>();
        if (apuntador == null)
            Debug.LogError("CinturonHerramientas necesita un IApuntadorJugador (p. ej. ApuntadorPrimeraPersona) en el jugador.", this);
        herramientas.RemoveAll(h => h == null);
        if (herramientas.Count == 0) herramientas.AddRange(GetComponentsInChildren<Herramienta>());
        if (herramientas.Count > MaximoPorPartida)
            Debug.LogWarning($"El GDD permite máximo {MaximoPorPartida} herramientas por partida; hay {herramientas.Count}.", this);
    }

    void Start() => Seleccionar(0);

    void Update()
    {
        for (int i = 0; i < entrada.Ranuras.Length; i++)
            if (entrada.Ranuras[i].WasPressedThisFrame()) Seleccionar(i);

        float rueda = entrada.Rueda.ReadValue<float>();
        if (rueda > 0.01f) Ciclar(-1);
        else if (rueda < -0.01f) Ciclar(+1);

        if (entrada.Usar.WasPressedThisFrame() && !PunteroSobreUI()) Usar();
    }

    public bool Usar()
    {
        var h = Actual;
        if (h == null || apuntador == null || !apuntador.PuedeUsarHerramientas) return false;
        bool ok = h.IntentarUsar(apuntador.OrigenHerramientas, apuntador.DireccionApuntado, gameObject);
        UsoIntentado?.Invoke(h, ok);
        return ok;
    }

    public void Seleccionar(int indice)
    {
        if (indice < 0 || indice >= herramientas.Count || indice == Indice) return;
        Indice = indice;
        Cambiada?.Invoke(Indice, Actual);
    }

    public void Ciclar(int paso)
    {
        if (herramientas.Count == 0) return;
        int n = herramientas.Count;
        Seleccionar(((Indice + paso) % n + n) % n);
    }

    // Antes de la partida: reemplaza las herramientas (selección de 3 del GDD 8).
    public void Equipar(IEnumerable<Herramienta> nuevas)
    {
        herramientas.Clear();
        foreach (var h in nuevas)
            if (h != null && herramientas.Count < MaximoPorPartida) herramientas.Add(h);
        Indice = -1;
        Seleccionar(0);
    }

    public Herramienta Buscar(TipoHerramienta tipo) => herramientas.Find(h => h != null && h.datos != null && h.Tipo == tipo);

    static bool PunteroSobreUI() => EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
}
