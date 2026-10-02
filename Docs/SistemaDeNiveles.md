# Sistema interno de niveles

## Probar el ejemplo

1. Abrir el proyecto con Unity **6000.5.8f1** y esperar a que termine la importación.
2. Abrir `Assets/Levels/Examples/Generated/GameRoot.unity` si ya existe. Si no, ejecutar **Pyramid > Niveles > Crear ejemplo jugable**.
3. Pulsar Play. El ejemplo usa el jugador y las cámaras de una copia de `SampleScene`, dos tramos y contenido de prueba.
4. Seleccionar **Sistema de niveles** para ver el progreso y usar Pausar, Continuar, Reiniciar recargando y Detener.
5. Editar `Assets/Levels/Examples/Generated/Entrada.level`, guardar y reiniciar recargando.

La escena original `Assets/Scenes/SampleScene.unity` no se modifica. El generador no sobrescribe un ejemplo existente. Las pistas del ejemplo son tonos sintéticos suaves para verificar cambios de música, no música final.

## Archivos y responsabilidades

| Carpeta | Responsabilidad |
| --- | --- |
| `Assets/Scripts/Levels/Data` | Definiciones de nivel y contenido; assets configurables desde el Inspector. |
| `Assets/Editor/Levels` | Lectura YAML, validación, importación, controles internos, generador de ejemplo y comprobaciones. |
| `Assets/Scripts/Levels/Runtime` | Sesión, progreso, orden de eventos, creación de entidades, salud y limpieza. |
| `Assets/Scripts/Levels/Adapters` | Conexión entre configuración y comportamiento de entidades. |
| `Assets/Scripts/Levels/Presentation` | Fondo, iluminación/niebla, posprocesado y canales de audio. |
| `Assets/Scripts/Levels/Streaming` | Preparación, carga y descarga aditiva de tramos. |
| `Assets/Plugins/Editor/YamlDotNet` | Lector YAML 16.3.0, limitado al Editor, con licencia MIT. |

El `.level` es YAML de autoría. `LevelImporter` lo convierte en un `LevelDefinition` importado. El reproductor recibe datos tipados y no analiza YAML ni utiliza AssetDatabase durante la partida. Cada sesión toma una copia de la definición para que una reimportación no cambie el índice de eventos de la partida actual.

El catálogo reúne definiciones por ID y es una herramienta de autoría. El nivel importado solo referencia los recursos que utiliza. No hay un catálogo global obligatorio en la escena de juego.

## Formato versión 1

```yaml
version: 1
id: mi_nivel
catalogo: Assets/Levels/MiCatalogo.asset
longitud_m: 600

inicial:
  fondo: fondo.desierto
  ambientacion: ambiente.dia
  musica: musica.principal
  sonido_ambiental: sonido.viento

eventos:
  - id: torreta_01
    en: 50
    accion: crear
    recurso: enemigo.torreta
    posicion: { s: 130, x: -8, y: 3 }
    parametros:
      vida: 30
      cadencia: 2
      dano: 5
      duracion_s: 20
      comportamiento: estacionaria

  - id: fin
    en: 600
    accion: finalizar
```

`inicial`, sus propiedades, `tramos` y `eventos` son opcionales. El nivel finaliza automáticamente al alcanzar `longitud_m` si no hay una orden anterior de finalización.

- `en`: metros de progreso que activan el evento.
- `s`: posición longitudinal absoluta sobre el recorrido, independiente de la velocidad y del momento exacto del fotograma.
- `x`, `y`: desplazamientos lateral y vertical respecto al centro del recorrido. Por defecto, cero.
- `tramo`: propietario opcional de una entidad. Al retirar ese tramo se retira la entidad. Sin propietario permanece hasta morir, agotar su duración o limpiar la sesión.
- `duracion_s`: vida útil del objeto en segundos de juego; cero significa ilimitada. No equivale a puntos de salud.
- Parámetros omitidos: se conservan los predeterminados de `EntityDefinition`.
- `habilitado: false`: reserva un evento con ID único sin exigir que el recurso futuro exista.

Las acciones disponibles son `crear`, `fondo`, `ambientacion`, `musica`, `sonido_ambiental` y `finalizar`. Los cambios de presentación admiten `transicion_s >= 0`.

Los eventos se ordenan por `en`; los empates conservan el orden del archivo. Un salto de progreso ejecuta todos los eventos alcanzados. Retroceder no repite eventos. `finalizar` debe ser el último evento ejecutable.

Los campos desconocidos se rechazan. Se deben usar puntos decimales. Los IDs distinguen mayúsculas/minúsculas. La validación muestra archivo y ubicación; la comprobación previa a compilación también verifica las escenas referenciadas.

## Registrar contenido nuevo

Crear una definición desde **Assets > Create > Pyramid > Niveles**, asignarle un ID estable y añadirla a `ContentCatalog.entries`.

Para una entidad:

1. Asignar un prefab a `EntityDefinition`.
2. Añadir `Health` en la raíz si debe admitir salud y daño.
3. Añadir uno o más componentes que implementen `ILevelEntityAdapter` cuando necesite comportamiento configurable.
4. Escribir el ID en un evento `crear`.

`LevelEntity` se añade automáticamente al instanciar. La instancia se prepara bajo un contenedor inactivo; recibe configuración antes de activarse. El adaptador no debe iniciar comportamiento dependiente del nivel en Awake/OnEnable: debe hacerlo en `Begin`.

Contrato del adaptador:

```csharp
bool SupportsParameter(string key);
string ValidateSettings(EntitySettings settings); // null si es válido
void Configure(EntitySettings settings, Transform player, Camera camera);
void Begin();
void End();
```

Los parámetros comunes están tipados en `EntitySettings`. Un adaptador puede aceptar claves nuevas: el importador las guarda como texto en `settings.extra` y el componente las consulta con `settings.Get("clave")`. Debe validar rango y tipo en `ValidateSettings`. Esto permite añadir enemigos sin modificar el lector ni `LevelRunner`.

`TorretaLevelAdapter` admite `dano`, `cadencia`, `alcance`, `velocidad` de retirada y `comportamiento` (`estacionaria` o `seguidora`). `ContactDamage` admite `dano`. `vida` requiere `Health`; `duracion_s` es común. Las claves no admitidas por los componentes del prefab se rechazan.

Para una acción de nivel nueva sí se amplía `LevelAction`, su interpretación en `LevelCompiler` y se registra su ejecutor en `LevelSession`. No es necesario cambiar el algoritmo de `LevelRunner`.

## Escenas por tramos

`GameRoot` conserva jugador, cámaras, UI y gestores. Cada escena de tramo debe tener un único objeto raíz con `SceneSegmentRoot` y un hijo `content` guardado **inactivo**. La geometría se diseña con coordenadas locales: su origen es `inicio_s` y el centro vertical/lateral del riel. No se deben duplicar jugador, cámara principal, música ni gestores globales en un tramo.

```yaml
tramos:
  - id: exterior
    recurso: escenario.exterior
    inicio_s: 0
    fin_s: 300
    precargar_en: 0
    descargar_en: 400
  - id: interior
    recurso: escenario.interior
    inicio_s: 300
    fin_s: 600
    precargar_en: 120
    descargar_en: 650
```

`SceneSegmentDefinition.scenePath` contiene la ruta completa de la escena, incluida en Build Settings. En esta versión cada tramo usa una escena distinta; los intervalos deben cubrir el recorrido sin huecos ni solapamientos. El periodo de residencia en memoria sí puede solaparse.

`LevelSceneStreamer` carga antes de llegar, coloca la raíz y activa su contenido. Si la siguiente zona no está lista, detiene temporalmente la sesión antes del límite (`boundaryMargin` más el avance previsto). La descarga se programa detrás del jugador; el autor debe dejar margen suficiente para la cámara y los objetos visibles. Es un recorrido hacia delante: los tramos retirados no se vuelven a cargar al retroceder.

Las operaciones iniciadas se dejan terminar. El reinicio suspende nuevas solicitudes, espera las operaciones pendientes y descarga antes de preparar la siguiente sesión. No se deja una carga bloqueando indefinidamente `allowSceneActivation`.

La descarga retira objetos de escena; no garantiza liberar todos sus recursos. Los assets referenciados por el nivel permanecen disponibles. Esta versión no incorpora Addressables ni descarga individual de texturas/prefabs. No se fuerza `Resources.UnloadUnusedAssets` durante el vuelo.

## Presentación, audio y pausa

- Fondo: color de cámara interpolable; skybox y prefab de fondo cambian inmediatamente. El prefab puede seguir la cámara con un factor de paralaje.
- Ambiente: interpolación de iluminación, rotación de luz y niebla exponencial. Perfiles de Volume y partículas cambian inmediatamente.
- Audio: dos canales independientes, música y ambiente. Las transiciones parten de los volúmenes actuales y las voces anteriores se destruyen al terminar. Las pistas usan AudioMixerGroup opcional.
- Pausa: reloj escalado detenido, controles bloqueados y canales de audio pausados. El freno utiliza tiempo de juego. La carga asíncrona puede terminar durante la pausa.
- Stop: limpia contenido y devuelve el reloj/controles. Restart: además restablece posición, freno, salud, disparo, cámaras, eventos y estado inicial.

Debe llamarse `StopLevel` y esperar `State == Idle` antes de retirar la escena base. El sistema está diseñado para una sesión de nivel activa.

## Alcance actual y pruebas

El movimiento implementado es el riel recto actual sobre +Z, con origen configurable y sin escala/rotación. `IRoute` permite una futura implementación curva, pero el controlador del jugador también necesitará esa adaptación. Los modos libre y carrera se conservan; la sesión de niveles inicia en modo Riel.

Se usa Instantiate/Destroy. Los contratos de inicialización/retirada permiten añadir pooling posteriormente. No hay editor visual ni edición de archivos en una build del jugador. Las ediciones de YAML se aplican al reiniciar desde el Inspector.

Menús:

- **Validar todos los niveles**: reimporta fuentes y verifica escenas.
- **Pruebas de datos y ejecución**: formato, errores, parámetros, orden, saltos, fin y reinicio del reproductor.
- **Prueba integrada de pausa, streaming y reinicio**: ejecuta el ejemplo, pausa, carga otro tramo, reinicia durante carga y comprueba limpieza.
- **Comprobar scripts de la build**: compila el código del jugador para macOS y comprueba la separación del código del Editor.

Ejecución automatizada con Unity (rutas adaptables):

```text
Unity -batchmode -nographics -quit -projectPath <proyecto>
  -executeMethod Pyramid.Levels.Editor.LevelSystemChecks.BuildExampleAndCheck
  -logFile <log>

Unity -batchmode -nographics -projectPath <proyecto>
  -executeMethod Pyramid.Levels.Editor.LevelPlayModeChecks.Run
  -logFile <log>
```

La segunda orden termina por sí misma al completar o fallar; no lleva `-quit` al iniciar.
