using System;
using System.Collections;
using UnityEngine;

namespace TikTokFactionWar
{
    /// <summary>
    /// Easing curves and coroutine tween helper (runs on unscaled time by default so UI/shell animations work while paused).
    /// </summary>
    public static class Ease
    {
        public static float OutCubic(float t)
        {
            float f = 1f - Mathf.Clamp01(t);
            return 1f - f * f * f;
        }

        public static float OutBack(float t)
        {
            t = Mathf.Clamp01(t);
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float tm1 = t - 1f;
            return 1f + c3 * tm1 * tm1 * tm1 + c1 * tm1 * tm1;
        }

        public static float OutElastic(float t)
        {
            t = Mathf.Clamp01(t);
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;
            const float c4 = (2f * Mathf.PI) / 3f;
            return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
        }

        public static IEnumerator Tween(
            float duration,
            Action<float> apply,
            Func<float, float> ease,
            bool unscaled = true)
        {
            if (duration <= 0.0001f)
            {
                apply?.Invoke(1f);
                yield break;
            }

            for (float t = 0f; t < 1f; t += (unscaled ? Time.unscaledDeltaTime : Time.deltaTime) / duration)
            {
                apply?.Invoke(ease(Mathf.Clamp01(t)));
                yield return null;
            }

            apply?.Invoke(1f);
        }
    }
}
