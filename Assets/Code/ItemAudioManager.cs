using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WeightedAudioClip
{
    public AudioClip clip;
    public float weight;
}

public class ItemAudioManager : MonoBehaviour
{
    public WeightedAudioClip[] audioClips;
    private AudioSource audioSource;
    public Transform listener;
    public ItemSpawner spawner;

    public float fadeInDuration = 0.05f;
    public float fadeOutDuration = 0.25f;

    // Persistence: an individual stays put and calls intermittently (call -> gap -> call)
    // for its whole life instead of each call being a fresh spawn elsewhere (no teleporting).
    // Set per-spawn from the SoundProfile.
    public float callLengthMin = 2f;
    public float callLengthMax = 5f;
    public float gapMin = 3f;
    public float gapMax = 9f;
    public bool fixedStart = false; // start each call from 0 (full clip) vs a random slice

    // Variety (2.6): per-call pitch/gain jitter + no back-to-back clip repeats, so a bird
    // calling repeatedly from one spot (and clusters of the same species) don't read as a loop.
    // Set per-spawn from the SoundProfile.
    public float pitchJitter = 0f;
    public float gainJitter = 0f;
    private int lastClipIndex = -1;
    private float callGain = 1f; // per-call level multiplier, folded into the envelope

    // Envelope: final volume = fade * callGain. Direction/loudness come from the 3D rolloff.
    private float fade = 0f;
    private float fadeTarget = 0f;
    private bool releasing = false; // fade out then return to pool
    private bool audible = true;    // voice-manager audibility (nearest win)
    private bool calling = false;   // true during a call, false during a gap
    private float callEndTime = 0f;
    private float gapEndTime = 0f;

    // Flyover (2.2): when set, the source glides from flyFrom to flyTo across the sky while it
    // calls, instead of staying put. Enabled per-spawn via BeginFlight (mobile species only).
    private bool flying = false;
    private Vector3 flyTo = Vector3.zero;
    private float flySpeed = 0f;

    // Occlusion (3.3b-ii): a trunk between this source and the listener muffles it (low-pass) and
    // ducks it a little. The raycast is throttled; cutoff/gain ease so it opens and closes smoothly
    // as you move — no popping. Trunks are the invisible colliders placed by AcousticTrees.
    private AudioLowPassFilter lowpass;
    private float occTick = 0f;
    private float occGain = 1f, occGainTarget = 1f;
    private float occCut = OpenCutoff, occCutTarget = OpenCutoff;
    private const float OpenCutoff = 22000f;
    private static readonly RaycastHit[] occHits = new RaycastHit[8];

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            Debug.LogError("AudioSource component is missing.");
        }
        else
        {
            audioSource.spatialBlend = 1f;   // full 3D: distance drives loudness
            audioSource.spatialize = true;   // HRTF binaural via the Steam Audio spatializer (3.3)
            AudioFactory.EnableSteamAudio(audioSource); // 3.3b-i: air absorption + physics distance
            lowpass = GetComponent<AudioLowPassFilter>();
            if (lowpass == null) lowpass = gameObject.AddComponent<AudioLowPassFilter>();
            lowpass.cutoffFrequency = OpenCutoff; // 3.3b-ii: driven by tree occlusion
        }
    }

    // Called by the spawner at spawn — the start of this individual's presence.
    public void Play()
    {
        if (audioSource == null)
        {
            return;
        }
        if (audioClips.Length == 0)
        {
            Debug.LogWarning("No audio clips assigned to the ItemAudioManager script.");
            return;
        }
        releasing = false;
        audible = true;
        fade = 0f;
        flying = false;      // perched by default; BeginFlight (post-Play) opts a spawn into motion
        lastClipIndex = -1;  // fresh individual: no previous clip to avoid yet
        VoiceManager.Instance.Register(this);
        StartCall();
    }

    // Spawner: turn this individual into a fly-over gliding from -> to across the sky. Call
    // after Play(). Reset each Play() so a pooled object reused as a perched bird stays put.
    public void BeginFlight(Vector3 from, Vector3 to, float speed)
    {
        flying = true;
        flyTo = to;
        flySpeed = speed;
        transform.position = from;
    }

    // Begin one call: fresh clip at a random offset, so a bird sings a different snippet
    // each time from the same spot.
    private void StartCall()
    {
        calling = true;
        callEndTime = Time.time + Random.Range(callLengthMin, callLengthMax);
        // Per-call variety: slight pitch shift + level trim, and never the same clip twice running.
        audioSource.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
        callGain = 1f + Random.Range(-gainJitter, gainJitter);
        audioSource.volume = 0f;
        audioSource.clip = SelectRandomClip();
        audioSource.Play();
        if (!fixedStart && audioSource.clip != null && audioSource.clip.length > 2f)
        {
            audioSource.time = Random.Range(0f, audioSource.clip.length - 1f);
        }
        else
        {
            audioSource.time = 0f;
        }
    }

    // Voice manager: cull (fade out + pause) when not among the nearest voices.
    public void SetAudible(bool value)
    {
        if (audioSource == null || releasing)
        {
            return;
        }
        audible = value;
        if (value && calling && !audioSource.isPlaying)
        {
            audioSource.UnPause();
        }
    }

    // Spawner: end this individual's presence — fade out, then return to pool.
    public void FadeOutAndRelease()
    {
        releasing = true;
        calling = false;
        fadeTarget = 0f;
        VoiceManager.UnregisterSafe(this);
        if (audioSource == null || fade <= 0.001f)
        {
            DoRelease();
        }
    }

    private void DoRelease()
    {
        releasing = false;
        calling = false;
        VoiceManager.UnregisterSafe(this);
        if (spawner != null)
        {
            spawner.ReleaseToPool(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    public void StopAudio()
    {
        VoiceManager.UnregisterSafe(this);
        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }

    public void SetDistances(float min, float max)
    {
        if (audioSource != null)
        {
            audioSource.minDistance = min;
            audioSource.maxDistance = max;
        }
    }

    void Update()
    {
        if (audioSource == null)
        {
            return;
        }

        // Flyover: glide across the sky whether calling or in a gap, so the pass reads as one
        // bird moving overhead. Reaching the far point just stops motion; lifetime/grid despawn.
        if (flying)
        {
            transform.position = Vector3.MoveTowards(transform.position, flyTo, flySpeed * Time.deltaTime);
        }

        if (!releasing)
        {
            // Call/gap cycle: sing for a call length, go quiet for a gap, repeat.
            if (calling && Time.time >= callEndTime)
            {
                calling = false;
                gapEndTime = Time.time + Random.Range(gapMin, gapMax);
            }
            else if (!calling && Time.time >= gapEndTime)
            {
                StartCall();
            }
            fadeTarget = (calling && audible) ? 1f : 0f;
        }

        if (fade != fadeTarget)
        {
            float dur = (fadeTarget > fade) ? fadeInDuration : fadeOutDuration;
            float step = (dur <= 0f) ? 1f : (Time.deltaTime / dur);
            fade = Mathf.MoveTowards(fade, fadeTarget, step);
        }

        // Occlusion: muffle + duck when a trunk sits between this source and the listener.
        if (lowpass != null && listener != null)
        {
            occTick += Time.deltaTime;
            if (occTick >= 0.15f) { occTick = 0f; UpdateOcclusion(); }
            occCut = Mathf.Lerp(occCut, occCutTarget, Time.deltaTime * 6f);
            occGain = Mathf.Lerp(occGain, occGainTarget, Time.deltaTime * 6f);
            lowpass.cutoffFrequency = occCut;
        }

        if (audioSource.isPlaying)
        {
            audioSource.volume = fade * callGain * occGain;
        }

        if (fade <= 0.0001f)
        {
            if (releasing)
            {
                DoRelease();
                return;
            }
            // Silent (in a gap or culled): pause to free the hardware voice.
            if (audioSource.isPlaying)
            {
                audioSource.Pause();
            }
        }
    }

    // Raycast source -> listener against the acoustic-tree trunks; the more trunks in the way, the
    // lower the low-pass cutoff (muffled) and the more it ducks. No trunks = fully open.
    private void UpdateOcclusion()
    {
        int mask = AcousticTrees.TreeLayerMask;
        Vector3 a = transform.position;
        Vector3 dir = listener.position - a;
        float dist = dir.magnitude;
        if (mask == 0 || dist < 0.75f)
        {
            occCutTarget = OpenCutoff; occGainTarget = 1f;
            return;
        }
        int hits = Physics.RaycastNonAlloc(a, dir / dist, occHits, dist, mask, QueryTriggerInteraction.Collide);
        if (hits <= 0)
        {
            occCutTarget = OpenCutoff; occGainTarget = 1f;
        }
        else
        {
            float b = Mathf.Clamp(hits, 1, 4) / 4f; // more trunks between = more muffled
            occCutTarget = Mathf.Lerp(OpenCutoff, 550f, b);
            occGainTarget = Mathf.Lerp(1f, 0.5f, b);
        }
    }

    void OnDisable()
    {
        VoiceManager.UnregisterSafe(this);
    }

    // Weighted pick that avoids repeating the immediately-previous clip when the species has
    // more than one variation (2.6 anti-repetition). Falls back gracefully with 0/1 clips.
    private AudioClip SelectRandomClip()
    {
        if (audioClips.Length == 0)
        {
            return null;
        }
        if (audioClips.Length == 1)
        {
            lastClipIndex = 0;
            return audioClips[0].clip;
        }

        // Total weight excluding the last-played clip, so we never pick it twice in a row.
        float totalWeight = 0f;
        for (int i = 0; i < audioClips.Length; i++)
        {
            if (i == lastClipIndex)
            {
                continue;
            }
            totalWeight += audioClips[i].weight;
        }
        // Degenerate weights (all zero / only the excluded one had weight): just advance off last.
        if (totalWeight <= 0f)
        {
            int idx = (lastClipIndex + 1) % audioClips.Length;
            lastClipIndex = idx;
            return audioClips[idx].clip;
        }

        float randomValue = Random.Range(0f, totalWeight);
        float cumulativeWeight = 0f;
        for (int i = 0; i < audioClips.Length; i++)
        {
            if (i == lastClipIndex)
            {
                continue;
            }
            cumulativeWeight += audioClips[i].weight;
            if (randomValue < cumulativeWeight)
            {
                lastClipIndex = i;
                return audioClips[i].clip;
            }
        }

        // Fallback: first clip that isn't the last one.
        int fallback = lastClipIndex == 0 ? 1 : 0;
        lastClipIndex = fallback;
        return audioClips[fallback].clip;
    }
}
