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
