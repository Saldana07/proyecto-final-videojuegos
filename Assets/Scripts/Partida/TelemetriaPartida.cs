using System.IO;
using System.Text;
using UnityEngine;

// Telemetría simple del GDD (sección de demos): una fila por cada cambio de estado de un estudiante
// y por cada impacto de herramienta. Se guarda en Logs/telemetria_*.csv al terminar. Dueño: Héctor.
public class TelemetriaPartida : MonoBehaviour
{
    public PartidaManager partida;
    readonly StringBuilder csv = new StringBuilder(32 * 1024);

    void Awake()
    {
        if (partida == null) partida = FindAnyObjectByType<PartidaManager>();
        csv.AppendLine("segundo;hora;estudiante;tipo;evento;herramienta;puesto;zona_caliente;verguenza");
    }

    void OnEnable()
    {
        Estudiante.OnCambioEstado += AlCambioEstado;
        Herramienta.Impacto += AlImpacto;
        if (partida != null) partida.EstadoCambiado += AlEstado;
    }

    void OnDisable()
    {
        Estudiante.OnCambioEstado -= AlCambioEstado;
        Herramienta.Impacto -= AlImpacto;
        if (partida != null) partida.EstadoCambiado -= AlEstado;
    }

    void AlCambioEstado(Estudiante e, EstadoEstudiante s) => Fila(e, s.ToString(), "");

    void AlImpacto(Herramienta h, IReceptorHerramienta r)
    {
        if (r is Estudiante e) Fila(e, "Impacto", h != null ? h.Tipo.ToString() : "");
    }

    void Fila(Estudiante e, string evento, string herramienta)
    {
        if (e == null || partida == null || partida.Estado == EstadoPartida.Intro) return;
        string tipo = e.EsInocente ? "Inocente-" + e.tipoInocente : (e.facultad != null ? e.facultad.nombre : "Colado");
        csv.Append(partida.Segundos.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)).Append(';')
           .Append(partida.HoraTexto).Append(';')
           .Append(e.name).Append(';').Append(tipo).Append(';').Append(evento).Append(';').Append(herramienta).Append(';')
           .Append(e.Puesto).Append(';').Append(e.EnZonaCaliente ? 1 : 0).Append(';')
           .Append(e.Verguenza.ToString("0", System.Globalization.CultureInfo.InvariantCulture)).AppendLine();
    }

    void AlEstado(EstadoPartida estado)
    {
        if (estado != EstadoPartida.Terminada) return;
        try
        {
            string ruta = MedidorRendimiento.Carpeta();
            Directory.CreateDirectory(ruta);
            File.WriteAllText(Path.Combine(ruta, $"telemetria_{System.DateTime.Now:yyyyMMdd_HHmmss}.csv"), csv.ToString(), Encoding.UTF8);
        }
        catch (System.Exception ex) { Debug.LogWarning("[Héctor] No pude guardar la telemetría: " + ex.Message); }
    }
}
