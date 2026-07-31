using UnityEngine;

// Which soundscape layer a profile belongs to. BED = continuous ambient floor
// (wind/insects, handled by the bed system in 2.3); EVENT/RARE = discrete spawned sounds.
public enum SoundLayer { Bed, Event, Rare }

// One species / sound type as data. Author via Assets → Create → Earth → Sound Profile,
// then group per biome in a BiomeProfileSet. This is the "SoundObject" 2.1 is built around.
[CreateAssetMenu(fileName = "SoundProfile", menuName = "Earth/Sound Profile")]
public class SoundProfile : ScriptableObject
{
    public string displayName;
    public SoundLayer layer = SoundLayer.Event;

    [Header("Clips")]
    [Tooltip("Clip variations for this species; internal weights choose between them per play.")]
    public WeightedAudioClip[] clips;

    [Header("Rarity & limits")]
    [Tooltip("Relative chance this profile is chosen when spawning (higher = more common).")]
    public float spawnWeight = 10f;
    [Tooltip("Max live instances of this profile at once.")]
    public int maxConcurrent = 2;

    [Header("Placement")]
    [Tooltip("Height band above ground (birds high, ground animals low).")]
    public float minHeight = 0f;
    public float maxHeight = 3f;

    [Header("Distance (world units)")]
    public float minDistance = 3f;
    [Tooltip("Heard out to here. Loud species (crow) carry far; a squirrel rustle does not.")]
    public float audibleRadius = 80f;

    [Header("Presence (seconds an individual stays and calls)")]
    public float lifetimeMin = 15f;
    public float lifetimeMax = 30f;

    [Header("Persistence — intermittent calling")]
    [Tooltip("How long each call lasts before a gap.")]
    public float callLengthMin = 2f;
    public float callLengthMax = 5f;
    [Tooltip("Quiet gap between calls (same bird, same spot).")]
    public float gapMin = 3f;
    public float gapMax = 9f;
}
