using Live2D.Cubism.Framework;
using UnityEngine;

public sealed class SuiyueBlinkLoop : MonoBehaviour
{
    public CubismEyeBlinkController Controller;
    public float IntervalSeconds = 3.2f;
    public float BlinkDurationSeconds = 0.3f;

    private float elapsed;
    private bool paused;

    public float CurrentOpening => Controller != null ? Controller.EyeOpening : 1f;
    public bool IsPaused => paused;

    private void Update()
    {
        if (Controller == null || paused) return;
        elapsed += Time.deltaTime;
        var interval = Mathf.Max(0.7f, IntervalSeconds);
        var duration = Mathf.Clamp(BlinkDurationSeconds, 0.12f, interval * 0.7f);
        var phase = elapsed % interval;
        if (phase < interval - duration)
        {
            Controller.EyeOpening = 1f;
            return;
        }

        var blink = (phase - (interval - duration)) / duration;
        if (blink < 0.3f)
            Controller.EyeOpening = 1f - Mathf.SmoothStep(0f, 1f, blink / 0.3f);
        else if (blink < 0.7f)
            Controller.EyeOpening = 0f;
        else
            Controller.EyeOpening = Mathf.SmoothStep(0f, 1f, (blink - 0.7f) / 0.3f);
    }

    public void SetPaused(bool value)
    {
        paused = value;
        elapsed = 0f;
        if (Controller != null) Controller.EyeOpening = 1f;
    }
}
