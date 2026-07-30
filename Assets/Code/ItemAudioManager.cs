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

    public float maxVolumeWhenFacing = 1.0f;
    public float minVolumeWhenNotFacing = 0.2f;
    public float maxAngle = 60.0f;

    private bool audible = true;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
        {
            Debug.LogError("AudioSource component is missing.");
        }
    }

    void Start()
    {
        if (listener == null)
        {
            listener = Camera.main.transform;
        }
    }

    // Called by the spawner each time this object is reused from the pool,
    // after it has been repositioned. Picks a fresh weighted clip and plays it.
    public void Play()
    {
        if (audioSource == null)
        {
            return;
        }

        if (audioClips.Length > 0)
        {
            audioSource.clip = SelectRandomClip();
            audioSource.Play();
            audible = true;
            VoiceManager.Instance.Register(this);
        }
        else
        {
            Debug.LogWarning("No audio clips assigned to the ItemAudioManager script.");
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

    // Called by the VoiceManager: pause when we're not one of the nearest voices,
    // resume (mid-sound) when we are.
    public void SetAudible(bool value)
    {
        if (audioSource == null || value == audible)
        {
            return;
        }
        audible = value;

        if (value)
        {
            audioSource.UnPause();
        }
        else
        {
            audioSource.Pause();
        }
    }

    void OnDisable()
    {
        VoiceManager.UnregisterSafe(this);
    }

    void Update()
    {
        if (audioSource == null || listener == null)
        {
            return;
        }

        AdjustVolumeBasedOnListenerPosition();
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

    private void AdjustVolumeBasedOnListenerPosition()
    {
        Vector3 directionToListener = (listener.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, directionToListener);
        float t = Mathf.Clamp01(angle / maxAngle);
        audioSource.volume = Mathf.Lerp(maxVolumeWhenFacing, minVolumeWhenNotFacing, t);
    }
}
