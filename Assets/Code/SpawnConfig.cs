using UnityEngine;

// One place to tune the spawner without touching code. Create via
// Assets → Create → Earth → Spawn Config, then drag it onto the ItemSpawner
// (WorldGenerator) component's Config slot. If no config is assigned, ItemSpawner
// keeps its built-in defaults, so the game still runs.
[CreateAssetMenu(fileName = "SpawnConfig", menuName = "Earth/Spawn Config")]
public class SpawnConfig : ScriptableObject
{
    [Header("Spawning")]
    [Tooltip("Max sound-objects alive at once (the pooled field around the player).")]
    public int itemMax = 40;
    [Tooltip("Per-attempt spawn chance, 0-100.")]
    public int itemChance = 1;
    [Tooltip("Grid half-size (world units) the field is spread across around the player.")]
    public int spawnRadius = 150;

    [Header("Lifetime (seconds)")]
    public int rndTimeMin = 5;
    public int rndTimeMax = 10;

    [Header("Voices")]
    [Tooltip("Max simultaneously audible voices (nearest win). Phones choke past ~32.")]
    public int maxVoices = 20;

    [Header("Fade (seconds)")]
    public float fadeInDuration = 0.05f;
    public float fadeOutDuration = 0.25f;

    [Header("Distance (world units)")]
    [Tooltip("Full volume within this distance.")]
    public float minDistance = 3f;
    [Tooltip("Heard out to here (AudioSource max distance). Keep < spawnRadius so sounds fade in as you approach.")]
    public float audibleRadius = 80f;
    [Tooltip("No discrete sounds spawn within this of the player (animals keep their distance).")]
    public float playerExclusionRadius = 10f;

    [Header("Spawn pacing")]
    [Tooltip("Min seconds between new spawns. Higher = calmer, more lulls.")]
    public float spawnIntervalMin = 1.0f;
    [Tooltip("Max seconds between new spawns.")]
    public float spawnIntervalMax = 3.0f;

    [Header("Per-period budget (main density dial)")]
    [Tooltip("Max NEW lead calls per rolling window. Low = sparse forest; answers are extra. 0 = keep spawner default.")]
    public int spawnsPerPeriod = 3;
    [Tooltip("Rolling window the lead budget is measured over (seconds). 0 = keep spawner default.")]
    public float spawnPeriodSeconds = 60f;

    [Header("Density (Perlin clustering)")]
    [Range(0f, 1f)]
    [Tooltip("0 = uniform spread, 1 = strong clumps and clearings.")]
    public float densityContrast = 0.5f;
    [Tooltip("Patch size in world units; bigger = broader pockets of life.")]
    public float densityScale = 40f;
}
