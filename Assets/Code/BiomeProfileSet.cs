using UnityEngine;

// The set of SoundProfiles that make up one biome (Forest, Wetland, ...). Assign to the
// ItemSpawner; adding a species = add a profile here, no code changes.
[CreateAssetMenu(fileName = "BiomeProfileSet", menuName = "Earth/Biome Profile Set")]
public class BiomeProfileSet : ScriptableObject
{
    public string biomeName = "Forest";
    public SoundProfile[] profiles;

    [Header("Ambient bed (continuous floor)")]
    [Tooltip("Continuous looping ambience played under everything (wind/insects). Leave empty for none.")]
    public AudioClip bedClip;
    [Range(0f, 1f)] public float bedVolume = 0.45f;
    [Tooltip("Crossfade seconds at the loop point (seamless, no cutout).")]
    public float bedCrossfade = 2f;
}
