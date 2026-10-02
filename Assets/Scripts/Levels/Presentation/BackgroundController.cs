using System.Collections;
using UnityEngine;

namespace Pyramid.Levels
{
    public sealed class BackgroundController : MonoBehaviour
    {
        Camera target;
        Material originalSky;
        Color originalColor;
        CameraClearFlags originalFlags;
        BackgroundDefinition current;
        GameObject backdrop;
        Coroutine transition;
        Vector3 anchor;
        bool captured;

        public void Capture(Camera camera)
        {
            if (captured) return;
            target = camera;
            originalSky = RenderSettings.skybox;
            if (target) { originalColor = target.backgroundColor; originalFlags = target.clearFlags; }
            captured = true;
        }
        public void Apply(BackgroundDefinition value, float seconds)
        {
            if (!value) return;
            if (transition != null) StopCoroutine(transition);
            if (backdrop) Destroy(backdrop);
            current = value;
            anchor = target ? target.transform.position : transform.position;
            RenderSettings.skybox = value.skybox;
            if (value.backdropPrefab) backdrop = Instantiate(value.backdropPrefab, anchor + value.cameraOffset, Quaternion.identity, transform);
            if (target) target.clearFlags = value.skybox ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
            transition = StartCoroutine(BlendColor(value.cameraColor, seconds));
        }
        IEnumerator BlendColor(Color color, float duration)
        {
            if (!target) yield break;
            var from = target.backgroundColor;
            float elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                target.backgroundColor = Color.Lerp(from, color, elapsed / duration);
                yield return null;
            }
            target.backgroundColor = color;
        }
        void LateUpdate()
        {
            if (backdrop && current && target)
                backdrop.transform.position = Vector3.Lerp(anchor, target.transform.position, current.followCamera) + current.cameraOffset;
        }
        public void Restore()
        {
            if (transition != null) StopCoroutine(transition);
            if (backdrop) { backdrop.SetActive(false); Destroy(backdrop); }
            current = null;
            if (!captured) return;
            RenderSettings.skybox = originalSky;
            if (target) { target.backgroundColor = originalColor; target.clearFlags = originalFlags; }
            captured = false;
        }
    }
}
