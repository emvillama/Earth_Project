using UnityEngine;

// One place to birth AudioSources. Every emitter in the project needs the same base setup —
// playOnAwake off, and the right spatialize flag. 3D sources route through the Steam Audio (3.3)
// HRTF spatializer (spatialize=true); 2D diffuse layers (beds/weather) stay unspatialized. NOTE:
// spatialize=true only works with the Steam Audio spatializer plugin set in Project Settings →
// Audio — without a plugin it silently kills audio (we hit that before). Keep them in sync.
public static class AudioFactory
{
    // 2D diffuse source (beds, weather) — present everywhere, no positional rolloff.
    public static AudioSource Add2D(GameObject go, bool loop)
    {
        var s = go.AddComponent<AudioSource>();
        s.playOnAwake = false;
        s.loop = loop;
        s.spatialBlend = 0f;
        s.spatialize = false;
        return s;
    }

    // 3D positional source (river emitters, per-object callers) with distance rolloff.
    public static AudioSource Add3D(GameObject go, bool loop, float minDistance,
                                    AudioRolloffMode rolloff = AudioRolloffMode.Linear)
    {
        var s = go.AddComponent<AudioSource>();
        s.playOnAwake = false;
        s.loop = loop;
        s.spatialBlend = 1f;
        s.spatialize = true;   // HRTF binaural via the Steam Audio spatializer plugin
        s.minDistance = minDistance;
        s.rolloffMode = rolloff;
        return s;
    }
}
