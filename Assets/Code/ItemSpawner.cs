using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class ItemSpawner : MonoBehaviour
{
    public static int length = 150;
    public GameObject cube;
    public GameObject player;
    public GameObject item;
    public int detailScale = 8;
    public int noiseHeight = 3;
    private Vector3 startPos = Vector3.zero;
    private Dictionary<Vector3, Tile> itemPos;
    public int itemChance = 1;
    public int itemMax = 40;

    private ObjectPool<GameObject> pool;
    private Transform mainCamera;

    public SpawnConfig config;
    public BiomeProfileSet biome;

    [Header("Call-and-response — live tuning")]
    [Tooltip("When on, these values override every species' neighborBias/neighborRadius so you " +
             "can tune clustering globally in Play mode. Off = each SoundProfile uses its own.")]
    public bool overrideNeighbor = false;
    [Tooltip("Global clustering chance while override is on. 0 = spread evenly, 1 = always cluster.")]
    [Range(0f, 1f)] public float neighborBiasOverride = 0.6f;
    [Tooltip("Global cluster radius while override is on (world units).")]
    public float neighborRadiusOverride = 20f;

    [Header("Flyovers — live tuning")]
    [Tooltip("Master on/off for birds passing overhead. Only species with canFlyover are eligible.")]
    public bool enableFlyovers = false;
    [Tooltip("Chance an eligible (mobile) bird spawns as a fly-over instead of perched.")]
    [Range(0f, 1f)] public float flyoverChance = 0.15f;
    [Tooltip("How fast fly-overs cross the sky (world units/sec).")]
    public float flyoverSpeed = 12f;
    [Tooltip("Height band above the player that fly-overs cross at (world units).")]
    public float flyoverHeightMin = 20f;
    public float flyoverHeightMax = 40f;

    private int rndTimeMin = 5;
    private int rndTimeMax = 10;
    private float fadeInDuration = 0.05f;
    private float fadeOutDuration = 0.25f;
    private float minDistance = 3f;
    private float audibleRadius = 80f;
    private float playerExclusionRadius = 10f;
    private float densityContrast = 0.5f;
    private float densityScale = 40f;
    private const float DensityOffset = 3137f; // decorrelate density noise from height noise
    private readonly Dictionary<SoundProfile, int> activeCounts = new Dictionary<SoundProfile, int>();
    private float spawnIntervalMin = 1.0f;
    private float spawnIntervalMax = 3.0f;
    private const float ManageInterval = 0.3f;
    private float manageTimer = 0f;
    private float nextSpawnTime = 0f;

    private bool IsInGrid(Vector3 position)
    {
        int xMin = XPlayerLocation - length;
        int xMax = XPlayerLocation + length;
        int zMin = ZPlayerLocation - length;
        int zMax = ZPlayerLocation + length;

        return (position.x >= xMin && position.x <= xMax && position.z >= zMin && position.z <= zMax);
    }

    private int XPlayerMove => (int)(player.transform.position.x - startPos.x);
    private int ZPlayerMove => (int)(player.transform.position.z - startPos.z);

    private int XPlayerLocation => (int)Mathf.Floor(player.transform.position.x);
    private int ZPlayerLocation => (int)Mathf.Floor(player.transform.position.z);

    void Start()
    {
        ApplyConfig();
        itemPos = new Dictionary<Vector3, Tile>();
        mainCamera = Camera.main != null ? Camera.main.transform : null;

        // Reusable pool of sound-objects instead of Instantiate/Destroy churn.
        pool = new ObjectPool<GameObject>(
            createFunc: () =>
            {
                GameObject o = Instantiate(item, transform);
                o.SetActive(false);
                return o;
            },
            actionOnGet: o => o.SetActive(true),
            actionOnRelease: o => o.SetActive(false),
            actionOnDestroy: o => Destroy(o),
            collectionCheck: false,
            defaultCapacity: itemMax,
            maxSize: itemMax * 2);

        // Continuous diffuse floor(s) — layered wind/insects, each crossfade-looped 2D.
        if (biome != null)
        {
            if (biome.bedLayers != null && biome.bedLayers.Length > 0)
            {
                foreach (var layer in biome.bedLayers)
                {
                    if (layer == null || layer.clip == null) continue;
                    var go = new GameObject("AmbientBed_" + layer.clip.name);
                    go.AddComponent<AmbientBed>().Init(layer.clip, layer.volume, 3f);
                }
            }
            else if (biome.bedClip != null) // legacy single bed
            {
                var bedGo = new GameObject("AmbientBed");
                bedGo.AddComponent<AmbientBed>().Init(biome.bedClip, biome.bedVolume, biome.bedCrossfade);
            }

            // River / stream (2.4): world-anchored chain of looping water emitters.
            if (biome.riverClip != null)
            {
                var riverGo = new GameObject("RiverSystem");
                var river = riverGo.AddComponent<RiverSystem>();
                river.player = player != null ? player.transform : null;
                river.waterClip = biome.riverClip;
            }
        }

        // 2.7 spatial mix: forest reverb that follows the player.
        var reverbGo = new GameObject("ForestReverb");
        reverbGo.AddComponent<ForestReverb>().player = player != null ? player.transform : null;

        ManageItems(Time.realtimeSinceStartup);
    }

    private void Update()
    {
        // Run management on a steady tick (not gated on movement) so paced spawning and
        // despawns happen whether the player is walking or standing still.
        manageTimer += Time.deltaTime;
        if (manageTimer >= ManageInterval)
        {
            manageTimer = 0f;
            ManageItems(Time.realtimeSinceStartup);
        }
    }

    private void ApplyConfig()
    {
        if (config == null)
        {
            return;
        }
        itemMax = config.itemMax;
        itemChance = config.itemChance;
        length = config.spawnRadius;
        rndTimeMin = config.rndTimeMin;
        rndTimeMax = config.rndTimeMax;
        fadeInDuration = config.fadeInDuration;
        fadeOutDuration = config.fadeOutDuration;
        minDistance = config.minDistance;
        audibleRadius = config.audibleRadius;
        playerExclusionRadius = config.playerExclusionRadius;
        densityContrast = config.densityContrast;
        densityScale = config.densityScale;
        spawnIntervalMin = config.spawnIntervalMin;
        spawnIntervalMax = config.spawnIntervalMax;
        VoiceManager.Instance.maxVoices = config.maxVoices;
    }

    private bool HasBiome()
    {
        return biome != null && biome.profiles != null && biome.profiles.Length > 0;
    }

    // Weighted-random pick over eligible profiles (discrete layers, under their concurrency
    // cap). Returns null if no biome is assigned (→ fallback) or all profiles are capped.
    private SoundProfile SelectProfile()
    {
        if (!HasBiome())
        {
            return null;
        }
        float total = 0f;
        foreach (var p in biome.profiles)
        {
            if (p != null && p.enabled && p.layer != SoundLayer.Bed && CurrentCount(p) < p.maxConcurrent)
            {
                total += p.spawnWeight;
            }
        }
        if (total <= 0f)
        {
            return null;
        }
        float r = Random.Range(0f, total);
        float cum = 0f;
        foreach (var p in biome.profiles)
        {
            if (p != null && p.enabled && p.layer != SoundLayer.Bed && CurrentCount(p) < p.maxConcurrent)
            {
                cum += p.spawnWeight;
                if (r < cum)
                {
                    return p;
                }
            }
        }
        return null;
    }

    private int CurrentCount(SoundProfile p)
    {
        return activeCounts.TryGetValue(p, out int c) ? c : 0;
    }

    private void ChangeCount(SoundProfile p, int delta)
    {
        int c = CurrentCount(p) + delta;
        activeCounts[p] = c < 0 ? 0 : c;
    }

    // Called by ItemAudioManager once its fade-out completes.
    public void ReleaseToPool(GameObject o)
    {
        pool.Release(o);
    }

    // Pick one currently-live individual of the same species at random to cluster near.
    // Reservoir sampling → uniform choice in a single pass, no temp list allocation.
    private bool TryPickNeighborAnchor(SoundProfile profile, Dictionary<Vector3, Tile> map, out Vector3 anchor)
    {
        anchor = Vector3.zero;
        int seen = 0;
        foreach (var kvp in map)
        {
            if (kvp.Value.profile != profile)
            {
                continue;
            }
            seen++;
            if (Random.Range(0, seen) == 0)
            {
                anchor = kvp.Value.tileObject.transform.position;
            }
        }
        return seen > 0;
    }

    private void ManageItems(float cTime)
    {
        var newItemPos = new Dictionary<Vector3, Tile>();
        var itemsToRemove = new List<Vector3>();

        foreach (var kvp in itemPos)
        {
            Vector3 loc = kvp.Key;
            GameObject itemObject = kvp.Value.tileObject;
            Vector3 itemPosition = itemObject.transform.position;

            bool shouldDestroy = false;

            // Fly-overs deliberately start and end outside the grid, so cull them by lifetime
            // (their crossing time) only; everything else also despawns when it leaves the grid.
            bool outOfGrid = !kvp.Value.flying && !IsInGrid(itemPosition);
            if (outOfGrid || kvp.Value.GetActiveDuration() >= kvp.Value.rndTime)
            {
                shouldDestroy = true;
            }

            // Wildlife awareness (2.8): a wary animal hushes and flees when the player comes
            // within its wary radius (not fly-overs — they're already passing through).
            if (!shouldDestroy && !kvp.Value.flying && kvp.Value.profile != null
                && kvp.Value.profile.waryRadius > 0f)
            {
                float wx = itemPosition.x - player.transform.position.x;
                float wz = itemPosition.z - player.transform.position.z;
                if (wx * wx + wz * wz < kvp.Value.profile.waryRadius * kvp.Value.profile.waryRadius)
                {
                    shouldDestroy = true;
                }
            }

            if (shouldDestroy)
            {
                itemsToRemove.Add(loc);
                if (kvp.Value.profile != null)
                {
                    ChangeCount(kvp.Value.profile, -1);
                }
                if (kvp.Value.audioManager != null)
                {
                    kvp.Value.audioManager.FadeOutAndRelease();
                }
                else
                {
                    pool.Release(itemObject);
                }
            }
            else
            {
                kvp.Value.cTimestamp = cTime;
                newItemPos[loc] = kvp.Value;
            }
        }

        foreach (Vector3 loc in itemsToRemove)
        {
            itemPos.Remove(loc);
        }

        // Paced spawning: at most one new sound per spawn interval (up to itemMax), so the
        // forest has natural ebb and flow instead of a nonstop wall of sound.
        if (Time.time >= nextSpawnTime && newItemPos.Count < itemMax)
        {
            if (TrySpawnOne(cTime, newItemPos))
            {
                nextSpawnTime = Time.time + Random.Range(spawnIntervalMin, spawnIntervalMax);
            }
        }

        itemPos = newItemPos;
    }

    // Attempt a single spawn: pick a profile under its cap, find a valid spot (outside the
    // player bubble), configure it, and play. Returns true if it spawned. Placement is either
    // uniform+density-weighted, or — for call-and-response — clustered near a same-species
    // neighbor so new calls answer existing birds instead of firing everywhere.
    private bool TrySpawnOne(float cTime, Dictionary<Vector3, Tile> map)
    {
        // Profile is position-independent, so choose it up front: it decides both the
        // concurrency check and whether this spawn should cluster near a neighbor.
        SoundProfile profile = SelectProfile();
        if (HasBiome() && profile == null)
        {
            return false; // every profile at its cap right now
        }

        // Fly-over: an eligible mobile species can spawn as a bird passing overhead instead of a
        // perched caller. Own placement (chord over the player), so it bypasses the ground-spawn
        // loop below and its player-exclusion/density gates.
        if (enableFlyovers && profile != null && profile.canFlyover && Random.value < flyoverChance)
        {
            return SpawnFlyover(cTime, map, profile);
        }

        for (int attempt = 0; attempt < 10; attempt++)
        {
            // Call-and-response: with neighborBias chance, anchor this spawn near an existing
            // individual of the same species instead of spreading it uniformly. Read fresh each
            // spawn so the live-tuning overrides on the spawner take effect during Play.
            float bias = overrideNeighbor ? neighborBiasOverride
                                          : (profile != null ? profile.neighborBias : 0f);
            Vector3 anchor = Vector3.zero;
            bool clustered = profile != null
                && bias > 0f
                && Random.value < bias
                && TryPickNeighborAnchor(profile, map, out anchor);

            int wx, wz;
            if (clustered)
            {
                float radius = overrideNeighbor ? neighborRadiusOverride : profile.neighborRadius;
                float ang = Random.Range(0f, Mathf.PI * 2f);
                float rad = Random.Range(profile.minDistance, radius);
                wx = Mathf.RoundToInt(anchor.x + Mathf.Cos(ang) * rad);
                wz = Mathf.RoundToInt(anchor.z + Mathf.Sin(ang) * rad);
            }
            else
            {
                wx = Random.Range(-length, length) + XPlayerLocation;
                wz = Random.Range(-length, length) + ZPlayerLocation;
            }

            float groundY = (yNoise(wx, wz, detailScale) * noiseHeight) + 10f;
            Vector3 loc = new Vector3(wx, groundY, wz);

            // Clustered spawns can land past the grid edge; keep everything in the live window.
            if (!IsInGrid(loc))
            {
                continue;
            }

            float ex = loc.x - player.transform.position.x;
            float ez = loc.z - player.transform.position.z;
            if (ex * ex + ez * ez < playerExclusionRadius * playerExclusionRadius)
            {
                continue;
            }
            if (map.ContainsKey(loc))
            {
                continue;
            }

            // Perlin density-weighted acceptance: life clusters into pockets vs clearings.
            // Skipped for clustered spawns — the neighbor anchor already provides the clustering,
            // and re-gating on density would fight it.
            if (!clustered)
            {
                float density = Mathf.PerlinNoise((loc.x + DensityOffset) / densityScale,
                                                  (loc.z + DensityOffset) / densityScale);
                float densityMul = Mathf.Lerp(1f, density * 2f, densityContrast); // avg ~1
                if (Random.Range(0f, 2f) > densityMul)
                {
                    continue;
                }
            }

            if (profile != null)
            {
                loc.y = groundY + Random.Range(profile.minHeight, profile.maxHeight);
            }

            Quaternion rot = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
            GameObject itemInstance = pool.Get();
            itemInstance.transform.SetPositionAndRotation(loc, rot);

            var audioManager = ConfigureSpawn(itemInstance, profile, out float life);
            audioManager.Play();

            Tile o = new Tile(cTime, itemInstance, life);
            o.audioManager = audioManager;
            o.profile = profile;
            map[loc] = o;
            return true;
        }
        return false;
    }

    // Get/attach the audio manager on a pooled instance and load it from the profile (or the
    // fallback distances). Sets `life` and bumps the concurrency count. Shared by the perched
    // and fly-over spawn paths so their audio setup can't drift apart.
    private ItemAudioManager ConfigureSpawn(GameObject instance, SoundProfile profile, out float life)
    {
        var audioManager = instance.GetComponent<ItemAudioManager>();
        if (audioManager == null)
        {
            audioManager = instance.AddComponent<ItemAudioManager>();
        }
        audioManager.listener = mainCamera;
        audioManager.spawner = this;
        audioManager.fadeInDuration = fadeInDuration;
        audioManager.fadeOutDuration = fadeOutDuration;

        if (profile != null)
        {
            audioManager.audioClips = profile.clips;
            audioManager.SetDistances(profile.minDistance, profile.audibleRadius);
            // Guard against 0 (older profile assets predate these fields; Unity loads
            // missing serialized fields as 0, which would make calls end instantly).
            audioManager.callLengthMin = profile.callLengthMin > 0f ? profile.callLengthMin : 2f;
            audioManager.callLengthMax = profile.callLengthMax > 0f ? profile.callLengthMax : 5f;
            audioManager.gapMin = profile.gapMin > 0f ? profile.gapMin : 3f;
            audioManager.gapMax = profile.gapMax > 0f ? profile.gapMax : 9f;
            audioManager.pitchJitter = profile.pitchJitter;
            audioManager.gainJitter = profile.gainJitter;
            audioManager.fixedStart = profile.fixedStart;
            if (profile.fadeIn > 0f) audioManager.fadeInDuration = profile.fadeIn;
            if (profile.fadeOut > 0f) audioManager.fadeOutDuration = profile.fadeOut;
            life = Random.Range(profile.lifetimeMin, profile.lifetimeMax);
            ChangeCount(profile, 1);
        }
        else
        {
            audioManager.SetDistances(minDistance, audibleRadius);
            audioManager.pitchJitter = 0f;
            audioManager.gainJitter = 0f;
            life = Random.Range(rndTimeMin, rndTimeMax);
        }
        return audioManager;
    }

    // Spawn a mobile bird that crosses the sky over the player, calling as it goes. Placement is
    // a chord through the player's audible range at a random heading/height; motion + audio are
    // driven by ItemAudioManager.BeginFlight. Fly-overs ignore the grid cull and despawn purely
    // by lifetime (set to the crossing time), so they aren't culled the instant they start
    // outside the grid. Returns true on spawn.
    private bool SpawnFlyover(float cTime, Dictionary<Vector3, Tile> map, SoundProfile profile)
    {
        float heading = Random.Range(0f, Mathf.PI * 2f);
        Vector3 dir = new Vector3(Mathf.Cos(heading), 0f, Mathf.Sin(heading));
        Vector3 side = new Vector3(-dir.z, 0f, dir.x); // perpendicular, to offset the chord
        float reach = profile.audibleRadius;           // cross the full range this species carries
        float offset = Random.Range(-reach * 0.5f, reach * 0.5f);
        float height = Random.Range(flyoverHeightMin, flyoverHeightMax);

        Vector3 center = player.transform.position + side * offset;
        center.y = player.transform.position.y + height;
        Vector3 from = center - dir * reach;
        Vector3 to = center + dir * reach;

        GameObject itemInstance = pool.Get();
        itemInstance.transform.SetPositionAndRotation(from, Quaternion.LookRotation(dir));

        var audioManager = ConfigureSpawn(itemInstance, profile, out float _);
        // Live exactly long enough to cross (plus a margin), regardless of the profile lifetime.
        float speed = Mathf.Max(flyoverSpeed, 0.1f);
        float life = (to - from).magnitude / speed + 1f;
        audioManager.Play();
        audioManager.BeginFlight(from, to, speed);

        Tile o = new Tile(cTime, itemInstance, life);
        o.audioManager = audioManager;
        o.profile = profile;
        o.flying = true;
        map[from] = o;
        return true;
    }

    private class Tile
    {
        public float cTimestamp;
        public GameObject tileObject;
        public ItemAudioManager audioManager;
        public SoundProfile profile;
        public bool flying;      // fly-over: ignore grid cull, despawn by lifetime only
        public float activationTime;
        public float rndTime;

        public Tile(float cTimestamp, GameObject tileObject, float rndTime)
        {
            this.tileObject = tileObject;
            this.cTimestamp = cTimestamp;
            this.activationTime = Time.time;
            this.rndTime = rndTime;
        }

        public float GetActiveDuration()
        {
            return Time.time - activationTime;
        }
    }

    private float yNoise(int x, int z, float detailScale)
    {
        float xNoise = (x + transform.position.x) / detailScale;
        float zNoise = (z + transform.position.y) / detailScale;
        return Mathf.PerlinNoise(xNoise, zNoise);
    }
}
