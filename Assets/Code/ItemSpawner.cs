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

    [Header("Weather")]
    [Tooltip("Master on/off for dynamic weather (thunderstorms). Toggle live to test. " +
             "Lower the timing values on the WeatherSystem object to cycle storms fast.")]
    public bool enableWeather = true;
    private WeatherController weather;
    private PeriodController period;

    [Header("Flow")]
    [Tooltip("Show the main menu (time of day / weather / biome) before building the world.")]
    public bool showMenu = true;
    private bool worldStarted = false;

    [Header("Spawn density — per-period budget")]
    [Tooltip("Max NEW lead calls allowed to START within each rolling window — the main density " +
             "dial. Kept low so the forest is sparse; answers (call-and-response) are extra. " +
             "Overridden by SpawnConfig if set there.")]
    public int spawnsPerPeriod = 3;
    [Tooltip("Length of the rolling window the budget is measured over (seconds).")]
    public float spawnPeriodSeconds = 60f;
    // Timestamps of lead spawns inside the current rolling window (pruned each tick).
    private readonly Queue<float> recentSpawns = new Queue<float>();

    [Header("Call-and-response")]
    [Tooltip("Chance a fresh lead call is answered by nearby birds of the same species — a short " +
             "conversation, then quiet. Only species with neighborBias > 0 answer.")]
    [Range(0f, 1f)] public float callResponseChance = 0.85f;
    [Tooltip("Fewest / most answers in one conversation (the back-and-forth).")]
    public int answersMin = 1;
    public int answersMax = 2;
    [Tooltip("Seconds between the call and each answer — short, so it reads as a reply.")]
    public float answerDelayMin = 2.5f;
    public float answerDelayMax = 7f;
    // Pending-conversation state (answers fire outside the budget, bypassing the long lead gap).
    private int burstAnswersLeft = 0;
    private float nextAnswerTime = 0f;
    private SoundProfile burstProfile;
    private Vector3 burstAnchor;

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

        // Main menu first (3.5): it calls BeginWorld() once the player picks and hits Start.
        if (showMenu && !GameConfig.Configured)
        {
            var menuGo = new GameObject("MainMenu");
            menuGo.AddComponent<MainMenu>().spawner = this;
            return;
        }
        BeginWorld();
    }

    // Builds the living world (beds, river, reverb, weather, period, trees) and starts spawning.
    // Split out of Start so the main menu can defer it until the player chooses their settings.
    public void BeginWorld()
    {
        if (worldStarted) return;
        worldStarted = true;

        // Continuous diffuse floor(s) — layered wind/insects, each crossfade-looped 2D.
        AmbientBed windBed = null, cricketBed = null;
        if (biome != null)
        {
            if (biome.bedLayers != null && biome.bedLayers.Length > 0)
            {
                foreach (var layer in biome.bedLayers)
                {
                    if (layer == null || layer.clip == null) continue;
                    var go = new GameObject("AmbientBed_" + layer.clip.name);
                    var bed = go.AddComponent<AmbientBed>();
                    bed.Init(layer.clip, layer.volume, 3f);
                    // Grab the wind & insect beds so weather can swell one and silence the other.
                    string n = layer.clip.name.ToLower();
                    if (n.Contains("wind")) windBed = bed;
                    else if (n.Contains("insect") || n.Contains("cricket") || n.Contains("grasshopper")) cricketBed = bed;
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

        // Dynamic weather: thunderstorm state machine that swells the wind, adds rain + thunder,
        // hushes the crickets and birds. Toggle via enableWeather (this spawner's Inspector).
        var weatherGo = new GameObject("WeatherSystem");
        weather = weatherGo.AddComponent<WeatherController>();
        weather.spawner = this;
        weather.player = player != null ? player.transform : null;
        weather.windBed = windBed;
        weather.cricketBed = cricketBed;

        // Time of day: user-selectable period (dev-toggle for now) that gates which species call,
        // how busy the forest is, and the insect-bed level. Weather layers on top of it.
        var periodGo = new GameObject("PeriodSystem");
        period = periodGo.AddComponent<PeriodController>();
        period.cricketBed = cricketBed;

        // 3.3b-ii acoustic geometry: invisible trunk colliders so sound can be occluded by "trees"
        // (raycast in ItemAudioManager). No visual world, so this is the only thing to block against.
        var treesGo = new GameObject("AcousticTrees");
        treesGo.AddComponent<AcousticTrees>().player = player != null ? player.transform : null;

        // Apply the player's main-menu choices. (enableWeather is this spawner's own toggle, which
        // the WeatherController reads via spawner.enableWeather.)
        period.current = GameConfig.Period;
        enableWeather = GameConfig.WeatherEnabled;
        weather.formChance = GameConfig.WeatherChance;

        ManageItems(Time.realtimeSinceStartup);
    }

    private void Update()
    {
        if (!worldStarted) return; // wait on the main menu

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
        // Per-period budget lives in config so it can be tuned there and override the scene value.
        if (config.spawnsPerPeriod > 0) spawnsPerPeriod = config.spawnsPerPeriod;
        if (config.spawnPeriodSeconds > 0f) spawnPeriodSeconds = config.spawnPeriodSeconds;
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
            if (p != null && p.enabled && p.layer != SoundLayer.Bed && CurrentCount(p) < p.maxConcurrent && IsActiveInPeriod(p))
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
            if (p != null && p.enabled && p.layer != SoundLayer.Bed && CurrentCount(p) < p.maxConcurrent && IsActiveInPeriod(p))
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

    // A species may call only in its assigned periods. None/unset = active in every period so
    // profiles authored before the day/night system keep spawning as before.
    private bool IsActiveInPeriod(SoundProfile p)
    {
        if (period == null || p.activePeriods == DayPeriodMask.None) return true;
        return (p.activePeriods & period.CurrentMask) != 0;
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

        // The forest is governed by a small budget of NEW *lead* calls per rolling window
        // (spawnsPerPeriod / spawnPeriodSeconds) — a real place has a rate of events over time, not
        // N sounds alive at once. A lead is then answered 1-2 times nearby (a short conversation),
        // and things go quiet again. itemMax stays only as a hard safety cap.
        while (recentSpawns.Count > 0 && recentSpawns.Peek() < Time.time - spawnPeriodSeconds)
        {
            recentSpawns.Dequeue();
        }
        float periodMul = period != null ? period.SpawnMultiplier : 1f;
        int budget = Mathf.Max(1, Mathf.RoundToInt(spawnsPerPeriod * periodMul));

        if (burstAnswersLeft > 0 && Time.time >= nextAnswerTime && newItemPos.Count < itemMax)
        {
            // Answer: a nearby bird of the same species replies, outside the budget/lead-gap so it
            // reads as an immediate back-and-forth. Skips if that species is already at its cap.
            if (burstProfile != null && CurrentCount(burstProfile) < burstProfile.maxConcurrent)
            {
                TrySpawnAt(burstProfile, true, burstAnchor, cTime, newItemPos, out _);
            }
            burstAnswersLeft--;
            nextAnswerTime = Time.time + Random.Range(answerDelayMin, answerDelayMax);
        }
        else if (recentSpawns.Count < budget && Time.time >= nextSpawnTime && newItemPos.Count < itemMax)
        {
            // Lead: weather hush thins new calls as a storm builds (hush 0 = normal, 1 = silent).
            float hush = weather != null ? weather.birdHush : 0f;
            bool allowed = hush <= 0f || Random.value > hush;
            if (allowed && TrySpawnOne(cTime, newItemPos, out SoundProfile spawned, out Vector3 pos))
            {
                recentSpawns.Enqueue(Time.time);
                // Start a conversation: chatty species (neighborBias > 0) get answered 1-2 times.
                if (spawned != null && spawned.neighborBias > 0f && Random.value < callResponseChance)
                {
                    burstProfile = spawned;
                    burstAnchor = pos;
                    burstAnswersLeft = Random.Range(answersMin, answersMax + 1);
                    nextAnswerTime = Time.time + Random.Range(answerDelayMin, answerDelayMax);
                }
            }
            // Space leads out across the window (jittered) so conversations are far apart.
            float avgGap = spawnPeriodSeconds / budget;
            nextSpawnTime = Time.time + avgGap * Random.Range(0.55f, 1.45f);
        }

        itemPos = newItemPos;
    }

    // Attempt a single spawn: pick a profile under its cap, find a valid spot (outside the
    // player bubble), configure it, and play. Returns true if it spawned. Placement is either
    // uniform+density-weighted, or — for call-and-response — clustered near a same-species
    // neighbor so new calls answer existing birds instead of firing everywhere.
    private bool TrySpawnOne(float cTime, Dictionary<Vector3, Tile> map,
                             out SoundProfile spawnedProfile, out Vector3 spawnedPos)
    {
        spawnedProfile = null;
        spawnedPos = Vector3.zero;

        // Profile is position-independent, so choose it up front: it decides both the
        // concurrency check and whether this spawn should cluster near a neighbor.
        SoundProfile profile = SelectProfile();
        if (HasBiome() && profile == null)
        {
            return false; // every profile at its cap right now
        }

        // Fly-over: an eligible mobile species can spawn as a bird passing overhead instead of a
        // perched caller. Own placement (chord over the player), bypassing the ground-spawn path.
        if (enableFlyovers && profile != null && profile.canFlyover && Random.value < flyoverChance)
        {
            if (SpawnFlyover(cTime, map, profile))
            {
                spawnedProfile = profile;
                spawnedPos = player.transform.position;
                return true;
            }
            return false;
        }

        // Call-and-response: with neighborBias chance, anchor this lead near an existing individual
        // of the same species instead of spreading it uniformly. Read fresh so live tuning applies.
        float bias = overrideNeighbor ? neighborBiasOverride
                                      : (profile != null ? profile.neighborBias : 0f);
        Vector3 anchor = Vector3.zero;
        bool clustered = profile != null
            && bias > 0f
            && Random.value < bias
            && TryPickNeighborAnchor(profile, map, out anchor);

        if (TrySpawnAt(profile, clustered, anchor, cTime, map, out spawnedPos))
        {
            spawnedProfile = profile;
            return true;
        }
        return false;
    }

    // Place and start one individual of `profile`. Clustered → lands near `anchor` (a neighbor or a
    // call-and-response answer); otherwise placed uniformly with Perlin density weighting. Shared by
    // lead spawns and answers so their placement can't drift apart. Reports the spawn position.
    private bool TrySpawnAt(SoundProfile profile, bool clustered, Vector3 anchor, float cTime,
                            Dictionary<Vector3, Tile> map, out Vector3 spawnedPos)
    {
        spawnedPos = Vector3.zero;
        for (int attempt = 0; attempt < 10; attempt++)
        {
            int wx, wz;
            if (clustered && profile != null)
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
            // Skipped for clustered spawns — the anchor already clusters them.
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
            spawnedPos = loc;
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
