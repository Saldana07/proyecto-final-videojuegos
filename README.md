# Recorrido virtual – Edificio Univalle

**Proyecto final de la asignatura Videojuegos.**

Proyecto de Unity con un recorrido en primera persona por un edificio del campus de la Universidad del Valle (Cali, Colombia).

## Créditos del modelo 3D

El modelado 3D del edificio **no es de autoría propia**. Proviene del modelo **"Campus univalle"** creado por **Cristian C.** y publicado en SketchUp 3D Warehouse:

- **Modelo original:** [Campus univalle – 3D Warehouse](https://3dwarehouse.sketchup.com/model/ab8c3983-02e8-4ad7-876f-51650091adac/Campus-univalle)
- **Autor:** Cristian C.
- **Última modificación del modelo original:** 4 de septiembre de 2020
- **Licencia:** [General Model License de 3D Warehouse](https://3dwarehouse.sketchup.com/tos/) (Trimble Inc.)

Todo el mérito del modelado le corresponde a su autor. Muchas gracias a Cristian C. por compartir su trabajo.

### Cambios hechos sobre el modelo original

- Se tomó **solo un edificio** del modelo completo del campus (grupo `Group 3876` del archivo original); el resto del campus no se incluye.
- Se convirtió a un prefab de Unity con mallas y materiales propios (`Assets/Edificio`).
- Se cambiaron los colores de los materiales (concreto, ladrillo, metal y vidrio) y se agregaron colisiones.
- Se añadió un suelo de césped y un personaje en primera persona.

## Requisitos

- **Unity 6** (versión `6000.6.3f1`) con **Universal Render Pipeline (URP)**
- Paquete **Input System**

## Cómo abrirlo

1. Clona el repositorio.
2. En **Unity Hub**: *Add → Add project from disk* y elige la carpeta del proyecto.
3. Abre la escena `Assets/Scenes/SampleScene.unity`.
4. Dale **Play**.

## Controles

| Tecla | Acción |
|---|---|
| W A S D | Caminar |
| Mouse | Mirar |
| Shift | Correr |
| Espacio | Saltar |
| Esc | Soltar el mouse (clic para volver a capturarlo) |

## Estructura del proyecto

```
Assets/
├── Edificio/          Prefab del edificio, mallas, materiales y suelo
├── Scenes/            Escena principal (SampleScene)
├── Scripts/           JugadorPrimeraPersona.cs (control en primera persona)
├── Editor/            UnivalleTools.cs (herramientas usadas para extraer el edificio)
└── Settings/          Configuración de URP (calidad, sombras, anti-aliasing)
```

El menú **Univalle** dentro de Unity (de `Assets/Editor/UnivalleTools.cs`) contiene las herramientas que se usaron para separar el edificio del modelo del campus, poner el suelo, las colisiones y el jugador.

## Jugador y herramientas (Carlos)

Implementa la parte de Carlos del reparto del GDD (sección 14.3) en la `SampleScene` con **cámara isométrica**: sigue al jugador y vuelve invisible el edificio que lo tapa (lo que se cruza entre la cámara y el jugador, y los techos y pisos de arriba cuando está bajo techo). Las piezas ocultas conservan su colisión.

| Script | Para qué |
|---|---|
| `Scripts/Jugador/EntradaJugador.cs` | Acciones del Input System nuevo (WASD/flechas/stick, mouse, clic, rueda, 1–5) |
| `Scripts/Jugador/JugadorIsometrico.cs` | Movimiento relativo a la cámara; mira al punto del piso que señala el mouse |
| `Scripts/Jugador/CamaraIsometrica.cs` | Cámara ortográfica (35°, 45°) que sigue al jugador y oculta el edificio que estorba |
| `Scripts/Jugador/JugadorConfig.cs` | ScriptableObject con velocidad (4 m/s), giro, gravedad |
| `Scripts/Jugador/ApuntadorPrimeraPersona.cs` | Alternativa en primera persona (no se usa en la escena) |
| `Scripts/Herramientas/TipoHerramienta.cs` | Enum del contrato 14.4 |
| `Scripts/Herramientas/IReceptorHerramienta.cs` | Interfaz que implementa el `Estudiante` + capas `Estudiante` y `Jugador` |
| `Scripts/Herramientas/IApuntadorJugador.cs` | Desacopla las herramientas de la cámara |
| `Scripts/Herramientas/HerramientaData.cs` | ScriptableObject con alcance, cono, recarga, munición, Vergüenza, sonidos |
| `Scripts/Herramientas/Herramienta.cs` | Clase base: recarga, munición, congelar (Derecho), eventos para el HUD |
| `Scripts/Herramientas/Megafono.cs` | `Physics.OverlapSphere` de 5 m + filtro de 45°, +60 Vergüenza, recarga 10 s |
| `Scripts/Herramientas/BotellaAgua.cs` | `SphereCast` de 3 m, 6 cargas; el primer obstáculo corta el chorro |
| `Scripts/Herramientas/Bebedero.cs` | Zona (trigger) que rellena el agua |
| `Scripts/Herramientas/CinturonHerramientas.cs` | Máximo 3 herramientas; teclas 1–5, rueda y clic izquierdo |
| `Scripts/Pruebas/DianaPrueba.cs`, `HUDHerramientasPrueba.cs` | Objetivos y HUD provisionales hasta tener el `Estudiante` y el HUD final |

Datos de balance en `Assets/Datos/` (`Jugador/JugadorConfig`, `Herramientas/Agua`, `Herramientas/Megafono`).

### Dónde están los objetivos

Se ubicaron con **No te me colés → Carlos → Analizar estructura del edificio**, que recorre la planta baja con rayos y guarda un mapa de dónde se puede caminar en `Logs/analisis_edificio.txt`. La planta baja (y≈0) es abierta, con columnas cada ~7 m; hay un segundo piso (y≈3,5) y un techo (y≈7).

- **Fila de inocentes** (grises): vestíbulo sur techado, z≈58,5, de x 219 a 236, con un hueco libre.
- **Sistemas** (azules, counter megáfono): dos en la plazoleta llegando a la fila y uno en el corredor interior.
- **Artes** (naranjas, counter agua): pasillo occidental y fondo del corredor.
- **Bebedero**: entrada occidental del vestíbulo (213, 57).
- **Jugador**: arranca en la plazoleta (229, 46), frente a la fila.

### Cómo probarlo

1. Abre `Assets/Scenes/SampleScene.unity` y dale **Play**.
2. **WASD** mueve (W = arriba en pantalla), el **mouse** apunta (marcador rojo en el piso), **1** = agua, **2** = megáfono, **clic** = usar.
3. El HUD de abajo a la izquierda muestra munición y recarga; la consola, cada impacto.

**2 - Integrar en SampleScene (isométrica)** vuelve a montar todo si se rompe. **3 - Autoprueba de herramientas** comprueba cono, alcance, munición, bebedero, cambio de herramienta y congelamiento. **Capturas de la vista isométrica** guarda imágenes en `Logs/` sin dar Play.

### Para Saldaña (contrato)

El `Estudiante` debe estar en la capa **Estudiante** e implementar `IReceptorHerramienta`:

```csharp
public class Estudiante : MonoBehaviour, IReceptorHerramienta
{
    public void RecibirHerramienta(TipoHerramienta tipo) { /* contrato 14.4 */ }
    // Opcional: con los números de la herramienta (Vergüenza, freno, origen)
    public void RecibirHerramienta(ImpactoHerramienta impacto) { /* ... */ }
}
```

Para Derecho ("argumenta"): `impacto.usuario.GetComponent<CinturonHerramientas>().Buscar(impacto.tipo).Congelar(3f)`.

### Para Héctor (HUD y audio)

`CinturonHerramientas.Herramientas`, `.Actual`, evento `Cambiada`; en cada `Herramienta`: `ProgresoRecarga` (0–1, recarga circular), `Municion`, eventos `Usada`, `SinMunicion`, `MunicionCambiada`. Los sonidos se asignan en cada `HerramientaData` (`sonidoUso`, `sonidoVacio`, `sonidoRelleno`). `Herramienta.Impacto` (estático) sirve para la telemetría CSV.

### Controles (vista isométrica)

| Entrada | Acción |
|---|---|
| W A S D / flechas | Moverse |
| Mouse | Apuntar (el personaje mira al punto del piso) |
| Clic izquierdo | Usar herramienta |
| 1–5 / rueda | Cambiar herramienta |
