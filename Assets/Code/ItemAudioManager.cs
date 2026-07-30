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

    public float fadeInDuration = 0.05f;  // fast: declick without dulling call attacks
    public float fadeOutDuration = 0.25f; // smooth: no abrupt cutoffs

    // Envelope only. Loudness + direction come from the AudioSource's own 3D rolloff and
    // spatialization (realistic: distance sets volume, not head facing). fade eases 0..1
    // on spawn, voice-cull, and despawn so nothing snaps on/off.
    private float fade = 0f;
    private float fadeTarget = 0f;
    private bool releasing = false;      // fade out then return to pool
    private bool pauseWhenSilent = false; // culled voice: pause once silent (frees the voice)
    private bool audible = true;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
        {
            Debug.LogError("AudioSource component is missing.");
        }
        else
        {
            // Full 3D: distance drives loudness, spatializer drives direction.
            audioSource.spatialBlend = 1f;
        }
    }

    // Called by the spawner each time this object is reused from the pool, after it has
    // been repositioned. Picks a fresh weighted clip and fades it in.
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
        pauseWhenSilent = false;
        audible = true;
        fade = 0f;
        fadeTarget = 1f;

        audioSource.volume = 0f;
        audioSource.clip = SelectRandomClip();
        audioSource.Play();
        VoiceManager.Instance.Register(this);
    }

    // Called by the VoiceManager: fade out + pause when we're not one of the nearest
    // voices, fade back in when we are.
    public void SetAudible(bool value)
    {
        if (audioSource == null || releasing || value == audible)
        {
            return;
        }
        audible = value;

        if (value)
        {
            if (!audioSource.isPlaying)
            {
                audioSource.UnPause();
            }
            pauseWhenSilent = false;
            fadeTarget = 1f;
        }
        else
        {
            pauseWhenSilent = true;
            fadeTarget = 0f;
        }
    }

    // Called by the spawner on despawn: fade out, then return to the pool.
    public void FadeOutAndRelease()
    {
        releasing = true;
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

    void Update()
    {
        if (audioSource == null)
        {
            return;
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
            if (pauseWhenSilent && audioSource.isPlaying)
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
