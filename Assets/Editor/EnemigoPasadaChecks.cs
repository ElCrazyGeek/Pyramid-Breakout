using System;
using System.Linq;
using System.Reflection;
using Pyramid.Levels;
using Pyramid.Levels.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>Pruebas deterministas del pase completo con el prefab real, sin modificar escenas.</summary>
public static class EnemigoPasadaChecks
{
    const string Folder = "Assets/Levels/Examples/Generated/";
    const string Example = "Assets/Levels/Examples/Pasadas.level";
    static int assertions;

    [MenuItem("Pyramid/Enemigos/Comprobar nave de pasada")]
    public static void Run()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Sal de Play antes de ejecutar esta prueba.");
        assertions = 0;
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "Nave de Pasada.prefab");
        Check(prefab != null, "Existe el prefab de la nave de pasada.");
        var ai = prefab.GetComponent<EnemigoPasada>();
        Check(ai && prefab.GetComponent<Health>() && prefab.GetComponent<Collider>(), "Prefab con comportamiento, vida y colisión.");
        Check(prefab.CompareTag("Enemy") && ai.boca && ai.prefabProyectil && ai.prefabProyectil.GetComponent<Proyectil>(),
            "Prefab conectado al modelo de combate y al proyectil enemigo.");
        Check(prefab.GetComponent<EnemigoPerseguidor>() == null && prefab.GetComponent<EnemigoSeguidor>() == null,
            "Solo un comportamiento de movimiento controla la nave.");

        CheckCatalog(prefab);
        using (var fixture = new Fixture(prefab)) CheckPasses(fixture);
        foreach (float playerX in new[] { -50f, 50f })
            foreach (bool fromRight in new[] { false, true })
                using (var fixture = new Fixture(prefab)) CheckEdgeAttack(fixture, playerX, fromRight);
        using (var fixture = new Fixture(prefab)) CheckMovementAndGuards(fixture);
        using (var fixture = new Fixture(prefab)) CheckConfiguration(fixture);
        LevelSystemChecks.RunAll();
        LevelBuildValidator.ValidateAll();
        Debug.Log($"PYRAMID_PASADA_CHECKS_OK: {assertions} comprobaciones del prefab, disparo, salida y repetición.");
    }

    static void CheckCatalog(GameObject prefab)
    {
        var catalog = AssetDatabase.LoadAssetAtPath<ContentCatalog>(Folder + "Catalog.asset");
        Check(catalog != null, "Existe el catálogo de contenido.");
        Check(catalog.BuildIndex().TryGetValue("enemigo.pasada", out var entry) &&
            entry is EntityDefinition definition && definition.prefab == prefab,
            "El catálogo resuelve enemigo.pasada al prefab correcto.");
        AssetDatabase.ImportAsset(Example, ImportAssetOptions.ForceSynchronousImport);
        var imported = AssetDatabase.LoadAssetAtPath<LevelDefinition>(Example);
        Check(imported && imported.valid, "El ejemplo .level se importa sin errores: " + (imported ? imported.diagnostics : "archivo ausente"));
        Check(imported.events.Any(item => item.action == LevelAction.Spawn && item.resource == entry),
            "El ejemplo crea la nueva nave.");
    }

    static void CheckPasses(Fixture fixture)
    {
        var ship = fixture.Ship;
        fixture.Step(0.001f);
        Check(ship.PasadasIniciadas == 1, "El primer pase comienza automáticamente.");
        Check(ship.transform.position.z > fixture.Player.position.z && ship.transform.position.x > fixture.Player.position.x,
            "La entrada comienza adelante y en un lateral.");
        Check(!fixture.Visible(), "La malla completa comienza fuera del encuadre.");
        Check(ship.transform.forward.z < -0.2f && Mathf.Abs(ship.transform.forward.x) > 0.2f,
            "La nave recorre una diagonal hacia atrás respecto al riel.");

        bool entered = false, passed = false, waited = false;
        float pauseTime = 0f;
        float closestHorizontal = float.PositiveInfinity;
        int firstPass = ship.PasadasIniciadas;
        for (int i = 0; i < 2000 && ship.PasadasIniciadas == firstPass; i++)
        {
            bool wasVisible = fixture.Visible();
            string previousState = ship.Estado.ToString();
            int previousShots = fixture.Shots;
            Vector3 previousRelative = ship.transform.position - fixture.Player.position;
            fixture.Step(0.02f, 30f * 0.02f);
            entered |= fixture.Visible();
            Vector3 relative = ship.transform.position - fixture.Player.position;
            closestHorizontal = Mathf.Min(closestHorizontal, new Vector2(relative.x, relative.z).magnitude);
            if (!passed && ship.transform.position.z <= fixture.Player.position.z)
            {
                passed = true;
                float fraction = previousRelative.z / (previousRelative.z - relative.z);
                float lateralAtCrossing = Mathf.Lerp(previousRelative.x, relative.x, fraction);
                Check(Mathf.Abs(lateralAtCrossing) >= ship.separacionPaso - 0.01f,
                    "Cruza el plano del jugador con la separación lateral configurada.");
                Check(lateralAtCrossing < 0f, "Cruza por delante del jugador y lo rebasa por el lado opuesto a la entrada.");
            }
            if (ship.Estado.ToString() == "Espera")
            {
                waited = true;
                pauseTime += 0.02f;
                Check(!fixture.Visible(), "La espera comienza con toda la nave fuera de cámara.");
            }
            if (previousState == "Salida" || previousState == "Espera")
                Check(fixture.Shots == previousShots, "No dispara durante la salida o la espera.");
            if (ship.PasadasIniciadas != firstPass)
            {
                Check(!wasVisible, "Solo recoloca la nave después de salir completamente de cámara.");
                Check(!fixture.Visible(), "El siguiente pase también aparece fuera de cámara.");
            }
        }

        Check(entered, "La trayectoria atraviesa el encuadre.");
        Check(fixture.Shots > 0, "La nave dispara durante el pase.");
        Check(closestHorizontal >= ship.separacionPaso - 0.01f && closestHorizontal <= ship.separacionPaso + 0.1f,
            "La distancia horizontal mínima conserva la separación configurada durante toda la diagonal.");
        Check(passed && waited, "Completa el pase junto al jugador y la salida antes de esperar.");
        Check(pauseTime >= ship.pausaEntrePasadas - 0.05f, "Respeta la pausa entre pasadas.");
        Check(ship.PasadasIniciadas == firstPass + 1, "Repite el ciclo sin destruir la nave.");
        Check(ship.transform.position.x < fixture.Player.position.x, "Alterna el lateral de entrada.");
        Check(ship.transform.position.z > fixture.Player.position.z, "La nueva entrada sigue adelante del jugador que avanza.");
    }

    static void CheckEdgeAttack(Fixture fixture, float playerX, bool fromRight)
    {
        fixture.Player.position = new Vector3(playerX, 0f, 0f);
        fixture.Camera.transform.position = new Vector3(playerX * 0.5f, 5f, -30f);
        fixture.Ship.entrarPorDerecha = fromRight;
        fixture.Step(0.001f);
        string context = $"jugador x={playerX}, entrada {(fromRight ? "derecha" : "izquierda")}";
        Check(!fixture.Visible(), "Comienza fuera de cámara con " + context);
        bool entered = false;
        for (int i = 0; i < 2000 && fixture.Ship.Estado.ToString() != "Espera"; i++)
        {
            fixture.Step(0.02f, 30f * 0.02f);
            entered |= fixture.Visible();
        }
        Check(entered, "El pase es visible con " + context);
        Check(fixture.Shots > 0, "Ataca durante el pase visible con " + context);
        Check(fixture.Ship.Estado.ToString() == "Espera" && !fixture.Visible(),
            "Sale completamente de cámara con " + context);
    }

    static void CheckMovementAndGuards(Fixture fixture)
    {
        var ship = fixture.Ship;
        fixture.Step(0.001f);
        Vector3 before = ship.transform.position;
        Vector3 direction = ship.transform.forward;
        fixture.Player.position += new Vector3(15f, -5f, 0f);
        fixture.Player.rotation = Quaternion.Euler(25f, 40f, 70f);
        fixture.Step(0.05f, 2.5f);
        Vector3 expected = before + direction * ship.velocidad * 0.05f + Vector3.forward * 2.5f;
        Check(Vector3.Distance(ship.transform.position, expected) < 0.01f,
            "Acompaña el avance del riel sin copiar la evasión lateral ni el giro del jugador.");
        Check(Vector3.Angle(ship.transform.forward, direction) < 0.01f, "Mantiene el rumbo del pase aunque el jugador apunte o gire.");

        before = ship.transform.position;
        int shots = fixture.Shots;
        fixture.Step(0f);
        Check(ship.transform.position == before && fixture.Shots == shots, "No avanza ni dispara con el tiempo detenido.");
        fixture.Player.gameObject.SetActive(false);
        fixture.Step(0.5f);
        Check(ship.transform.position == before && fixture.Shots == shots, "Un objetivo desactivado suspende el ataque.");
        fixture.Player.gameObject.SetActive(true);
        ship.GetComponent<Health>().ResetHealth(0f);
        fixture.Step(0.5f);
        Check(ship.transform.position == before && fixture.Shots == shots, "Una nave sin vida no se mueve ni dispara.");
    }

    static void CheckConfiguration(Fixture fixture)
    {
        var ship = fixture.Ship;
        foreach (string key in new[] { "velocidad", "cadencia", "dano", "alcance", "entrada", "lateral", "separacion", "salida", "pausa", "lado", "alternar" })
            Check(ship.SupportsParameter(key), "Parámetro de nivel admitido: " + key);
        Check(!ship.SupportsParameter("parametro_inexistente"), "Rechaza parámetros desconocidos.");
        var settings = new EntitySettings { speed = 70f, fireRate = 3f, damage = 4f, range = 100f };
        settings.extra.Add(new EntityParameter { key = "lado", value = "izquierda" });
        settings.extra.Add(new EntityParameter { key = "alternar", value = "false" });
        settings.extra.Add(new EntityParameter { key = "pausa", value = "2.25" });
        settings.extra.Add(new EntityParameter { key = "separacion", value = "25" });
        Check(ship.ValidateSettings(settings) == null, "Configuración válida del adaptador.");
        ship.Configure(settings, fixture.Player, fixture.Camera);
        Check(!ship.enabled && ship.objetivo == fixture.Player && ship.camara == fixture.Camera,
            "Configure espera a Begin y enlaza jugador y cámara.");
        Check(ship.velocidad == 70f && ship.cadenciaDisparo == 3f && ship.danio == 4f && ship.alcance == 100f &&
            ship.pausaEntrePasadas == 2.25f && ship.separacionPaso == 25f && !ship.alternarLado && !ship.entrarPorDerecha,
            "Aplica los parámetros de combate y del pase.");
        ship.Begin();
        Check(ship.enabled, "Begin activa el comportamiento.");
        ship.End();
        Check(!ship.enabled, "End desactiva el comportamiento.");
        settings.extra.Find(item => item.key == "pausa").value = "NaN";
        Check(ship.ValidateSettings(settings) != null, "Rechaza tiempos no finitos.");
        settings.extra.Find(item => item.key == "pausa").value = "-1";
        Check(ship.ValidateSettings(settings) != null, "Rechaza tiempos negativos.");
    }

    static void Check(bool condition, string message)
    {
        assertions++;
        if (!condition) throw new Exception("Prueba de pasada fallida: " + message);
    }

    sealed class Fixture : IDisposable
    {
        public readonly EnemigoPasada Ship;
        public readonly Transform Player;
        public readonly Camera Camera;
        public int Shots { get; private set; }
        readonly Scene scene;
        readonly MethodInfo advance;
        readonly Renderer[] renderers;

        public Fixture(GameObject prefab)
        {
            scene = EditorSceneManager.NewPreviewScene();
            Player = NewObject("Jugador de prueba").transform;
            Camera = NewObject("Cámara de prueba").AddComponent<Camera>();
            Camera.transform.position = new Vector3(0f, 5f, -30f);
            Camera.fieldOfView = 60f;
            Camera.aspect = 16f / 9f;
            Camera.nearClipPlane = 0.3f;
            Camera.farClipPlane = 1000f;
            var instance = Object.Instantiate(prefab);
            SceneManager.MoveGameObjectToScene(instance, scene);
            Ship = instance.GetComponent<EnemigoPasada>();
            Ship.objetivo = Player;
            Ship.camara = Camera;
            Ship.GetComponent<Health>().ResetHealth(20f);
            // Las balas conservan su contrato real, sin ejecutar audio, físicas ni Start en modo edición.
            var projectile = NewObject("Proyectil de comprobación");
            projectile.SetActive(false);
            projectile.AddComponent<Proyectil>();
            Ship.prefabProyectil = projectile;
            Invoke("Awake");
            Invoke("OnEnable");
            advance = typeof(EnemigoPasada).GetMethod("Avanzar", BindingFlags.Instance | BindingFlags.NonPublic);
            if (advance == null) throw new MissingMethodException("EnemigoPasada.Avanzar(float)");
            renderers = Ship.GetComponentsInChildren<Renderer>();
        }

        GameObject NewObject(string name)
        {
            var item = new GameObject(name);
            SceneManager.MoveGameObjectToScene(item, scene);
            return item;
        }

        void Invoke(string name) => typeof(EnemigoPasada).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(Ship, null);

        public void Step(float deltaTime, float advancePlayer = 0f)
        {
            Player.position += Vector3.forward * advancePlayer;
            Camera.transform.position += Vector3.forward * advancePlayer;
            try { advance.Invoke(Ship, new object[] { deltaTime }); }
            catch (TargetInvocationException error) { throw error.InnerException ?? error; }
            ClearShots(true);
        }

        public bool Visible()
        {
            Plane[] planes = GeometryUtility.CalculateFrustumPlanes(Camera);
            return renderers.Any(renderer => renderer && renderer.enabled && GeometryUtility.TestPlanesAABB(planes, renderer.bounds));
        }

        void ClearShots(bool validate)
        {
            foreach (var shot in Resources.FindObjectsOfTypeAll<Proyectil>())
            {
                if (!shot || EditorUtility.IsPersistent(shot) || shot.origen != Ship.gameObject) continue;
                if (validate)
                {
                    Shots++;
                    Check(shot.tagOrigen == "Enemy" && shot.danio == Ship.danio, "Cada disparo identifica su bando, emisor y daño.");
                    Vector3 direction = (Player.position - shot.transform.position).normalized;
                    Check(Vector3.Dot(shot.transform.forward, direction) > 0.99f, "El disparo apunta al jugador sin girar el rumbo de la nave.");
                    Check(Ship.transform.position.z > Player.position.z, "Solo dispara mientras se encuentra delante del jugador.");
                }
                Object.DestroyImmediate(shot.gameObject);
            }
        }

        public void Dispose()
        {
            ClearShots(false);
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }
}
