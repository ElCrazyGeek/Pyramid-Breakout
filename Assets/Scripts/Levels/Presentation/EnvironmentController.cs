using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace Pyramid.Levels
{
    public sealed class EnvironmentController : MonoBehaviour
    {
        public Light directionalLight;
        Color originalAmbient, originalFog, originalLight;
        float originalDensity, originalIntensity;
        bool originalFogEnabled, captured;
        FogMode originalFogMode;
        AmbientMode originalAmbientMode;
        Quaternion originalRotation;
        Coroutine transition;
        GameObject particles;
        Volume volume;

        public void Capture()
        {
            if (captured) return;
            captured = true;
            originalAmbient = RenderSettings.ambientLight;
            originalAmbientMode = RenderSettings.ambientMode;
            originalFog = RenderSettings.fogColor;
            originalDensity = RenderSettings.fogDensity;
            originalFogEnabled = RenderSettings.fog;
            originalFogMode = RenderSettings.fogMode;
            if (directionalLight)
            {
                originalLight = directionalLight.color;
                originalIntensity = directionalLight.intensity;
                originalRotation = directionalLight.transform.rotation;
            }
        }
        public void Apply(EnvironmentDefinition value, float seconds)
        {
            if (!value) return;
            Capture();
            if (transition != null) StopCoroutine(transition);
            if (particles) { particles.SetActive(false); Destroy(particles); }
            if (value.particlesPrefab) particles = Instantiate(value.particlesPrefab, transform);
            if (!volume)
            {
                var go = new GameObject("Volumen del nivel");
                go.transform.SetParent(transform, false);
                volume = go.AddComponent<Volume>();
                volume.isGlobal = true;
                volume.priority = 100;
            }
            // Perfiles y partículas son cambios discretos; luz y niebla se interpolan.
            volume.sharedProfile = value.volumeProfile;
            transition = StartCoroutine(Blend(value, seconds));
        }
        IEnumerator Blend(EnvironmentDefinition value, float duration)
        {
            var ambient = RenderSettings.ambientLight;
            var fogColor = RenderSettings.fogColor;
            float density = RenderSettings.fog ? RenderSettings.fogDensity : 0;
            var lightColor = directionalLight ? directionalLight.color : Color.white;
            float intensity = directionalLight ? directionalLight.intensity : 0;
            var rotation = directionalLight ? directionalLight.transform.rotation : Quaternion.identity;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fog = RenderSettings.fog || value.fog;
            float elapsed = 0;
            do
            {
                elapsed += Time.deltaTime;
                float t = duration <= 0 ? 1 : Mathf.Clamp01(elapsed / duration);
                RenderSettings.ambientLight = Color.Lerp(ambient, value.ambient, t);
                RenderSettings.fogColor = Color.Lerp(fogColor, value.fogColor, t);
                RenderSettings.fogDensity = Mathf.Lerp(density, value.fog ? value.fogDensity : 0, t);
                if (directionalLight)
                {
                    directionalLight.color = Color.Lerp(lightColor, value.lightColor, t);
                    directionalLight.intensity = Mathf.Lerp(intensity, value.lightIntensity, t);
                    directionalLight.transform.rotation = Quaternion.Slerp(rotation, Quaternion.Euler(value.lightEuler), t);
                }
                if (t >= 1) break;
                yield return null;
            } while (true);
            RenderSettings.fog = value.fog;
        }
        public void Restore()
        {
            if (transition != null) StopCoroutine(transition);
            if (particles) { particles.SetActive(false); Destroy(particles); }
            if (volume) { volume.enabled = false; Destroy(volume.gameObject); }
            if (!captured) return;
            RenderSettings.ambientLight = originalAmbient;
            RenderSettings.ambientMode = originalAmbientMode;
            RenderSettings.fog = originalFogEnabled;
            RenderSettings.fogColor = originalFog;
            RenderSettings.fogDensity = originalDensity;
            RenderSettings.fogMode = originalFogMode;
            if (directionalLight)
            {
                directionalLight.color = originalLight;
                directionalLight.intensity = originalIntensity;
                directionalLight.transform.rotation = originalRotation;
            }
            captured = false;
        }
    }
}
