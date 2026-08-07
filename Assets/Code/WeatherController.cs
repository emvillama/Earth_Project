using UnityEngine;

// CANOPY — dynamic weather (thunderstorm state machine).
//
// Flow (per design):
//   Clear   — every ~clearCheckSeconds there's a `formChance` (10%) a storm starts forming.
//   Forming — light rain building for ~stageSeconds; then `formToStorm` (25%) it becomes a full
//             storm, otherwise it dissipates back to Clear.
//   Storm   — full storm for ~stageSeconds; then `stormContinue` (50%) it keeps going another
//             stage, otherwise it starts Clearing.
//   Clearing— easing off for ~stageSeconds; then `clearingRampUp` (10%) it flares back into a
//             Storm, otherwise it settles into Calm.
//   Calm    — the quiet after: ~30s near-silence, then birds ramp back in, then crickets ~60s
//             after the birds; once both have returned, back to Clear.
//
// A single smoothed `intensity` (0..1) ramps toward each phase's target so everything grows and
// fades gradually. It crossfades a gentle light-rain loop into a heavy downpour, swells the wind,
// fires thunder (distant rumbles early, sharp cracks at peak) more often and louder as it builds,
// and silences the birds. BIRD RULE: birds only sing in Clear, during the calm recovery, and very
// faintly under light rain — any real rain or thunder shuts them up entirely.
//
// All clips load from Resources/Weather/<group> folders (RainHeavy, RainLight, ThunderDistant,
// ThunderClose) and are picked at random when a group is called, so nothing repeats predictably.
// No Inspector wiring. Auto-created by ItemSpawner; on/off is ItemSpawner.enableWeather.
public class WeatherController : MonoBehaviour
{
    [Header("Wiring (set by ItemSpawner)")]
    public ItemSpawner spawner;      // authoritative on/off via spawner.enableWeather
    public AmbientBed windBed;
    public AmbientBed cricketBed;
    public Transform player;
    [Tooltip("Locked full storm that never fades (the menu's 'Stormy' pick) until the player returns " +
             "to the menu and chooses something else.")]
    public bool lockStorm = false;

    [Header("Timing (seconds) — lower these to test fast")]
    [Tooltip("While Clear, roll for a storm forming this often.")]
    public float clearCheckSeconds = 300f;
    [Tooltip("How long each active phase (forming/storm/clearing) lasts before the next roll.")]
    public float stageSeconds = 120f;

    [Header("Calm after the storm (recovery timeline)")]
    [Tooltip("Seconds of near-silence right after the storm before birds start returning.")]
    public float calmQuietSeconds = 30f;
    [Tooltip("Seconds birds take to ramp back to full once they start returning.")]
    public float birdRampSeconds = 30f;
    [Tooltip("Seconds AFTER the birds begin returning before crickets start coming back.")]
    public float cricketDelayAfterBirds = 60f;
    [Tooltip("Seconds crickets take to ramp back once they return.")]
    public float cricketRampSeconds = 20f;

    [Header("Transition chances")]
    [Range(0f, 1f)] public float formChance = 0.10f;      // Clear -> Forming, per check
    [Range(0f, 1f)] public float formToStorm = 0.25f;     // Forming -> Storm (else Clear)
    [Range(0f, 1f)] public float stormContinue = 0.50f;   // Storm -> Storm (else Clearing)
    [Range(0f, 1f)] public float clearingRampUp = 0.10f;  // Clearing -> Storm (else Calm)

    [Header("Feel")]
    [Tooltip("Seconds for intensity to travel the full 0..1 — bigger = slower, more gradual.")]
    public float rampSeconds = 30f;
    [Tooltip("Forming sits at light-rain level so a few birds still sing while it builds.")]
    [Range(0f, 1f)] public float formingIntensity = 0.22f;
    [Range(0f, 1f)] public float clearingIntensity = 0.40f;
    [Tooltip("Wind bed gets up to (1 + this) louder at full storm.")]
    public float windBoost = 2.5f;
    [Tooltip("How much of normal bird activity remains during the calm after (0 = none).")]
    [Range(0f, 1f)] public float calmBirdActivity = 0.4f;

    [Header("Rain")]
    [Tooltip("At/below this intensity it's 'light rain' — gentle loop, a few birds, no thunder.")]
    [Range(0f, 1f)] public float lightRainCeil = 0.28f;
    [Tooltip("Peak volume of the heavy downpour at full storm.")]
    [Range(0f, 1f)] public float rainMaxVol = 0.6f;
    [Tooltip("Peak volume of the gentle light-rain layer.")]
    [Range(0f, 1f)] public float lightRainMaxVol = 0.4f;
    [Tooltip("Bird hush under light rain (0 = full birds, 1 = silent). ~0.85 = only the occasional chirp.")]
    [Range(0f, 1f)] public float lightRainBirdHush = 0.85f;

    [Header("Thunder")]
    [Tooltip("Intensity below this: no thunder (kept above light-rain so drizzle stays thunderless).")]
    [Range(0f, 1f)] public float thunderStartsAt = 0.30f;
    public float thunderGapMax = 42f;                     // seconds between strikes when just building
    public float thunderGapMin = 8f;                      // seconds between strikes at full storm

    [Header("Live (read-only)")]
    [SerializeField] private string phaseName = "Clear";
    [Range(0f, 1f)] public float intensity = 0f;
    [Tooltip("0 = birds normal, 1 = silent. Read by ItemSpawner.")]
    [Range(0f, 1f)] public float birdHush = 0f;
    [Tooltip("Toggle in Play to jump straight to a full storm (test helper).")]
    public bool debugStartStorm = false;

    private enum Phase { Clear, Forming, Storm, Clearing, Calm }
    private Phase phase = Phase.Clear;
    private float phaseTimer = 0f;
    private float CricketReturnAt => calmQuietSeconds + cricketDelayAfterBirds; // from calm start

    private AudioSource rainHeavy, rainLight, thunder;
    private AudioClip[] rainHeavyClips, rainLightClips, thunderDistant, thunderClose;
    private float thunderTimer = 0f;

    void Start()
    {
        rainHeavyClips = Resources.LoadAll<AudioClip>("Weather/RainHeavy");
        rainLightClips = Resources.LoadAll<AudioClip>("Weather/RainLight");
        thunderDistant = Resources.LoadAll<AudioClip>("Weather/ThunderDistant");
        thunderClose   = Resources.LoadAll<AudioClip>("Weather/ThunderClose");

        rainHeavy = NewSource("RainHeavy", loop: true);
        rainLight = NewSource("RainLight", loop: true);
        thunder   = NewSource("Thunder", loop: false);
        rainHeavy.volume = 0f;
        rainLight.volume = 0f;
        PickRain();               // seed both loops with a random clip and start them (silent)

        // "Stormy" locks straight into a full storm from frame one (see the lock in Update).
        if (lockStorm) { phase = Phase.Storm; intensity = 1f; }
    }

    private AudioSource NewSource(string name, bool loop)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform);
        return AudioFactory.Add2D(go, loop); // weather is everywhere — a 2D layer
    }

    private bool Active => spawner == null || spawner.enableWeather;

    void Update()
    {
        float dt = Time.deltaTime;

        if (!Active)
        {
            phase = Phase.Clear; phaseTimer = 0f;
            intensity = Mathf.MoveTowards(intensity, 0f, dt / Mathf.Max(1f, rampSeconds));
            ApplyAudio(dt);
            return;
        }

        // Locked storm ("Stormy" pick): stay a full storm, never run the transition machine.
        if (lockStorm)
        {
            phase = Phase.Storm;
            intensity = Mathf.MoveTowards(intensity, 1f, dt / Mathf.Max(1f, rampSeconds));
            phaseName = "Storm (locked)";
            ApplyAudio(dt);
            return;
        }

        if (debugStartStorm) { debugStartStorm = false; PickRain(); Enter(Phase.Storm); }

        phaseTimer += dt;
        float target = 0f;

        switch (phase)
        {
            case Phase.Clear:
                target = 0f;
                if (phaseTimer >= clearCheckSeconds)
                {
                    phaseTimer = 0f;
                    if (Random.value < formChance) Enter(Phase.Forming);
                }
                break;

            case Phase.Forming:
                target = formingIntensity;
                if (phaseTimer >= stageSeconds)
                    Enter(Random.value < formToStorm ? Phase.Storm : Phase.Clear);
                break;

            case Phase.Storm:
                target = 1f;
                if (phaseTimer >= stageSeconds)
                    Enter(Random.value < stormContinue ? Phase.Storm : Phase.Clearing);
                break;

            case Phase.Clearing:
                target = clearingIntensity;
                if (phaseTimer >= stageSeconds)
                    Enter(Random.value < clearingRampUp ? Phase.Storm : Phase.Calm);
                break;

            case Phase.Calm:
                target = 0f;
                // Stay in calm until both birds and crickets have fully returned, then resume Clear.
                if (phaseTimer >= CricketReturnAt + cricketRampSeconds) Enter(Phase.Clear);
                break;
        }

        intensity = Mathf.MoveTowards(intensity, target, dt / Mathf.Max(1f, rampSeconds));
        phaseName = phase.ToString();
        ApplyAudio(dt);
    }

    private void Enter(Phase p)
    {
        // A brand-new weather event (Clear -> Forming) rolls fresh rain clips so no two storms
        // sound the same. Re-entering Storm/Clearing keeps the current rain going.
        if (p == Phase.Forming && phase == Phase.Clear) PickRain();
        phase = p;
        phaseTimer = 0f;
    }

    private void ApplyAudio(float dt)
    {
        // During the calm after the storm, birds return first (after a quiet window), then crickets
        // a while later — each ramping back gradually rather than snapping on.
        float calmHush = 0f, calmCricketSuppress = 0f;
        if (phase == Phase.Calm)
        {
            float t = phaseTimer;
            calmHush = (t < calmQuietSeconds)
                ? 1f - calmBirdActivity                                   // quiet window: only a few birds
                : Mathf.Lerp(1f - calmBirdActivity, 0f, (t - calmQuietSeconds) / Mathf.Max(0.1f, birdRampSeconds));
            calmCricketSuppress = (t < CricketReturnAt)
                ? 1f                                                      // crickets still gone
                : Mathf.Lerp(1f, 0f, (t - CricketReturnAt) / Mathf.Max(0.1f, cricketRampSeconds));
        }

        // Wind swells with intensity; crickets fade out as the storm builds and stay gone through the calm.
        if (windBed != null) windBed.volumeScale = 1f + intensity * windBoost;
        if (cricketBed != null)
        {
            float suppress = Mathf.Max(Mathf.Clamp01(intensity * 1.6f), calmCricketSuppress);
            cricketBed.volumeScale = Mathf.MoveTowards(cricketBed.volumeScale, 1f - suppress, dt * 0.7f);
        }

        // BIRD RULE: silent under any real rain/thunder; only a few under light rain; recovery in calm.
        if (phase == Phase.Calm)
        {
            birdHush = calmHush;
        }
        else if (intensity <= 0.02f)
        {
            birdHush = 0f;                          // clear skies — full birdsong
        }
        else if (intensity <= lightRainCeil)
        {
            birdHush = lightRainBirdHush;           // light rain — very occasional chirp
        }
        else
        {
            birdHush = 1f;                          // storm / real rain / thunder — none
        }

        // Rain: a gentle light layer leads, crossfading into the heavy downpour as it builds.
        float heavyTarget = Mathf.Clamp01((intensity - lightRainCeil) / Mathf.Max(0.01f, 1f - lightRainCeil)) * rainMaxVol;
        float lightTarget = Mathf.Clamp01(intensity / Mathf.Max(0.01f, lightRainCeil)) * lightRainMaxVol
                            * (1f - Mathf.Clamp01((intensity - lightRainCeil) / 0.4f));
        if (rainHeavy != null) rainHeavy.volume = Mathf.MoveTowards(rainHeavy.volume, heavyTarget, dt * 0.5f);
        if (rainLight != null) rainLight.volume = Mathf.MoveTowards(rainLight.volume, lightTarget, dt * 0.5f);

        // Thunder: more frequent and louder as intensity rises; distant rumbles early, cracks at peak.
        if (thunder != null)
        {
            if (intensity < thunderStartsAt)
            {
                thunderTimer = Mathf.Min(thunderTimer, 3f); // primed to fire soon once it kicks up
            }
            else
            {
                thunderTimer -= dt;
                if (thunderTimer <= 0f && !thunder.isPlaying)
                {
                    AudioClip c = PickThunder();
                    if (c != null)
                    {
                        thunder.clip = c;
                        thunder.volume = Mathf.Lerp(0.35f, 1f, intensity);
                        thunder.pitch = Random.Range(0.95f, 1.05f);
                        thunder.Play();
                    }
                    float gap = Mathf.Lerp(thunderGapMax, thunderGapMin, intensity);
                    thunderTimer = gap * Random.Range(0.7f, 1.3f);
                }
            }
        }
    }

    private void PickRain()
    {
        AssignLoop(rainHeavy, rainHeavyClips);
        AssignLoop(rainLight, rainLightClips);
    }

    private void AssignLoop(AudioSource src, AudioClip[] group)
    {
        if (src == null) return;
        AudioClip c = RandomClip(group);
        if (c == null) return;
        src.clip = c;
        src.Play();
        if (c.length > 1f) src.time = Random.Range(0f, c.length); // random offset so layers don't phase
    }

    private AudioClip PickThunder()
    {
        // Low intensity: mostly distant rumbles. High: mostly sharp close cracks. Crossover for variety.
        AudioClip[] grp = (intensity < 0.55f)
            ? (Random.value < 0.75f ? thunderDistant : thunderClose)
            : (Random.value < 0.65f ? thunderClose : thunderDistant);
        return RandomClip(grp) ?? RandomClip(thunderDistant) ?? RandomClip(thunderClose);
    }

    private AudioClip RandomClip(AudioClip[] a)
    {
        if (a == null || a.Length == 0) return null;
        return a[Random.Range(0, a.Length)];
    }
}
