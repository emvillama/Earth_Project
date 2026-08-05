using UnityEngine;

// One place to birth AudioSources. Every emitter in the project needs the same base setup —
// playOnAwake off and, critically, spatialize=false (a stray spatialize=true routing to a null
// plugin silently killed all audio once). Centralizing it means that convention can't drift
// between the bed, river, weather, and per-object systems.
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
        s.spatialize = false;
        s.minDistance = minDistance;
        s.rolloffMode = rolloff;
        return s;
    }
}
