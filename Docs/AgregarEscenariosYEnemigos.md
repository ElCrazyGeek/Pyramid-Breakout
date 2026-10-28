# Agregar escenarios y enemigos manualmente

Esta guía explica cómo preparar un prefab, registrarlo en el catálogo desde el
Inspector de Unity y crearlo desde un archivo `.level`.

El flujo es: **prefab → definición de entidad → catálogo → evento del nivel**.
Guardar un prefab en una carpeta no lo registra automáticamente.

## 1. Elegir el catálogo

Abre el `.level` que quieres editar y revisa su campo `catalogo`. El ejemplo del
proyecto utiliza:

```yaml
catalogo: Assets/Levels/Examples/Generated/Catalog.asset
```

Ese es el asset que debes modificar en los pasos siguientes. Si necesitas uno
independiente, créalo con **Assets > Create > Pyramid > Niveles > Catálogo** y
escribe su ruta completa en el `.level`.

Trabaja fuera de Play para guardar los cambios de los prefabs y las definiciones.
Usa el Inspector para editar los assets; Unity administra sus referencias y
archivos `.meta`.

## 2. Agregar un escenario como prefab

### Preparar el escenario

1. Crea o abre el prefab en **Prefab Mode**. Puedes guardarlo en
   `Assets/Escenarios 3D catalogo/`.
2. Coloca dentro sus modelos, materiales y los colliders que necesite el juego.
   Un modelo visible no tiene colisión automáticamente.
3. Ajusta escala, orientación y el punto de origen del conjunto. El generador
   colocará la raíz en la posición indicada por el `.level`.
4. Guarda el prefab. Evita incluir jugador, cámara principal o gestores globales
   dentro de un escenario que se pueda instanciar varias veces.

### Crear la definición

1. En Project, selecciona la carpeta donde guardarás la definición. Puedes usar
   `Assets/Levels/Examples/Generated/` junto al catálogo actual.
2. Ejecuta **Assets > Create > Pyramid > Niveles > Entidad**.
3. Nombra el asset, por ejemplo `Templo.asset`.
4. En su Inspector, configura:

| Campo | Valor de ejemplo | Función |
| --- | --- | --- |
| Id | `escenario.templo` | Identificador que escribirás en `recurso`. |
| Prefab | Tu prefab del templo | Arrastra el asset desde Project. |
| Preserve Prefab Rotation | Activado | Conserva la rotación del prefab respecto al recorrido. |
| Defaults > Lifetime | `0` | Evita que desaparezca por tiempo. |
| Defaults > Health | `40` | Debe ser mayor que cero, aunque el escenario no tenga `Health`. |

La escala se conserva al instanciar. Con **Preserve Prefab Rotation** desactivado,
la raíz toma la orientación del recorrido. La posición guardada de la raíz del
prefab se reemplaza por `posicion`; los desplazamientos de sus hijos se conservan.
Actualmente no hay campos YAML para cambiar escala o rotación por evento: ajusta
el prefab o crea una variante y regístrala con otro ID.

Un escenario decorativo no necesita `Health`. Añádelo a la raíz únicamente si
quieres que sea destructible mediante el sistema de daño. `LevelEntity` se añade
automáticamente durante la creación.

### Registrarlo y usarlo

1. Selecciona `Catalog.asset`.
2. Despliega **Entries** y añade un elemento con **+** o incrementando el tamaño
   de la lista.
3. Arrastra `Templo.asset` a ese elemento. La lista recibe la definición de
   entidad, no el prefab directamente.
4. Comprueba que no haya elementos vacíos ni IDs repetidos. Al duplicar una
   definición debes cambiar su campo **Id**; renombrar el archivo no basta.
5. Añade este bloque dentro de `eventos` en tu `.level`:

```yaml
  - id: templo_01
    en: 0
    accion: crear
    recurso: escenario.templo
    posicion: { s: 200, x: 0, y: -10 }
```

El valor de `y` es ilustrativo: ajústalo a la altura de tu modelo y al origen de
la ruta. Puedes repetir el mismo recurso en varios eventos con IDs distintos.

### Escenarios ya registrados

| Prefab | Recurso |
| --- | --- |
| Fortaleza | `escenario.fortaleza` |
| Nave destruida | `escenario.nave_destruida` |
| Pasillo de artilleros | `escenario.pasillo_artilleros` |

Los tres tienen duración ilimitada y conservan su orientación. Consulta el nivel
[Escenarios3D.level](../Assets/Levels/Examples/Escenarios3D.level) para ver su uso.

## 3. Agregar un enemigo

### Preparar el prefab

1. Crea un prefab o una variante de un enemigo existente. Configura su modelo,
   colliders, scripts de movimiento/disparo y referencias internas, como boca
   de disparo o proyectil.
2. Añade **Health** en la raíz si quieres admitir `vida` desde el `.level`.
   Los colliders pueden estar en hijos: los proyectiles del proyecto buscan
   `IDamageable` en sus padres.
3. Configura tags y capas según los scripts del enemigo y las colisiones del
   proyecto. Verifica también las referencias al jugador que resuelve su script
   o adaptador al aparecer.
4. Guarda el prefab.

Registrar un prefab permite crearlo, pero sus scripts determinan cómo se mueve
y ataca. Los campos de `Defaults` no se conectan automáticamente a cualquier
script del enemigo.

### Crear y registrar su definición

1. Crea una **Entidad** desde **Assets > Create > Pyramid > Niveles > Entidad**.
2. Nómbrala `Guardia.asset`, asigna **Id = `enemigo.guardia`** y arrastra su prefab
   al campo **Prefab**.
3. Configura **Defaults > Health**, por ejemplo `30`, y **Lifetime**, por ejemplo
   `20` segundos. La salud de la definición se aplica al `Health` de la raíz al
   aparecer; `Lifetime = 0` desactiva el límite de tiempo del generador.
4. Elige **Preserve Prefab Rotation** según la orientación de su modelo.
5. Añade `Guardia.asset` a **Entries** del catálogo usado por tu nivel.
6. Añade un evento como este, suponiendo que el prefab tiene `Health` en la raíz:

```yaml
  - id: guardia_01
    en: 50
    accion: crear
    recurso: enemigo.guardia
    posicion: { s: 180, x: -12, y: 0 }
    parametros:
      vida: 30
      duracion_s: 20
```

### Parámetros que puedes cambiar desde el nivel

| YAML | Campo de Defaults | Requisito |
| --- | --- | --- |
| `duracion_s` | Lifetime | Disponible para cualquier entidad. |
| `vida` | Health | Componente `Health` en la raíz del prefab. |
| `dano` | Damage | Adaptador que lo admita, como `TorretaLevelAdapter` o `ContactDamage`. |
| `cadencia` | Fire Rate | `TorretaLevelAdapter` u otro adaptador compatible. |
| `alcance` | Range | `TorretaLevelAdapter` u otro adaptador compatible. |
| `velocidad` | Speed | Con `TorretaLevelAdapter`, controla la velocidad de retirada. |
| `comportamiento` | Behaviour | Con `TorretaLevelAdapter`: `estacionaria` o `seguidora`. |

`vida` debe ser mayor que cero; los demás valores numéricos de esta tabla deben
ser no negativos y finitos. Los parámetros omitidos conservan los valores de la
definición. Un parámetro no admitido por los componentes del prefab produce un
error de importación, aunque exista un campo con un nombre parecido en su script.

Para una torreta que utilice el componente **Torreta**, añade
**TorretaLevelAdapter** al mismo objeto. Configura también su proyectil y puntos
de disparo. Entonces puedes usar, por ejemplo:

```yaml
    parametros:
      vida: 30
      dano: 5
      cadencia: 2
      alcance: 120
      duracion_s: 20
      velocidad: 5
      comportamiento: estacionaria
```

Este bloque presupone `Health` en la raíz y el adaptador de torreta. No añadas
`TorretaLevelAdapter` a un enemigo que solo use `Artillero` o `Lanzador_Rayos`
esperando que configure esos scripts: el adaptador trabaja con `Torreta`.
Para otros scripts, configura sus valores directamente en el prefab o implementa
un adaptador específico.

### Si necesitas parámetros para un script nuevo

Implementa `ILevelEntityAdapter` y añade el componente al prefab:

1. `SupportsParameter` declara las claves YAML aceptadas.
2. `ValidateSettings` devuelve un mensaje si la configuración no es válida, o
   `null` si es correcta.
3. `Configure` aplica los valores al script y recibe jugador y cámara. Se llama
   antes de activar la instancia.
4. `Begin` inicia el comportamiento después de activarla.
5. `End` detiene el comportamiento al liberar la entidad mediante `Release`.

La lógica que dependa de la configuración debe empezar en `Begin`, evitando
iniciarla prematuramente desde `Awake` o `OnEnable`. Para claves adicionales usa
`EntitySettings.Get(...)`. Puedes tomar como referencia
[TorretaLevelAdapter.cs](../Assets/Scripts/Levels/Adapters/TorretaLevelAdapter.cs).

## 4. Entender la posición y la duración

| Campo | Significado |
| --- | --- |
| `id` del evento | Identificador único de esa aparición dentro del nivel. |
| `recurso` | ID exacto de la definición registrada en el catálogo. |
| `en` | Metros de progreso del jugador que disparan el evento. |
| `posicion.s` | Posición longitudinal absoluta sobre el recorrido. |
| `posicion.x`, `posicion.y` | Desplazamientos lateral y vertical respecto a la ruta. |
| `parametros.duracion_s` | Segundos de vida desde la creación; `0` significa ilimitada. |
| `tramo` | ID opcional de un tramo existente; la entidad se retira al descargarlo. |

`en` y `posicion.s` deben estar entre `0` y `longitud_m`. Por ejemplo, `en: 50`
y `s: 180` crean el objeto por delante del jugador cuando alcanza 50 metros.
Las distancias no son segundos. Usa puntos decimales, espacios para indentar y
respeta mayúsculas/minúsculas en los IDs.

Sin `tramo`, una entidad permanece hasta que agote su duración, su lógica la
destruya o se limpie la sesión. Con `tramo`, la posición `s` debe estar dentro de
su intervalo y `en` debe cumplir `precargar_en <= en < descargar_en`.

## 5. Agregar un escenario como escena de tramo

Usa este flujo cuando necesites cargar y descargar una escena `.unity` por
secciones. Una definición de entidad se usa en `eventos` con `accion: crear`;
una definición **Tramo de escena** se usa en `tramos`.

1. Crea una escena con un único objeto raíz, por ejemplo `TramoTemplo`, y añade
   **SceneSegmentRoot** a esa raíz.
2. Crea debajo un hijo `Contenido`. Pon dentro toda la geometría y asigna ese
   hijo al campo **Content** de `SceneSegmentRoot`.
3. Guarda `Contenido` desactivado y la raíz activa. Diseña las posiciones locales
   respecto al inicio del tramo; el sistema colocará la raíz en `inicio_s`.
4. Guarda la escena, por ejemplo `Assets/Scenes/Templo.unity`. Mantén jugador,
   cámaras principales y gestores globales en `GameRoot`.
5. Añade y habilita la escena en la lista de escenas de compilación de Unity.
6. Crea **Assets > Create > Pyramid > Niveles > Tramo de escena**. Asigna
   **Id = `escenario.templo_tramo`** y **Scene Path = `Assets/Scenes/Templo.unity`**.
7. Añade esa definición a **Entries** del catálogo.
8. Para un nivel de 600 metros formado por ese único tramo, usa:

```yaml
longitud_m: 600
tramos:
  - id: templo
    recurso: escenario.templo_tramo
    inicio_s: 0
    fin_s: 600
    precargar_en: 0
    descargar_en: 650
```

Si hay varios tramos, deben cubrir toda `longitud_m` sin huecos ni solapamientos.
Cada tramo debe usar una escena distinta. Se exige
`0 <= precargar_en <= inicio_s < fin_s <= longitud_m` y
`descargar_en >= fin_s`. El margen de descarga permite conservar contenido detrás
del jugador mientras aún se ve en cámara.

## 6. Validar y probar

1. Guarda prefabs, definiciones, catálogo y `.level`.
2. Espera a que Unity termine de importar. Ejecuta
   **Pyramid > Niveles > Validar todos los niveles** y revisa la Console.
3. Abre `Assets/Levels/Examples/Generated/GameRoot.unity` y selecciona
   **Sistema de niveles**.
4. Asigna tu `.level` al campo **Level** de **LevelSession** y pulsa Play.
5. Comprueba posición, orientación, escala, colisiones y comportamiento. Si has
   agregado un enemigo destructible, prueba también que reciba daño.
6. Después de editar el YAML durante Play, pulsa **Iniciar / reiniciar recargando**
   en el Inspector de `LevelSession` para aplicar los cambios.

## Problemas frecuentes

| Síntoma o mensaje | Qué revisar |
| --- | --- |
| `No existe el recurso` | ID exacto, definición añadida a Entries y ruta de catálogo del `.level`. |
| Recurso vacío o sin identificador | Elimina elementos vacíos de Entries y completa Id en cada definición. |
| Identificador duplicado | Cambia Id en la definición duplicada o retira la entrada repetida. |
| Entidad sin prefab | Asigna un prefab del Project al campo Prefab de la definición. |
| No admite el parámetro | Comprueba Health en la raíz o el adaptador correspondiente. |
| No es un recurso EntityDefinition/SceneSegmentDefinition | Usa Entidad en eventos `crear` y Tramo de escena en `tramos`. |
| Desaparece a los 20 segundos | Ajusta Defaults > Lifetime o `duracion_s`; revisa también temporizadores del script propio. |
| Aparece girado | Revisa Preserve Prefab Rotation y la rotación guardada del prefab. |
| Aparece demasiado alto, lejos o fuera de cámara | Ajusta `posicion`, origen de la ruta, pivote y escala del prefab. |
| El enemigo aparece pero no dispara | Revisa su script, objetivo, referencias, alcance, capas y configuración del adaptador. |
| El escenario no colisiona | Comprueba colliders, capas y configuración física del prefab. |
| Error de escena de tramo | Revisa Scene Path, inclusión en compilación, raíz única y Content hijo inactivo. |

Más detalles del formato y del sistema en [SistemaDeNiveles.md](SistemaDeNiveles.md).
