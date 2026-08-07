using UnityEngine;

// One diffuse floor layer (wind, insects, ...), looped seamlessly with an equal-power
// crossfade at the loop point so there's no hard cut on field recordings that don't loop
// cleanly. Two AudioSources play the same clip, offset by (length - crossfade); each fades
// in/out over `crossfade` so the overlap masks the seam. Scheduled on the audio clock
// (dspTime) for sample-accurate timing. 2D — a diffuse floor, present everywhere.
public class AmbientBed : MonoBehaviour
{
    public AudioClip clip;
    public float volume = 0.45f;
    public float crossfade = 3f;

    // External multiplier (0..n) other systems push in — e.g. WeatherController swells the wind
    // and silences the crickets during a storm. 1 = untouched. Smoothed by whoever drives it.
    public float volumeScale = 1f;

    private AudioSource s0, s1;
    private double dur;
    private double start0, start1; // dsp start time of each source's current pass

    public void Init(AudioClip c, float vol, float xf)
    {
        clip = c;
        volume = vol;
        crossfade = Mathf.Max(0.25f, xf);
    }

    // Swap to a new clip and restart the seamless loop. Safe before Start (just stores the clip) or
    // during play — callers swap while the bed is silent (e.g. rain between storms) so it's inaudible.
    public void SetClip(AudioClip c)
    {
        if (c == null || c == clip) return;
        clip = c;
        if (s0 == null) return; // not started yet — Start() will pick it up
        dur = (double)clip.samples / clip.frequency;
        if (dur <= crossfade * 2.0) crossfade = (float)(dur * 0.25);
        s0.Stop(); s1.Stop();
        s0.clip = clip; s1.clip = clip;
        double t0 = AudioSettings.dspTime + 0.05;
        start0 = t0; s0.PlayScheduled(start0);
        start1 = t0 + dur - crossfade; s1.PlayScheduled(start1);
    }

    void Start()
    {
        if (clip == null)
        {
            return;
        }
        s0 = AudioFactory.Add2D(gameObject, loop: false); // 2D diffuse floor
        s1 = AudioFactory.Add2D(gameObject, loop: false);
        foreach (var s in new[] { s0, s1 })
        {
            s.clip = clip;
            s.volume = 0f;
        }
        dur = (double)clip.samples / clip.frequency;
        if (dur <= crossfade * 2.0)
        {
            crossfade = (float)(dur * 0.25); // very short clip: shrink the crossfade
        }

        double t0 = AudioSettings.dspTime + 0.15;
        start0 = t0;
        s0.PlayScheduled(start0);
        start1 = t0 + dur - crossfade; // second copy overlaps the first's tail
        s1.PlayScheduled(start1);
    }

    void Update()
    {
        if (clip == null)
        {
            return;
        }
        double now = AudioSettings.dspTime;

        // Reschedule each source's next pass to begin as the *other* one is ending,
        // keeping a continuous alternating crossfade chain.
        if (now >= start0 + dur)
        {
            start0 = start1 + dur - crossfade;
            s0.PlayScheduled(start0);
        }
        if (now >= start1 + dur)
        {
            start1 = start0 + dur - crossfade;
            s1.PlayScheduled(start1);
        }

        s0.volume = Envelope(now, start0);
        s1.volume = Envelope(now, start1);
    }

    // Equal-power fade in over the first `crossfade`, full in the middle, fade out over the
    // last `crossfade`. sqrt keeps constant power through the overlap (no -3dB dip).
    private float Envelope(double now, double start)
    {
        double t = now - start;
        if (t < 0.0 || t > dur)
        {
            return 0f;
        }
        float k;
        if (t < crossfade)
        {
            k = (float)(t / crossfade);
        }
        else if (t > dur - crossfade)
        {
            k = (float)((dur - t) / crossfade);
        }
        else
        {
            k = 1f;
        }
        return volume * Mathf.Max(0f, volumeScale) * Mathf.Sqrt(Mathf.Clamp01(k));
    }
}
