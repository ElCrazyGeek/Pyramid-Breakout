# Nave de pasada

El prefab **Nave de Pasada** reutiliza la nave enemiga, proyectil, salud, sonidos
y explosión existentes. Entra desde delante y un costado, dispara durante la
aproximación, cruza por delante, pasa por el costado opuesto del jugador y continúa hasta salir de la pantalla.
Espera fuera de vista y repite desde el otro lado.

## Probarla

1. Abre `Assets/Levels/Examples/Generated/GameRoot.unity`.
2. En **Sistema de niveles > LevelSession > Level**, asigna
   `Assets/Levels/Examples/Pasadas.level`.
3. Pulsa Play. Al recorrer 30 metros aparece una nave que repite pasadas.

El nivel de prueba no carga escenas adicionales. Para volver al nivel habitual,
asigna de nuevo `Assets/Levels/Examples/Generated/Entrada.level`.

También puedes arrastrar
`Assets/Levels/Examples/Generated/Nave de Pasada.prefab` a una escena de vuelo en
rieles. El componente **EnemigoPasada** permite asignar **Objetivo** y **Camara**;
en una sesión de nivel recibe estas referencias del generador.

## Añadirla a un nivel

El recurso `enemigo.pasada` ya está registrado en el catálogo del proyecto.
Añade un evento dentro de `eventos`:

```yaml
  - id: pasada_01
    en: 100
    accion: crear
    recurso: enemigo.pasada
    posicion: { s: 210, x: 90, y: 12 }
    parametros:
      activacion: 150
      vida: 20
      velocidad: 65
      cadencia: 2
      lado: derecha
      alternar: true
      pausa: 1.5
      duracion_s: 0
```

`en` determina cuándo se crea. Espera inmóvil hasta que el jugador esté dentro
de `distanciaActivacion` (distancia 3D en unidades de Unity, 150 por defecto).
Se configura en el Inspector o con `activacion` en el archivo `.level`. Una vez
activada, continúa repitiendo sus pasadas; `alcance` controla por separado cuándo dispara. La trayectoria se calcula respecto al jugador
al comenzar cada pasada; `posicion` es la posición inicial del generador.
La nave conserva el avance longitudinal del jugador y fija el recorrido lateral
durante cada pase, de modo que el jugador puede esquivarlo.

## Ajustes

| Parámetro `.level` | Función | Valor inicial |
| --- | --- | --- |
| `activacion` | Radio para iniciar el primer pase desde la posición de aparición. | `150` |
| `velocidad` | Velocidad del pase respecto al avance del jugador, en unidades/s. | `65` |
| `entrada` | Distancia por delante del jugador al iniciar. | `110` |
| `lateral` | Separación lateral de la entrada. | `90` |
| `separacion` | Distancia horizontal mínima del pase a la posición inicial del jugador. | `20` |
| `salida` | Distancia detrás del jugador antes de poder repetir. | `65` |
| `pausa` | Espera fuera de pantalla entre pasadas, en segundos. | `1.5` |
| `lado` | Primer lado de entrada: `derecha` o `izquierda`. | `derecha` |
| `alternar` | Cambiar de lado en cada pasada: `true` o `false`. | `true` |
| `cadencia` | Disparos por segundo; `0` desactiva el disparo. | `2` |
| `alcance` | Distancia máxima de disparo. | `95` |
| `dano` | Daño de cada proyectil. | `1` |
| `vida` | Salud del enemigo. | `20` |
| `duracion_s` | Duración total; `0` permite repetir hasta destruirlo o limpiar el nivel. | `0` |

En el prefab también puedes ajustar la altura de entrada y de paso, la distancia
mínima de disparo y el ángulo permitido para apuntar. Mantén una separación
acorde al tamaño del modelo: la configuración inicial conserva una distancia
horizontal mínima de 20 unidades para una nave con un collider de unas 25 unidades
de ancho, incluso cuando la cámara obliga a ampliar la entrada. El jugador puede
moverse hacia esa trayectoria o esquivarla una vez iniciado el pase.

## Comprobaciones

Fuera de Play, ejecuta **Pyramid > Enemigos > Comprobar nave de pasada**.
La prueba usa escenas temporales para comprobar el pase, disparos, separación,
repetición, pausa y entradas con el jugador en los extremos del encuadre.
También valida el catálogo y los archivos `.level`.
