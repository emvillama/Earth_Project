using System.Collections.Generic;
using UnityEngine;

// River / stream (CANOPY 2.4). A world-anchored line of looping water emitters along a
// meandering course. The river flows generally along +X and meanders in Z (Perlin), so it
// reads as one coherent directional band, not scattered points. Its position is a pure
// function of world-X, so it stays put in the world; as the player moves, emitters are added
// ahead and recycled behind (pooled), extending the river with the procedural landscape while
// keeping the audio continuous. Each emitter is a STATIC 3D looping source (no moving-source
// artifacts); emitters start at random loop offsets so they don't phase-align.
//
// Auto-created by ItemSpawner when the biome has a riverClip. Tune the sliders on this
// component (select the "RiverSystem" object at runtime).
public class RiverSystem : MonoBehaviour
{
    [Header("Enable / density")]
    public bool riverActive = true;
    [Tooltip("Distance between water emitters. SMALLER = denser, louder, fuller river (the test slider).")]
    [Range(6f, 60f)] public float emitterSpacing = 22f;
    [Tooltip("How far each emitter carries. Keep >= spacing so they overlap into a continuous line.")]
    public float audibleRadius = 40f;
    [Range(0f, 1f)] public float volume = 0.6f;

    [Header("Course (world-anchored)")]
    [Tooltip("The river's base cross-position in world Z.")]
    public float channelZ = 0f;
    [Tooltip("How far the river meanders sideways from its channel.")]
    public float meanderAmplitude = 30f;
    [Tooltip("Meander tightness (smaller = longer, lazier bends).")]
    public float meanderFrequency = 0.012f;
    [Tooltip("Height of the water near the world floor.")]
    public float groundY = 9f;

    public AudioClip waterClip;
    public Transform player;

    private readonly Dictionary<int, AudioSource> emitters = new Dictionary<int, AudioSource>();
    private readonly Stack<AudioSource> pool = new Stack<AudioSource>();
    private readonly List<int> toRemove = new List<int>();
    private float tick;

    // River Z at a given world X — a smooth meander so the course flows coherently.
    private float RiverZ(float x)
    {
        return channelZ + meanderAmplitude * (Mathf.PerlinNoise(x * meanderFrequency, 0.37f) * 2f - 1f);
    }

    void Update()
    {
        if (player == null || waterClip == null)
        {
            return;
        }
        tick += Time.deltaTime;
        if (tick < 0.25f)
        {
            return;
        }
        tick = 0f;

        if (!riverActive)
        {
            ClearAll();
            return;
        }

        float reach = audibleRadius + emitterSpacing;
        float px = player.position.x;
        int kMin = Mathf.FloorToInt((px - reach) / emitterSpacing);
        int kMax = Mathf.CeilToInt((px + reach) / emitterSpacing);

        for (int k = kMin; k <= kMax; k++)
        {
            if (emitters.TryGetValue(k, out var live))
            {
                live.maxDistance = audibleRadius; // keep synced with the sliders
                live.volume = volume;
                continue;
            }
            float x = k * emitterSpacing;
            AudioSource src = GetEmitter();
            src.transform.position = new Vector3(x, groundY, RiverZ(x));
            src.maxDistance = audibleRadius;
            src.volume = volume;
            src.Play();
            if (waterClip.length > 1f)
            {
                src.time = Random.Range(0f, waterClip.length); // decorrelate so they don't flange
            }
            emitters[k] = src;
        }

        toRemove.Clear();
        foreach (var kv in emitters)
        {
            if (kv.Key < kMin || kv.Key > kMax) toRemove.Add(kv.Key);
        }
        foreach (int k in toRemove)
        {
            Recycle(emitters[k]);
            emitters.Remove(k);
        }
    }

    private AudioSource GetEmitter()
    {
        AudioSource s = pool.Count > 0 ? pool.Pop() : NewEmitter();
        s.gameObject.SetActive(true);
        return s;
    }

    private AudioSource NewEmitter()
    {
        var go = new GameObject("RiverEmitter");
        go.transform.SetParent(transform);
        var s = go.AddComponent<AudioSource>();
        s.clip = waterClip;
        s.loop = true;
        s.playOnAwake = false;
        s.spatialBlend = 1f;   // 3D: located, so you hear the river from the right direction
        s.spatialize = false;
        s.minDistance = 4f;
        s.rolloffMode = AudioRolloffMode.Linear; // even overlap between neighbours
        return s;
    }

    private void Recycle(AudioSource s)
    {
        s.Stop();
        s.gameObject.SetActive(false);
        pool.Push(s);
    }

    private void ClearAll()
    {
        foreach (var kv in emitters)
        {
            Recycle(kv.Value);
        }
        emitters.Clear();
    }
}
