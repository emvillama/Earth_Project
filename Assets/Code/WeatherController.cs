using UnityEngine;

// CANOPY — dynamic weather (thunderstorm state machine).
//
// Flow (per design):
//   Clear   — every ~clearCheckSeconds there's a `formChance` (10%) a storm starts forming.
//   Forming — building for ~stageSeconds; then `formToStorm` (25%) it becomes a full storm,
//             otherwise it dissipates back to Clear.
//   Storm   — full storm for ~stageSeconds; then `stormContinue` (50%) it keeps going another
//             stage, otherwise it starts Clearing.
//   Clearing— easing off for ~stageSeconds; then `clearingRampUp` (10%) it flares back into a
//             Storm, otherwise it settles into Calm.
//   Calm    — the quiet after: crickets gone, few birds, no rain, for ~calmSeconds, then Clear.
//
// A single smoothed `intensity` (0..1) ramps toward each phase's target so everything grows and
// fades gradually — wind swells first, then rain fills in, thunder gets more frequent and louder,
// and birds fall silent as it builds. Drives the wind/cricket AmbientBeds, a 2D rain loop, and
// thunder one-shots. Weather clips load from Resources/Weather (no Inspector wiring). Auto-created
// by ItemSpawner; the on/off toggle lives on ItemSpawner (enableWeather) in the World Spawner tab.
public class WeatherController : MonoBehaviour
{
    [Header("Wiring (set by ItemSpawner)")]
    public ItemSpawner spawner;      // authoritative on/off via spawner.enableWeather
    public AmbientBed windBed;
    public AmbientBed cricketBed;
    public Transform player;

    [Header("Timing (seconds) — lower these to test fast")]
    [Tooltip("While Clear, roll for a storm forming this often.")]
    public float clearCheckSeconds = 300f;
    [Tooltip("How long each active phase (forming/storm/clearing) lasts before the next roll.")]
    public float stageSeconds = 120f;
    [Tooltip("How long the quiet 'calm after the storm' lasts.")]
    public float calmSeconds = 120f;

    [Header("Transition chances")]
    [Range(0f, 1f)] public float formChance = 0.10f;      // Clear -> Forming, per check
    [Range(0f, 1f)] public float formToStorm = 0.25f;     // Forming -> Storm (else Clear)
    [Range(0f, 1f)] public float stormContinue = 0.50f;   // Storm -> Storm (else Clearing)
    [Range(0f, 1f)] public float clearingRampUp = 0.10f;  // Clearing -> Storm (else Calm)

    [Header("Feel")]
    [Tooltip("Seconds for intensity to travel the full 0..1 — bigger = slower, more gradual.")]
    public float rampSeconds = 30f;
    [Range(0f, 1f)] public float formingIntensity = 0.35f;
    [Range(0f, 1f)] public float clearingIntensity = 0.40f;
    [Tooltip("Wind bed gets up to (1 + this) louder at full storm.")]
    public float windBoost = 2.5f;
    [Tooltip("Peak rain loop volume at full storm.")]
    [Range(0f, 1f)] public float rainMaxVol = 0.6f;
    [Tooltip("How much of normal bird activity remains during the calm after (0 = none).")]
    [Range(0f, 1f)] public float calmBirdActivity = 0.4f;

    [Header("Thunder")]
    [Range(0f, 1f)] public float thunderStartsAt = 0.18f; // intensity below this: silent
    public float thunderGapMax = 42f;                     // seconds between claps when just building
    public float thunderGapMin = 8f;                      // seconds between claps at full storm

    [Header("Live (read-only)")]
    [SerializeField] private string phaseName = "Clear";
    [Range(0f, 1f)] public float intensity = 0f;
    [Tooltip("0 = birds normal, 1 = silent. Read by ItemSpawner.")]
    [Range(0f, 1f)] public float birdHush = 0f;
    [Tooltip("Toggle in Play to jump straight to a full storm (test helper).")]
    public bool debugStartStorm = false;

    private enum Phase { Clear, Forming, Storm, Clearing, Calm }
    private Phase phase = Phase.Clear;
    private bool postStorm = false;
    private float phaseTimer = 0f;

    private AudioSource rain, thunder;
    private AudioClip rainClip;
    private AudioClip[] thunderClips;      // 0 distant, 1 rumble, 2 clap
    private float thunderTimer = 0f;

    void Start()
    {
        rainClip = Resources.Load<AudioClip>("Weather/Rain_Forest");
        thunderClips = new[]
        {
            Resources.Load<AudioClip>("Weather/Thunder_Distant"),
            Resources.Load<AudioClip>("Weather/Thunder_Rumble"),
            Resources.Load<AudioClip>("Weather/Thunder_Clap"),
        };

        rain = NewSource("RainLoop", loop: true);
        rain.clip = rainClip;
        rain.volume = 0f;
        if (rainClip != null) rain.Play();

        thunder = NewSource("Thunder", loop: false);
    }

    private AudioSource NewSource(string name, bool loop)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform);
        var s = go.AddComponent<AudioSource>();
        s.spatialBlend = 0f;  // weather is everywhere — a 2D layer
        s.spatialize = false;
        s.playOnAwake = false;
        s.loop = loop;
        return s;
    }

    private bool Active => spawner == null || spawner.enableWeather;

    void Update()
    {
        float dt = Time.deltaTime;

        if (!Active)
        {
            phase = Phase.Clear; postStorm = false; phaseTimer = 0f;
            intensity = Mathf.MoveTowards(intensity, 0f, dt / Mathf.Max(1f, rampSeconds));
            ApplyAudio(dt);
            return;
        }

        if (debugStartStorm) { debugStartStorm = false; Enter(Phase.Storm); }

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
                if (phaseTimer >= calmSeconds) { postStorm = false; Enter(Phase.Clear); }
                break;
        }

        intensity = Mathf.MoveTowards(intensity, target, dt / Mathf.Max(1f, rampSeconds));
        phaseName = phase.ToString();
        ApplyAudio(dt);
    }

    private void Enter(Phase p)
    {
        phase = p;
        phaseTimer = 0f;
        if (p == Phase.Calm) postStorm = true;   // crickets/birds stay suppressed through the calm
    }

    private void ApplyAudio(float dt)
    {
        // Wind swells with intensity; crickets fade out as it builds and stay gone through the calm.
        if (windBed != null) windBed.volumeScale = 1f + intensity * windBoost;
        if (cricketBed != null)
        {
            float suppress = Mathf.Max(Mathf.Clamp01(intensity * 1.6f), postStorm ? 1f : 0f);
            cricketBed.volumeScale = Mathf.MoveTowards(cricketBed.volumeScale, 1f - suppress, dt * 0.7f);
        }

        // Birds fall silent as the storm grows; only a few return during the calm after.
        float hush = Mathf.Clamp01(intensity * 1.4f);
        if (postStorm) hush = Mathf.Max(hush, 1f - calmBirdActivity);
        birdHush = hush;

        // Rain lags the wind in a bit, then fills to full.
        if (rain != null)
        {
            float rainVol = Mathf.Clamp01((intensity - 0.2f) / 0.8f) * rainMaxVol;
            rain.volume = Mathf.MoveTowards(rain.volume, rainVol, dt * 0.5f);
        }

        // Thunder: more frequent and louder as intensity rises; distant rumbles early, cracks late.
        if (thunder != null && thunderClips != null)
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

    private AudioClip PickThunder()
    {
        if (thunderClips == null || thunderClips.Length == 0) return null;
        // Early/low intensity: distant + rumble. High intensity: rumble + clap.
        int idx;
        if (intensity < 0.55f) idx = Random.value < 0.6f ? 0 : 1;
        else idx = Random.value < 0.5f ? 1 : 2;
        AudioClip c = thunderClips[Mathf.Clamp(idx, 0, thunderClips.Length - 1)];
        if (c == null) // fall back to any loaded clip
            foreach (var t in thunderClips) if (t != null) return t;
        return c;
    }
}
