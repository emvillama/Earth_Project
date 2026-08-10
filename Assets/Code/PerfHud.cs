using UnityEngine;

// 3.2 performance pass — frame-rate cap + on-device profiling overlay.
//
// Earth is audio-first (there's no meaningful visual world — a static image sits over the 3D scene),
// so we cap the render frame rate to save battery and heat; the audio runs on its own thread and is
// unaffected. The overlay (top-left) reports FPS + frame time, the audio voice count (audible / total
// tracked), and how many spawned sound-objects are live vs. pooled — so we can walk the app and read
// real numbers back, then tune maxVoices / spawn budget / frame cap.
//
// Created by ItemSpawner.BeginWorld(). Set `show = false` (or lower targetFrameRate) once tuned.
public class PerfHud : MonoBehaviour
{
    public ItemSpawner spawner;
    public bool show = true;
    [Tooltip("Render cap. 60 = smooth; 30 favours battery/thermals (fine for an audio-first app).")]
    public int targetFrameRate = 60;

    private float smoothDelta = 0.016f;
    private GUIStyle style;

    void Awake()
    {
        QualitySettings.vSyncCount = 0;                 // let targetFrameRate govern, not the display
        Application.targetFrameRate = targetFrameRate;
    }

    void Update()
    {
        // Exponential moving average of frame time → a stable FPS read that doesn't jitter every frame.
        smoothDelta = Mathf.Lerp(smoothDelta, Time.unscaledDeltaTime, 0.1f);
    }

    void OnGUI()
    {
        if (!show) return;
        if (style == null)
            style = new GUIStyle(GUI.skin.label) { fontSize = 40, fontStyle = FontStyle.Bold };

        float fps = smoothDelta > 0f ? 1f / smoothDelta : 0f;
        int voices  = VoiceManager.HasInstance ? VoiceManager.Instance.AudibleVoices : 0;
        int tracked = VoiceManager.HasInstance ? VoiceManager.Instance.TrackedVoices : 0;
        int live    = spawner != null ? spawner.ActiveItems : 0;
        int pooled  = spawner != null ? spawner.PooledObjects : 0;

        string t = string.Format("{0:0} fps  ({1:0.0} ms)\nvoices {2}/{3}\nobjects {4}  (pool {5})",
                                  fps, smoothDelta * 1000f, voices, tracked, live, pooled);

        var r = new Rect(28, 40, 700, 220);
        DrawShadowed(r, t);
    }

    private void DrawShadowed(Rect r, string t)
    {
        Color prev = style.normal.textColor;
        style.normal.textColor = new Color(0f, 0f, 0f, 0.75f);
        GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), t, style);
        style.normal.textColor = Color.white;
        GUI.Label(r, t, style);
        style.normal.textColor = prev;
    }
}
