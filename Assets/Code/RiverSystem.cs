using System.Collections.Generic;
using UnityEngine;

// River / stream (CANOPY 2.4) — a single, discoverable, wandering river (Minecraft-style).
//
// There is never more than one river. When none exists, it periodically has a *chance* to
// seed at a random spot away from the player, so you only "come upon" one as you explore.
// Its course is a smooth meander (gentle seeded turns — windy, not straight, and consistent
// if you backtrack) that extends whichever way you follow it. Width varies along its length
// (smooth noise) so stretches are wide/loud or a quiet trickle. Walk far enough from it and
// you leave it behind; after a cooldown a new one can appear elsewhere.
//
// Emitters are static 3D looping sources placed along the nearby stretch of the course
// (volume + radius scaled by local width), started at random loop offsets so they don't flange.
// Auto-created by ItemSpawner when the biome has a riverClip.
public class RiverSystem : MonoBehaviour
{
    [Header("Discovery (one river at a time)")]
    public bool riverActive = true;
    [Tooltip("Chance, each ~2s while no river exists, that one appears nearby. Higher = come upon rivers more often.")]
    [Range(0f, 1f)] public float discoveryChance = 0.12f;
    [Tooltip("How far away a new river seeds from you (you then wander into earshot).")]
    public float discoverMin = 45f;
    public float discoverMax = 95f;
    [Tooltip("Leave the river by this distance and it despawns (a new one can appear later).")]
    public float leaveDistance = 140f;
    public float rediscoverCooldown = 12f;

    [Header("Course shape (windiness)")]
    [Tooltip("Spacing between course nodes.")]
    public float segmentLength = 12f;
    [Tooltip("Max bend per segment — higher = curvier river.")]
    [Range(0f, 45f)] public float maxTurnDeg = 20f;

    [Header("Width: wide/loud vs narrow/trickle")]
    [Range(0f, 1f)] public float widthMin = 0.3f;
    [Range(0f, 1f)] public float widthMax = 1f;
    [Tooltip("How quickly width varies along the river (smaller = long wide/narrow stretches).")]
    public float widthNoiseScale = 0.11f;

    [Header("Sound")]
    public float baseVolume = 0.7f;
    [Tooltip("Audible radius at full width (scaled down for trickles).")]
    public float baseRadius = 34f;
    public float groundY = 9f;

    public AudioClip waterClip;
    public Transform player;

    private struct RNode { public Vector3 pos; public float headingRad; public float width; }
    private readonly Dictionary<int, RNode> nodes = new Dictionary<int, RNode>();
    private readonly Dictionary<int, AudioSource> emitters = new Dictionary<int, AudioSource>();
    private readonly Stack<AudioSource> pool = new Stack<AudioSource>();
    private readonly List<int> scratch = new List<int>();

    private bool hasRiver;
    private float riverSeed;
    private Vector3 origin;
    private float baseHeadingRad;
    private int nearIndex;
    private float tick, discoverTick, cooldown;

    private float TurnRad(int i)
    {
        // smooth seeded turn per segment → gentle, natural meander (not jagged)
        return (Mathf.PerlinNoise(riverSeed + i * 0.35f, 13.7f) * 2f - 1f) * (maxTurnDeg * Mathf.Deg2Rad);
    }

    private float WidthAt(int i)
    {
        return Mathf.Lerp(widthMin, widthMax, Mathf.PerlinNoise(riverSeed * 0.5f + i * widthNoiseScale, 71.3f));
    }

    private RNode Node(int i)
    {
        if (nodes.TryGetValue(i, out var n)) return n;
        RNode r;
        if (i == 0)
        {
            r.headingRad = baseHeadingRad;
            r.pos = origin;
        }
        else if (i > 0)
        {
            RNode p = Node(i - 1);
            r.headingRad = p.headingRad + TurnRad(i);
            r.pos = p.pos + new Vector3(Mathf.Cos(r.headingRad), 0f, Mathf.Sin(r.headingRad)) * segmentLength;
        }
        else // i < 0: step backward from node i+1
        {
            RNode p = Node(i + 1);
            r.headingRad = p.headingRad - TurnRad(i + 1);
            r.pos = p.pos - new Vector3(Mathf.Cos(p.headingRad), 0f, Mathf.Sin(p.headingRad)) * segmentLength;
        }
        r.pos.y = groundY;
        r.width = WidthAt(i);
        nodes[i] = r;
        return r;
    }

    void Update()
    {
        if (player == null || waterClip == null || !riverActive)
        {
            if (hasRiver && !riverActive) Despawn();
            return;
        }

        tick += Time.deltaTime;
        if (tick < 0.25f) return;
        tick = 0f;

        if (!hasRiver)
        {
            if (cooldown > 0f) { cooldown -= 0.25f; return; }
            discoverTick += 0.25f;
            if (discoverTick >= 2f)
            {
                discoverTick = 0f;
                if (Random.value < discoveryChance) Discover();
            }
            return;
        }

        // Track the node nearest the player (local search along the course).
        UpdateNearIndex();
        float nearestDist = Vector3.Distance(player.position, Node(nearIndex).pos);
        if (nearestDist > leaveDistance)
        {
            Despawn();
            cooldown = rediscoverCooldown;
            return;
        }

        // Place emitters along the nearby stretch; recycle the rest.
        int span = Mathf.CeilToInt((baseRadius + segmentLength) / segmentLength) + 1;
        for (int k = nearIndex - span; k <= nearIndex + span; k++)
        {
            RNode nd = Node(k);
            float radius = baseRadius * nd.width;
            float vol = baseVolume * nd.width;
            if (emitters.TryGetValue(k, out var live))
            {
                live.maxDistance = radius;
                live.volume = vol;
                continue;
            }
            AudioSource src = GetEmitter();
            src.transform.position = nd.pos;
            src.maxDistance = radius;
            src.volume = vol;
            src.Play();
            if (waterClip.length > 1f) src.time = Random.Range(0f, waterClip.length);
            emitters[k] = src;
        }
        scratch.Clear();
        foreach (var kv in emitters)
            if (kv.Key < nearIndex - span || kv.Key > nearIndex + span) scratch.Add(kv.Key);
        foreach (int k in scratch) { Recycle(emitters[k]); emitters.Remove(k); }
    }

    private void UpdateNearIndex()
    {
        // walk the index toward the player's nearest node (a few steps per tick is plenty)
        for (int step = 0; step < 6; step++)
        {
            float d0 = (Node(nearIndex).pos - player.position).sqrMagnitude;
            float dUp = (Node(nearIndex + 1).pos - player.position).sqrMagnitude;
            float dDn = (Node(nearIndex - 1).pos - player.position).sqrMagnitude;
            if (dUp < d0 && dUp <= dDn) nearIndex++;
            else if (dDn < d0) nearIndex--;
            else break;
        }
    }

    private void Discover()
    {
        float ang = Random.value * Mathf.PI * 2f;
        float dist = Random.Range(discoverMin, discoverMax);
        origin = player.position + new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * dist;
        origin.y = groundY;
        riverSeed = Random.value * 1000f;
        baseHeadingRad = Random.value * Mathf.PI * 2f;
        nodes.Clear();
        nearIndex = 0;
        hasRiver = true;
    }

    private void Despawn()
    {
        foreach (var kv in emitters) Recycle(kv.Value);
        emitters.Clear();
        nodes.Clear();
        hasRiver = false;
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
        s.spatialBlend = 1f;
        s.spatialize = false;
        s.minDistance = 4f;
        s.rolloffMode = AudioRolloffMode.Linear;
        return s;
    }

    private void Recycle(AudioSource s)
    {
        s.Stop();
        s.gameObject.SetActive(false);
        pool.Push(s);
    }
}
