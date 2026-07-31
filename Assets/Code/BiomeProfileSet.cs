using UnityEngine;

// One diffuse floor layer (e.g. wind, or insects), looped 2D under everything.
[System.Serializable]
public class BedLayer
{
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 0.4f;
}

// The set of SoundProfiles that make up one biome (Forest, Wetland, ...). Assign to the
// ItemSpawner; adding a species = add a profile here, no code changes.
[CreateAssetMenu(fileName = "BiomeProfileSet", menuName = "Earth/Biome Profile Set")]
public class BiomeProfileSet : ScriptableObject
{
    public string biomeName = "Forest";
    public SoundProfile[] profiles;

    [Header("Ambient bed (continuous diffuse floor)")]
    [Tooltip("Layered floor sources (wind + insects), each looped 2D. Keep these bird-free — " +
             "discrete birds come from the positional spawn system so they track with head movement.")]
    public BedLayer[] bedLayers;

    [Header("Legacy single bed (used only if bedLayers is empty)")]
    public AudioClip bedClip;
    [Range(0f, 1f)] public float bedVolume = 0.45f;
    public float bedCrossfade = 2f;
}
