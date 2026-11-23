using System;
using System.Collections.Generic;
using UnityEngine;

// Clase base de todas las herramientas: recarga, munición, congelamiento y
// entrega del impacto al estudiante. Cada herramienta solo implementa Ejecutar
// (cómo detecta a quién alcanza).
public abstract class Herramienta : MonoBehaviour
{
    public HerramientaData datos;

    public TipoHerramienta Tipo => datos.tipo;
    public int Municion { get; private set; }
    public bool UsaMunicion => datos != null && datos.municionMax > 0;
    public bool TieneMunicion => !UsaMunicion || Municion > 0;
    public bool EnRecarga => Time.time < listaEn;
    public bool Congelada => Time.time < congeladaHasta;
    public bool Lista => datos != null && !EnRecarga && !Congelada && TieneMunicion;
    public float SegundosRestantes => Mathf.Max(0f, Mathf.Max(listaEn, congeladaHasta) - Time.time);

    // 0 = recién usada, 1 = lista. Es lo que dibuja la recarga circular del HUD.
    public float ProgresoRecarga
    {
        get
        {
            if (Congelada) return 0f;
            if (!EnRecarga || datos.recarga <= 0f) return 1f;
            return Mathf.Clamp01(1f - (listaEn - Time.time) / datos.recarga);
        }
    }

    // Para el HUD y el audio de Héctor.
    public event Action<Herramienta> Usada;
    public event Action<Herramienta> SinMunicion;
    public event Action<Herramienta> MunicionCambiada;
    // Estático para que la telemetría (CSV) escuche todas las herramientas sin buscarlas.
    public static event Action<Herramienta, IReceptorHerramienta> Impacto;

    float listaEn;
    float congeladaHasta;

    protected virtual void Awake() => Reiniciar();

    // Munición llena y sin recarga pendiente (inicio de partida o de un día nuevo).
    public void Reiniciar()
    {
        Municion = datos != null ? datos.municionMax : 0;
        listaEn = 0f;
        congeladaHasta = 0f;
        MunicionCambiada?.Invoke(this);
    }

    // Lo llama el CinturonHerramientas con el clic izquierdo. Devuelve true si se usó.
    public bool IntentarUsar(Vector3 origen, Vector3 direccion, GameObject usuario)
    {
        if (datos == null || EnRecarga || Congelada) return false;
        if (!TieneMunicion)
        {
            Sonar(datos.sonidoVacio, origen);
            SinMunicion?.Invoke(this);
            return false;
        }

        if (direccion.sqrMagnitude < 1e-4f) direccion = transform.forward;
        direccion.Normalize();

        // Usar contra un inocente también gasta la recarga (GDD 4.3); eso lo decide quien recibe.
        Ejecutar(origen, direccion, usuario);

        if (UsaMunicion)
        {
            Municion--;
            MunicionCambiada?.Invoke(this);
        }
        listaEn = Time.time + datos.recarga;
        Sonar(datos.sonidoUso, origen);
        if (datos.efectoPrefab != null)
            Destroy(Instantiate(datos.efectoPrefab, origen, Quaternion.LookRotation(direccion)), 3f);
        Usada?.Invoke(this);
        return true;
    }

    protected abstract void Ejecutar(Vector3 origen, Vector3 direccion, GameObject usuario);

    // Bebedero o mejoras. Devuelve cuántas cargas entraron.
    public int Rellenar(int cantidad)
    {
        if (!UsaMunicion) return 0;
        int antes = Municion;
        Municion = Mathf.Clamp(Municion + cantidad, 0, datos.municionMax);
        if (Municion != antes) MunicionCambiada?.Invoke(this);
        return Municion - antes;
    }

    // Derecho "argumenta" y congela la herramienta 3 s (GDD 7.2).
    public void Congelar(float segundos)
    {
        congeladaHasta = Mathf.Max(congeladaHasta, Time.time + segundos);
    }

    // Busca un receptor en el collider (o sus padres) y le entrega el impacto.
    // 'yaAlcanzados' evita golpear dos veces al mismo estudiante si tiene varios colliders.
    protected bool Impactar(Collider col, Vector3 origen, GameObject usuario, HashSet<IReceptorHerramienta> yaAlcanzados = null)
    {
        var receptor = col.GetComponentInParent<IReceptorHerramienta>();
        if (receptor == null) return false;
        if (yaAlcanzados != null && !yaAlcanzados.Add(receptor)) return false;

        receptor.RecibirHerramienta(new ImpactoHerramienta
        {
            tipo = datos.tipo,
            verguenza = datos.verguenza,
            frenarSegundos = datos.frenarSegundos,
            origen = origen,
            usuario = usuario,
        });
        Impacto?.Invoke(this, receptor);
        return true;
    }

    protected void Sonar(AudioClip clip, Vector3 donde)
    {
        if (clip != null) AudioSource.PlayClipAtPoint(clip, donde, datos.volumen);
    }
}
