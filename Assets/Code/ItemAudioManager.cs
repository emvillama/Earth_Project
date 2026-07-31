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

    // Envelope: final volume = fade. Direction/loudness come from the AudioSource's 3D rolloff.
    private float fade = 0f;
    private float fadeTarget = 0f;
    private bool releasing = false; // fade out then return to pool
    private bool audible = true;    // voice-manager audibility (nearest win)
    private bool calling = false;   // true during a call, false during a gap
    private float callEndTime = 0f;
    private float gapEndTime = 0f;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            Debug.LogError("AudioSource component is missing.");
        }
        else
        {
            audioSource.spatialBlend = 1f; // full 3D: distance drives loudness
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
        VoiceManager.Instance.Register(this);
        StartCall();
    }

    // Begin one call: fresh clip at a random offset, so a bird sings a different snippet
    // each time from the same spot.
    private void StartCall()
    {
        calling = true;
        callEndTime = Time.time + Random.Range(callLengthMin, callLengthMax);
        audioSource.volume = 0f;
        audioSource.clip = SelectRandomClip();
        audioSource.Play();
        if (audioSource.clip != null && audioSource.clip.length > 2f)
        {
            audioSource.time = Random.Range(0f, audioSource.clip.length - 1f);
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

        if (audioSource.isPlaying)
        {
            audioSource.volume = fade;
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

    void OnDisable()
    {
        VoiceManager.UnregisterSafe(this);
    }

    private AudioClip SelectRandomClip()
    {
        float totalWeight = 0f;
        foreach (var weightedClip in audioClips)
        {
            totalWeight += weightedClip.weight;
        }

        float randomValue = Random.Range(0, totalWeight);
        float cumulativeWeight = 0f;

        foreach (var weightedClip in audioClips)
        {
            cumulativeWeight += weightedClip.weight;
            if (randomValue < cumulativeWeight)
            {
                return weightedClip.clip;
            }
        }

        return audioClips[0].clip;
    }
}
