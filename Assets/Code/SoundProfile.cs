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
    [Tooltip("Uncheck to disable this species entirely — it won't spawn.")]
    public bool enabled = true;
    public SoundLayer layer = SoundLayer.Event;

    [Tooltip("Which times of day this species may call in. None/unset = all periods (back-compat). " +
             "e.g. owls = Night, songbirds = Day (Dawn|Midday|Dusk).")]
    public DayPeriodMask activePeriods = DayPeriodMask.All;

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

    [Header("Call-and-response (spawn-near-neighbor)")]
    [Tooltip("Chance a new individual clusters near an existing one of the same species " +
             "instead of spawning anywhere. 0 = spread evenly (default), 1 = always cluster.")]
    [Range(0f, 1f)] public float neighborBias = 0f;
    [Tooltip("How far from an existing individual a clustered spawn can land (world units).")]
    public float neighborRadius = 25f;

    [Header("Variety (anti-repetition, per call)")]
    [Tooltip("Random pitch spread per call (±fraction). Keeps repeated calls from sounding identical.")]
    [Range(0f, 0.5f)] public float pitchJitter = 0.06f;
    [Tooltip("Random gain spread per call (±fraction). Subtle level variation between calls.")]
    [Range(0f, 0.5f)] public float gainJitter = 0.12f;

    [Header("Flyover (mobile species only)")]
    [Tooltip("If set, this species can spawn as a moving fly-over (bird passing overhead) when " +
             "the spawner's flyovers are enabled. Leave off for perched/understory birds.")]
    public bool canFlyover = false;

    [Header("Playback")]
    [Tooltip("Start each call from the beginning and play full, instead of a random slice (e.g. a dove's coo).")]
    public bool fixedStart = false;
    [Tooltip("Fade-in seconds (0 = use the global default).")]
    public float fadeIn = 0f;
    [Tooltip("Fade-out seconds (0 = use the global default).")]
    public float fadeOut = 0f;

    [Header("Wildlife awareness (2.8) — reacts to the player")]
    [Tooltip("Player distance (world units) at which this animal hushes and flees. 0 = bold, ignores you.")]
    public float waryRadius = 0f;
}
