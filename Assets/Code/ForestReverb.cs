using UnityEngine;

// CANOPY 2.7 spatial mix polish: a subtle forest reverb (early reflections off trunks) that
// follows the player so every nearby source shares a consistent sense of space. Auto-created
// by ItemSpawner — no scene setup. Tune the preset/range live on the "ForestReverb" object.
public class ForestReverb : MonoBehaviour
{
    public Transform player;
    public AudioReverbPreset preset = AudioReverbPreset.Forest;
    [Tooltip("Full reverb within this distance of the player.")]
    public float minDistance = 30f;
    [Tooltip("Reverb fades to none past this — keep >= how far you can hear sources.")]
    public float maxDistance = 250f;

    private AudioReverbZone zone;

    void Start()
    {
        zone = gameObject.AddComponent<AudioReverbZone>();
        zone.reverbPreset = preset;
        zone.minDistance = minDistance;
        zone.maxDistance = maxDistance;
    }

    void Update()
    {
        if (player != null) transform.position = player.position;
        if (zone != null)
        {
            if (zone.reverbPreset != preset) zone.reverbPreset = preset;
            zone.minDistance = minDistance;
            zone.maxDistance = maxDistance;
        }
    }
}
