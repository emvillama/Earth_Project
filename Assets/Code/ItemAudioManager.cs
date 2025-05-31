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

    private float maxAngleRad;

    void Awake()
    {
        maxAngleRad = maxAngle * Mathf.Deg2Rad;
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
        {
            Debug.LogError("AudioSource component is missing.");
            return;
        }

        if (audioClips.Length > 0)
        {
            AudioClip randomClip = SelectRandomClip();
            audioSource.clip = randomClip;
            audioSource.Play();
        }
        else
        {
            Debug.LogWarning("No audio clips assigned to the ItemAudioManager script.");
        }
    }

    void Start()
    {
        if (listener == null)
        {
            listener = Camera.main.transform;
            Debug.Log("Listener assigned to main camera.");
        }
    }

    void Update()
    {
        if (audioSource == null || listener == null)
        {
            Debug.LogError("AudioSource or listener is null.");
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

        Debug.Log($"Angle: {angle}, Volume: {audioSource.volume}");
    }
}