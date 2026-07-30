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
}
