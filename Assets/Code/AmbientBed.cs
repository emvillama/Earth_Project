using System.Collections;
using UnityEngine;

// Continuous ambient floor. Plays one clip on two AudioSources and crossfades between
// them across the loop point, so the bed never cuts out or clicks at the seam. 2D so it
// surrounds the player everywhere. Auto-created by ItemSpawner from the biome's bedClip.
public class AmbientBed : MonoBehaviour
{
    public AudioClip clip;
    public float volume = 0.45f;
    public float crossfade = 2f;

    private AudioSource a, b, current;
    private float switchTime;

    public void Init(AudioClip c, float vol, float xf)
    {
        clip = c;
        volume = vol;
        crossfade = Mathf.Max(0.1f, xf);
    }

    void Start()
    {
        if (clip == null)
        {
            return;
        }
        a = gameObject.AddComponent<AudioSource>();
        b = gameObject.AddComponent<AudioSource>();
        foreach (var s in new[] { a, b })
        {
            s.clip = clip;
            s.loop = false;
            s.playOnAwake = false;
            s.spatialBlend = 0f; // 2D: ambient floor, present everywhere
            s.volume = 0f;
        }
        current = a;
        current.volume = volume;
        current.Play();
        switchTime = Time.time + Mathf.Max(0.5f, clip.length - crossfade);
    }

    void Update()
    {
        if (clip == null || current == null)
        {
            return;
        }
        if (Time.time >= switchTime)
        {
            AudioSource next = (current == a) ? b : a;
            next.time = 0f;
            next.Play();
            StartCoroutine(Crossfade(current, next));
            current = next;
            switchTime = Time.time + Mathf.Max(0.5f, clip.length - crossfade);
        }
    }

    private IEnumerator Crossfade(AudioSource outS, AudioSource inS)
    {
        float t = 0f;
        while (t < crossfade)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / crossfade);
            inS.volume = volume * k;
            outS.volume = volume * (1f - k);
            yield return null;
        }
        outS.Stop();
        outS.volume = 0f;
    }
}
