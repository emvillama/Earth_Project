using UnityEngine;

// Four selectable times of day. NOT clock-driven — the player (eventually) picks one; for now it's
// a dev toggle (number keys 1-4 or the Inspector), and a future in-game UI will call SetPeriod().
public enum DayPeriod { Dawn, Midday, Dusk, Night }

// Bitmask of periods a species may call in (SoundProfile.activePeriods). None (0) is treated as
// "all periods" so pre-existing profiles keep working until they're given a mask.
[System.Flags]
public enum DayPeriodMask
{
    None = 0,
    Dawn = 1,
    Midday = 2,
    Dusk = 4,
    Night = 8,
    Day = Dawn | Midday | Dusk,
    All = Dawn | Midday | Dusk | Night,
}

// Gives the soundscape a time-of-day: which species may call (via SoundProfile.activePeriods), how
// busy it is (spawn-budget multiplier — dawn chorus vs sparse night), and the insect-bed level
// (quiet midday, loud at night). Auto-created by ItemSpawner. Toggle for testing with 1-4; hand a
// future UI SetPeriod()/Cycle(). Weather runs on top of whatever period is selected.
public class PeriodController : MonoBehaviour
{
    [Tooltip("Current time of day. Set here, via hotkeys (1 Dawn / 2 Midday / 3 Dusk / 4 Night), or a future UI.")]
    public DayPeriod current = DayPeriod.Midday;

    [Header("Dev toggle (not shipped to players yet)")]
    [Tooltip("Number keys 1-4 switch the period in Play mode, for testing.")]
    public bool hotkeysEnabled = true;

    [Header("Per-period feel — order: Dawn, Midday, Dusk, Night")]
    [Tooltip("Spawn-budget multiplier (dawn chorus busiest, night sparse).")]
    public float[] spawnMultiplier = { 1.3f, 1.0f, 0.85f, 0.4f };
    [Tooltip("Insect/cricket bed base volume (quiet by day, loud at night).")]
    public float[] insectVolume = { 0.08f, 0.04f, 0.20f, 0.32f };
    [Tooltip("Seconds to ease the insect bed when the period changes.")]
    public float blendSeconds = 4f;

    [Header("Live (read-only)")]
    [SerializeField] private string periodName = "Midday";

    public AmbientBed cricketBed; // set by ItemSpawner

    public DayPeriodMask CurrentMask => (DayPeriodMask)(1 << (int)current);
    public float SpawnMultiplier => Pick(spawnMultiplier, 1f);

    public void SetPeriod(DayPeriod p) { current = p; }         // for the future UI
    public void Cycle() { current = (DayPeriod)(((int)current + 1) % 4); }

    private float Pick(float[] a, float dflt)
    {
        int i = (int)current;
        return (a != null && i >= 0 && i < a.Length) ? a[i] : dflt;
    }

    void Update()
    {
        if (hotkeysEnabled)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) current = DayPeriod.Dawn;
            else if (Input.GetKeyDown(KeyCode.Alpha2)) current = DayPeriod.Midday;
            else if (Input.GetKeyDown(KeyCode.Alpha3)) current = DayPeriod.Dusk;
            else if (Input.GetKeyDown(KeyCode.Alpha4)) current = DayPeriod.Night;
        }
        periodName = current.ToString();

        // Ease the insect bed toward this period's base level. Weather still multiplies on top of
        // this via the bed's volumeScale, so a storm can silence the crickets at any time of day.
        if (cricketBed != null)
        {
            float target = Pick(insectVolume, cricketBed.volume);
            cricketBed.volume = Mathf.MoveTowards(cricketBed.volume, target,
                                                  Time.deltaTime / Mathf.Max(0.1f, blendSeconds));
        }
    }
}
