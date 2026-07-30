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

        ManageItems(Time.realtimeSinceStartup);
    }

    private void Update()
    {
        if (Mathf.Abs(XPlayerMove) >= 1 || Mathf.Abs(ZPlayerMove) >= 1)
        {
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
        VoiceManager.Instance.maxVoices = config.maxVoices;
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

        int currentItemCount = newItemPos.Count;
        int itemsToSpawn = itemMax - currentItemCount;

        for (int i = 0; i < itemsToSpawn; i++)
        {
            int x = Random.Range(-length, length);
            int z = Random.Range(-length, length);
            Vector3 loc = new Vector3(x + XPlayerLocation,
                (yNoise(x + XPlayerLocation, z + ZPlayerLocation, detailScale) * noiseHeight) + 10f,
                z + ZPlayerLocation);

            // Player exclusion bubble: animals keep their distance, so nothing spawns
            // right on top of the player (horizontal distance).
            float ex = loc.x - player.transform.position.x;
            float ez = loc.z - player.transform.position.z;
            if (ex * ex + ez * ez < playerExclusionRadius * playerExclusionRadius)
            {
                continue;
            }

            if (!newItemPos.ContainsKey(loc))
            {
                // Perlin density: life clusters into pockets instead of spreading evenly.
                // densityMul averages ~1 so overall spawn rate is preserved; contrast
                // controls how clumpy vs uniform it is.
                float density = Mathf.PerlinNoise((loc.x + DensityOffset) / densityScale,
                                                  (loc.z + DensityOffset) / densityScale);
                float densityMul = Mathf.Lerp(1f, density * 2f, densityContrast);
                if (Random.Range(0f, 100f) < itemChance * densityMul)
                {
                    Quaternion randomRotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);

                    GameObject itemInstance = pool.Get();
                    itemInstance.transform.SetPositionAndRotation(loc, randomRotation);

                    // Ensure ItemAudioManager is present, then (re)start its audio.
                    var audioManager = itemInstance.GetComponent<ItemAudioManager>();
                    if (audioManager == null)
                    {
                        audioManager = itemInstance.AddComponent<ItemAudioManager>();
                    }
                    audioManager.listener = mainCamera;
                    audioManager.spawner = this;
                    audioManager.fadeInDuration = fadeInDuration;
                    audioManager.fadeOutDuration = fadeOutDuration;
                    audioManager.SetDistances(minDistance, audibleRadius);
                    audioManager.Play();

                    Tile o = new Tile(cTime, itemInstance, Random.Range(rndTimeMin, rndTimeMax));
                    o.audioManager = audioManager;
                    newItemPos[loc] = o;
                }
            }
        }

        itemPos = newItemPos;
    }

    private class Tile
    {
        public float cTimestamp;
        public GameObject tileObject;
        public ItemAudioManager audioManager;
        public float activationTime;
        public int rndTime;

        public Tile(float cTimestamp, GameObject tileObject, int rndTime)
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
