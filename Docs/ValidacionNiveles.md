# Validación del sistema de niveles

Fecha: 30 de septiembre de 2026. Unity: 6000.5.8f1.

## Resultados

- Compilación de los scripts de juego y del Editor: aprobada.
- `LevelSystemChecks.RunAll`: **22 comprobaciones aprobadas**. Incluyen lectura YAML, coordenadas, configuración independiente, contenido futuro deshabilitado, recursos y campos inválidos, valores no finitos, IDs duplicados, orden estable, saltos de progreso y no repetición de eventos.
- Generación de `GameRoot`, catálogo, prefabs, audio de prueba y dos escenas de tramo: completada.
- Validación del nivel importado y escenas incluidas: aprobada.
- `LevelPlayModeChecks.Run`: aprobada. Se verificó pausa del jugador, carga aditiva del segundo tramo, reinicio durante carga, nuevo inicio de eventos, creación de entidades, retirada del tramo anterior y limpieza final con recuperación del reloj.
- Compilación de scripts del jugador para macOS: aprobada; sin el assembly del Editor ni YamlDotNet entre las assemblies compiladas del jugador. YamlDotNet está configurado como plugin exclusivo del Editor.
- Revisión de espacios y errores de diff: aprobada.

## Límites de esta validación

Las pruebas de partida se ejecutaron en modo automático sin renderizado. No se ha medido rendimiento en un ejecutable completo ni comprobado visualmente la presentación o escuchado el audio. La compilación para macOS comprobó scripts; no generó una aplicación distribuible.

En el arranque del Editor se registró una excepción del indexador interno `UnityEditor.Search.SearchDatabase`, independiente del código de niveles; no impidió completar la prueba de partida. El primer bloqueo del servicio de licencias se resolvió cerrando el proceso que había dejado el intento inicial de validación.

Las instrucciones para repetir las comprobaciones están en `SistemaDeNiveles.md` y en el menú **Pyramid > Niveles**.
