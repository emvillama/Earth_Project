using UnityEngine;

// The set of SoundProfiles that make up one biome (Forest, Wetland, ...). Assign to the
// ItemSpawner; adding a species = add a profile here, no code changes.
[CreateAssetMenu(fileName = "BiomeProfileSet", menuName = "Earth/Biome Profile Set")]
public class BiomeProfileSet : ScriptableObject
{
    public string biomeName = "Forest";
    public SoundProfile[] profiles;
}
