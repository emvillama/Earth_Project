using UnityEngine;

// Continuous ambient floor. A single looping 2D source — Unity loops it sample-accurately
// with no gap or cut. 2D so it surrounds the player everywhere (diffuse floor; not a
// localizable point source, which is correct for wind/insect ambience and head-tracking).
// Auto-created by ItemSpawner from the biome's bedClip.
public class AmbientBed : MonoBehaviour
{
    public AudioClip clip;
    public float volume = 0.45f;
    public float crossfade = 2f; // kept for compatibility; a well-made loop clip needs none

    private AudioSource source;

    public void Init(AudioClip c, float vol, float xf)
    {
        clip = c;
        volume = vol;
        crossfade = Mathf.Max(0f, xf);
    }

    void Start()
    {
        if (clip == null)
        {
            return;
        }
        source = gameObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = true;         // seamless continuous playback — never cuts out
        source.playOnAwake = false;
        source.spatialBlend = 0f;   // 2D: diffuse floor, present everywhere
        source.spatialize = false;
        source.volume = volume;
        source.Play();
    }

    // Allow live volume tweaks from the biome/inspector.
    void Update()
    {
        if (source != null && !Mathf.Approximately(source.volume, volume))
        {
            source.volume = volume;
        }
    }
}
