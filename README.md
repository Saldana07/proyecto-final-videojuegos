# ¡No te me colés!

**Proyecto final de la asignatura Videojuegos.**

Juego 3D hecho en Unity. De 10:00 a 12:00, la fila del restaurante universitario es un campo de batalla: eres el estudiante que madrugó a hacer fila y tu misión es que se cuele la menor cantidad de gente antes de que suene el timbre. Lees las señales de cada colado, actúas antes de que se acomode y gestionas tus herramientas.

| | |
|---|---|
| **Género** | Acción táctica / gestión de tiempo con elementos de *tower defense* y deducción |
| **Vista** | 3D isométrica, en un edificio del campus de la Universidad del Valle (Cali, Colombia) |
| **Partida** | 4 minutos reales = 2 horas de juego (10:00 → 12:00) |
| **Plataforma** | PC (teclado y mouse) |

**La regla de oro:** un colado solo es vulnerable mientras se está metiendo en la fila; cuando ya quedó adentro, es intocable. Cada facultad tiene su señal (*tell*), su herramienta que la saca (*counter*) y una que no le hace nada. Y cuidado: entre 30 % y 50 % de los que llegan son inocentes, y atacarlos agota la paciencia de la fila.

## Equipo

| Integrante | Parte del juego |
|---|---|
| Carlos | Jugador, cámara y herramientas (agua, megáfono) |
| Saldaña | Estudiantes, máquina de estados, fila y oleadas |
| Héctor | Partida, interfaz (HUD, resultados) y arte; autor del documento de diseño (GDD) |

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
- Se añadió un suelo de césped alrededor del edificio.

## Requisitos

- **Unity 6** (versión `6000.6.3f1`) con **Universal Render Pipeline (URP)**
- Paquetes **Input System** y **AI Navigation** (vienen en `Packages/manifest.json`)

## Cómo abrirlo

1. Clona el repositorio.
2. En **Unity Hub**: *Add → Add project from disk* y elige la carpeta del proyecto.
3. Abre una escena y dale **Play**:
   - `Assets/Scenes/SampleScene.unity`: el jugador y las herramientas, con objetivos de prueba.
   - `Assets/Scenes/Escena_Saldana.unity`: los estudiantes que llegan por oleadas e intentan colarse en la fila.
   - **`Assets/Scenes/Escena_Juego.unity`: el juego completo** (todo lo anterior + partida, HUD, resultados y audio).

## Controles

| Entrada | Acción |
|---|---|
| W A S D / flechas | Moverse |
| Mouse | Apuntar (el personaje mira al punto del piso) |
| Clic izquierdo | Usar herramienta |
| 1 = agua, 2 = megáfono / rueda | Cambiar herramienta |

## Estructura del proyecto

```
Assets/
├── Edificio/          Prefab del edificio (modelo de Cristian C.), mallas y materiales
├── Scenes/            Escena_Juego (el juego completo), SampleScene (Carlos) y Escena_Saldana (Saldaña)
├── Audio/             Sonidos sintetizados del juego (Héctor)
├── Scripts/
│   ├── Jugador/       Movimiento isométrico y cámara (Carlos)
│   ├── Herramientas/  Agua, megáfono, bebedero y contrato con los estudiantes (Carlos)
│   ├── Estudiantes/   Estudiante, fila, facultades y spawner por fases (Saldaña)
│   ├── Partida/       GameManager, HUD, resultados, audio, letreros y rendimiento (Héctor)
│   └── Pruebas/       Objetivos y HUD provisionales
├── Datos/             ScriptableObjects de balance (herramientas, jugador, estudiantes)
├── Prefabs/           Estudiante y objetos de los tells
├── Editor/            Herramientas de editor (menús "No te me colés" y "Univalle")
└── Settings/          Configuración de URP (calidad, sombras, anti-aliasing)
```

El menú **Univalle** (de `Assets/Editor/UnivalleTools.cs`) contiene las herramientas que se usaron para separar el edificio del modelo del campus y prepararlo. El menú **No te me colés** tiene las herramientas de cada integrante para montar y probar su parte.

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

## Estudiantes y fila (Saldaña)

Implementa la parte de Saldaña del reparto del GDD (sección 14.3): la fila, la máquina de estados de los estudiantes, las facultades Sistemas y Artes, los inocentes y el spawner por fases. Se prueba en su propia escena, `Assets/Scenes/Escena_Saldana.unity`, para no chocar con la `SampleScene`.

| Script | Para qué |
|---|---|
| `Scripts/Estudiantes/EstadoEstudiante.cs` | Enums del contrato 14.4: `EstadoEstudiante` y `TipoInocente` |
| `Scripts/Estudiantes/Estudiante.cs` | Máquina de estados Merodeo → Aproximación → Intento → Asegurado / Expulsado / Desistido; implementa `IReceptorHerramienta` de Carlos; Vergüenza individual; eventos para Héctor |
| `Scripts/Estudiantes/FacultadData.cs` | ScriptableObject por facultad: tell, counter, resistencia, tiempo de Intento, velocidad |
| `Scripts/Estudiantes/ConfigEstudiantes.cs` | ScriptableObject con el balance común (decaimiento de Vergüenza 10/s, Aproximación 3 s, radios de disuasión) |
| `Scripts/Estudiantes/Fila.cs` | Puestos, huecos visibles, reservas, ida al baño, abandono y compactación; zona caliente delante del puesto del jugador |
| `Scripts/Estudiantes/SpawnerEstudiantes.cs` | Llegadas por las 5 fases de la sección 5 del GDD (Calma → Estampida), 40 % de inocentes |

Datos de balance en `Assets/Datos/Estudiantes/` (`ConfigEstudiantes`, `Sistemas`, `Artes`) y prefabs en `Assets/Prefabs/Estudiantes/`.

### Cómo probarlo

1. Menú **No te me colés → Saldaña → 2 - Crear escena de prueba**: crea los datos, el prefab, la escena con la fila, el spawner y el NavMesh.
2. Dale **Play**. A los 3 s llega el primer colado.
3. El colado da vueltas en la plazoleta, elige un hueco (disco amarillo en el piso), muestra su tell (Sistemas: celular y audífonos azules; Artes: maqueta naranja) con anillo **amarillo** (Aproximación), y luego se mete con anillo **rojo** (Intento).
4. Sistemas cae con el **megáfono** (2) y Artes con el **agua** (1); la otra herramienta no les hace nada. Una vez dentro de la fila (Asegurado) ya no se puede sacar.
5. Los inocentes no tienen tell ni anillo: atacarlos dispara `OnFalsoPositivo`.

La etiqueta encima de cada uno (facultad, estado, Vergüenza) es para depurar; se apaga en `ConfigEstudiantes → mostrarEtiqueta`.

### Para Héctor (eventos)

```csharp
Estudiante.OnExpulsado            += e => { /* +10 Dignidad; +2 Paciencia si e.EnZonaCaliente */ };
Estudiante.OnDesistido            += e => { /* +5 Dignidad */ };
Estudiante.OnAsegurado            += e => { /* +1 infiltrado; delante si e.EnZonaCaliente; +10 Escándalo */ };
Estudiante.OnFalsoPositivo        += e => { /* −15 Paciencia */ };
Estudiante.OnActuoSobreAsegurado  += e => { /* −5 Paciencia */ };
SpawnerEstudiantes.RelojExterno    = () => segundosDePartida;   // usar el reloj del GameManager
SpawnerEstudiantes.OnCambioFase   += fase => { /* fase.nombre para el HUD */ };
```

Cada `Estudiante` expone `Estado`, `Verguenza` (0–100, para la barra sobre el colado) y `ProgresoIntento` (0–1).

## Partida, interfaz, resultados y audio (Héctor)

Implementa la parte de Héctor del reparto del GDD (sección 14.3): el GameManager con el reloj de 10:00 a 12:00, la Paciencia de la fila, el Escándalo y la Dignidad; el HUD; las pantallas de inicio, pausa y resultados; el audio; los letreros de la fila, y la optimización para llegar a 60 FPS o más. Todo se junta en la escena **`Assets/Scenes/Escena_Juego.unity`**, que es la escena del juego (la primera del Build).

| Script | Para qué |
|---|---|
| `Scripts/Partida/PartidaManager.cs` | GameManager: intro → jugando → pausa → terminada. Reloj de 240 s, fases, Paciencia, Escándalo, Dignidad, derrota y timbre. Escucha los eventos del `Estudiante` (contrato 14.4) y le da el reloj al spawner de Saldaña |
| `Scripts/Partida/PartidaConfig.cs` | ScriptableObject con todo el balance de la partida (`Assets/Datos/Partida/PartidaConfig`) |
| `Scripts/Partida/Puntaje.cs` | Fórmulas del GDD 10: Efectividad, rango, bono de Dignidad, hora de almuerzo |
| `Scripts/Partida/HUDPartida.cs` | HUD (reloj, barras, contadores, herramientas con recarga circular, Vergüenza e Intento sobre cada colado, flechas a colados fuera de pantalla, avisos) y pantallas de inicio, pausa y resultados |
| `Scripts/Partida/UIFabrica.cs` | Arma la interfaz en código (uGUI), sin prefabs que choquen en Git |
| `Scripts/Partida/AudioPartida.cs` | Sonidos en 2D: agua, megáfono, relleno, sin munición, expulsado, falso positivo, colado y timbre |
| `Scripts/Partida/MarcadoresFila.cs` | Arte de la fila: zona caliente pintada en el piso, "TU PUESTO #40" y la puerta del restaurante |
| `Scripts/Partida/MedidorRendimiento.cs` | FPS en pantalla (F3) e informe `Logs/rendimiento_*.txt` al terminar cada partida |
| `Scripts/Partida/TelemetriaPartida.cs` | Telemetría CSV del GDD: `Logs/telemetria_*.csv` con cada cambio de estado e impacto |
| `Editor/Hector/HectorPartidaEditor.cs` | Menú **No te me colés → Héctor** (ver abajo) |

Los sonidos de `Assets/Audio` son sintetizados por nosotros (no tienen licencia de terceros).

### Cómo probarlo

1. **No te me colés → Héctor → 1 - Crear escena de juego**: copia `Escena_Saldana` (que ya trae lo de Carlos) en `Escena_Juego` y le agrega la partida. Si `Escena_Juego` ya existe, solo rehace el grupo `Partida (Héctor)`.
2. **4 - Jugar** (o abrir `Escena_Juego` y dar Play). Clic o Enter para empezar.
3. **3 - Autoprueba de la partida**: 25 comprobaciones de las fórmulas y de la lógica (resultado en la consola y en `Logs/autoprueba_hector.txt`).

| Entrada | Acción |
|---|---|
| Esc / P | Pausa (Seguir, Reiniciar, Salir) |
| R | Reiniciar en la pausa o en los resultados |
| F3 | Mostrar FPS, CPU, GPU, SetPass y sombras |
| F4 *(solo editor o build de desarrollo)* | Tiempo x3 |
| F5 *(depuración)* | Terminar la partida ya (timbre) |
| F6 / F7 *(depuración)* | Usar la herramienta actual contra el colado / el inocente más cercano (si está a tiro) |

### Reglas implementadas (GDD 6 y 10)

- **Paciencia** empieza en 100: −15 por falso positivo, −5 por actuar sobre un Asegurado, +2 por expulsar a un colado que iba a la zona caliente. En 0 la fila **te saca a ti**: derrota.
- **Escándalo** +10 por cada colado que se asegura en la zona caliente (el Vigilante queda para después del 30 %).
- **Dignidad** +10 por expulsado, +5 por desistido y el bono del rango al final (S 100, A 60, B 30, C 10).
- **Efectividad** = frustrados / (frustrados + infiltrados) × 100 − 5 × falsos positivos. **Rango** S (≥ 95 % y 0 falsos positivos), A ≥ 85, B ≥ 70, C ≥ 50, D.
- **Hora de almuerzo** = 0,5 min × (puesto + infiltrados delante). El protagonista es el #40 (puesto 11 de la fila + 29 que ya van adentro).
- Al sonar el timbre (o con la derrota) la acción se congela y salen los resultados.

### Rendimiento (meta: 60 FPS o más)

**No te me colés → Héctor → 2 - Optimizar rendimiento** deja los ajustes en `Settings/PC_RPAsset` y `PC_Renderer` (también quedan en `Logs/optimizacion_hector.txt`):

| Ajuste | Antes | Después | Por qué |
|---|---|---|---|
| Distancia de sombras | 120 m | 55 m | Con la cámara a 35 m el piso visible está entre ~20 y ~52 m |
| Cascadas de sombra | 4 | 1 | La cámara ortográfica ve todo a una profundidad parecida |
| Mapa de sombras | 4096 | 2048 | Alcanza para 55 m |
| SSAO | encendido | apagado | Redibujaba las ~3.800 piezas del edificio en un pase extra |
| MSAA | 4x | apagado | La cámara ya usa SMAA: era antialiasing doble |
| Texturas de color y profundidad | encendidas | apagadas | Ningún efecto las usa |
| Cámara isométrica (`distancia`) | 60 m | 35 m | Mismo encuadre ortográfico, sombras más cortas |

También se optimizó código sin cambiar comportamiento: `CamaraIsometrica` calcula los bounds del edificio una sola vez (ordenados por altura) y usa `SphereCastNonAlloc`; `Estudiante` solo repinta su material y rearma su etiqueta cuando algo cambia. **Cámara en `Escena_Juego`:** `tamano` 8 (antes 10, más cerca del jugador) y `radioCorte` 8 (antes 14). `CamaraIsometrica` tiene la opción nueva `soloBajoTecho` (activa): los techos y pisos de arriba solo se ocultan cuando el jugador tiene un techo encima; antes se ocultaban también con el jugador afuera, en la plazoleta, y el edificio se veía desaparecer por partes.

`Escena_Juego` usa una copia de `ConfigEstudiantes` (`ConfigEstudiantes_Juego`) con las etiquetas de depuración apagadas; `Escena_Saldana` sigue igual.

**5 - Build de prueba de rendimiento** compila el juego para Windows en `Builds/Windows` (ignorada por Git) y lo ejecuta en modo prueba: si junto al `.exe` existe `prueba_rendimiento.txt`, la partida arranca sola, dura los 240 s, guarda `Logs/rendimiento_*.txt` junto al `.exe` y se cierra. Para jugar normal con esa build, borra ese archivo.
