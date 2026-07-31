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

        // Continuous ambient floor for this biome (wind/insects), if one is set.
        if (biome != null && biome.bedClip != null)
        {
            var bedGo = new GameObject("AmbientBed");
            bedGo.AddComponent<AmbientBed>().Init(biome.bedClip, biome.bedVolume, biome.bedCrossfade);
        }

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
            if (p != null && p.layer != SoundLayer.Bed && CurrentCount(p) < p.maxConcurrent)
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
            if (p != null && p.layer != SoundLayer.Bed && CurrentCount(p) < p.maxConcurrent)
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

            if (!IsInGrid(itemPosition) || kvp.Value.GetActiveDuration() >= kvp.Value.rndTime)
            {
                shouldDestroy = true;
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

    // Attempt a single spawn: find a valid spot (outside the player bubble, density-weighted)
    // and a profile under its cap, configure it, and play. Returns true if it spawned.
    private bool TrySpawnOne(float cTime, Dictionary<Vector3, Tile> map)
    {
        for (int attempt = 0; attempt < 10; attempt++)
        {
            int x = Random.Range(-length, length);
            int z = Random.Range(-length, length);
            float groundY = (yNoise(x + XPlayerLocation, z + ZPlayerLocation, detailScale) * noiseHeight) + 10f;
            Vector3 loc = new Vector3(x + XPlayerLocation, groundY, z + ZPlayerLocation);

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
            float density = Mathf.PerlinNoise((loc.x + DensityOffset) / densityScale,
                                              (loc.z + DensityOffset) / densityScale);
            float densityMul = Mathf.Lerp(1f, density * 2f, densityContrast); // avg ~1
            if (Random.Range(0f, 2f) > densityMul)
            {
                continue;
            }

            SoundProfile profile = SelectProfile();
            if (HasBiome() && profile == null)
            {
                continue; // every profile at its cap right now
            }
            if (profile != null)
            {
                loc.y = groundY + Random.Range(profile.minHeight, profile.maxHeight);
            }

            Quaternion rot = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
            GameObject itemInstance = pool.Get();
            itemInstance.transform.SetPositionAndRotation(loc, rot);

            var audioManager = itemInstance.GetComponent<ItemAudioManager>();
            if (audioManager == null)
            {
                audioManager = itemInstance.AddComponent<ItemAudioManager>();
            }
            audioManager.listener = mainCamera;
            audioManager.spawner = this;
            audioManager.fadeInDuration = fadeInDuration;
            audioManager.fadeOutDuration = fadeOutDuration;

            float life;
            if (profile != null)
            {
                audioManager.audioClips = profile.clips;
                audioManager.SetDistances(profile.minDistance, profile.audibleRadius);
                audioManager.callLengthMin = profile.callLengthMin;
                audioManager.callLengthMax = profile.callLengthMax;
                audioManager.gapMin = profile.gapMin;
                audioManager.gapMax = profile.gapMax;
                life = Random.Range(profile.lifetimeMin, profile.lifetimeMax);
                ChangeCount(profile, 1);
            }
            else
            {
                audioManager.SetDistances(minDistance, audibleRadius);
                life = Random.Range(rndTimeMin, rndTimeMax);
            }
            audioManager.Play();

            Tile o = new Tile(cTime, itemInstance, life);
            o.audioManager = audioManager;
            o.profile = profile;
            map[loc] = o;
            return true;
        }
        return false;
    }

    private class Tile
    {
        public float cTimestamp;
        public GameObject tileObject;
        public ItemAudioManager audioManager;
        public SoundProfile profile;
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
