using UnityEngine;

// Player footstep / foliage SFX — CANOPY 2.5. Plays a leaf-crunch on each footfall, with the
// occasional stick/branch snap, so the player's own movement becomes part of the 3D space.
//
// NOT wired into the scene yet, on purpose. Footsteps only feel real when the crunch lands on the
// ACTUAL footfall — a crunch on an off-step reads as fake. So the accurate path is event-driven:
// have the player controller or an animation footstep event call Footstep() once per real step.
// A distance-based auto-stride is included as a prototype-only convenience (autoStride), but it
// only ESTIMATES steps and can land off-beat — leave it off for anything meant to feel real.
//
// To use later: add this to the player (or a child), assign clip sets, and either call Footstep()
// from your gait/animation, or tick autoStride for rough testing.
[RequireComponent(typeof(AudioSource))]
public class PlayerFootsteps : MonoBehaviour
{
    [Header("Clips")]
    [Tooltip("Leaf-crunch variations; one plays per footstep (weighted, no back-to-back repeat).")]
    public WeightedAudioClip[] leafCrunch;
    [Tooltip("Occasional stick/branch snaps, layered onto some steps.")]
    public WeightedAudioClip[] branchSnap;
    [Range(0f, 1f)]
    [Tooltip("Chance a given footstep also triggers a branch snap.")]
    public float branchSnapChance = 0.08f;

    [Header("Naturalism (per step)")]
    [Tooltip("Random pitch spread per step (±fraction), so repeated steps don't sound identical.")]
    [Range(0f, 0.5f)] public float pitchJitter = 0.08f;
    [Tooltip("Random gain spread per step (±fraction).")]
    [Range(0f, 0.5f)] public float gainJitter = 0.15f;
    [Tooltip("Base level for footstep SFX.")]
    [Range(0f, 1f)] public float volume = 0.8f;

    [Header("Auto-stride (PROTOTYPE ONLY — estimates steps, can hit off-beats)")]
    [Tooltip("If on, plays a step every strideLength of horizontal movement instead of waiting for " +
             "Footstep() calls. Convenience for testing only; prefer event-driven for realism.")]
    public bool autoStride = false;
    [Tooltip("Horizontal distance between auto-stride steps (world units).")]
    public float strideLength = 1.8f;
    [Tooltip("Ignore auto-stride below this speed (units/sec) so idle jitter doesn't crunch.")]
    public float minMoveSpeed = 0.5f;

    private AudioSource source;
    private int lastLeafIndex = -1;
    private int lastBranchIndex = -1;
    private Vector3 lastPos;
    private float distanceAccum;

    void Awake()
    {
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f; // the player's own steps: heard as 2D, right at the listener
        lastPos = transform.position;
    }

    void Update()
    {
        if (!autoStride)
        {
            return;
        }

        Vector3 pos = transform.position;
        Vector3 delta = pos - lastPos;
        delta.y = 0f; // horizontal travel only
        lastPos = pos;

        float dt = Time.deltaTime;
        if (dt <= 0f)
        {
            return;
        }

        float speed = delta.magnitude / dt;
        if (speed < minMoveSpeed)
        {
            distanceAccum = 0f; // reset so the first step after standing still isn't instant
            return;
        }

        distanceAccum += delta.magnitude;
        if (distanceAccum >= strideLength)
        {
            distanceAccum -= strideLength;
            Footstep();
        }
    }

    // Call this on each real footfall (animation event / controller) for accurate, in-sync steps.
    public void Footstep()
    {
        PlayFrom(leafCrunch, ref lastLeafIndex, 1f);
        if (branchSnap != null && branchSnap.Length > 0 && Random.value < branchSnapChance)
        {
            PlayFrom(branchSnap, ref lastBranchIndex, 1f);
        }
    }

    // Weighted pick that avoids the immediately-previous clip, with per-step pitch/gain jitter.
    private void PlayFrom(WeightedAudioClip[] clips, ref int lastIndex, float levelScale)
    {
        if (source == null || clips == null || clips.Length == 0)
        {
            return;
        }
        int index = PickIndex(clips, lastIndex);
        if (index < 0 || clips[index].clip == null)
        {
            return;
        }
        lastIndex = index;
        source.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
        float gain = 1f + Random.Range(-gainJitter, gainJitter);
        source.PlayOneShot(clips[index].clip, volume * levelScale * gain);
    }

    // Weighted index, excluding the last one played so no clip repeats back-to-back.
    private int PickIndex(WeightedAudioClip[] clips, int lastIndex)
    {
        if (clips.Length == 1)
        {
            return 0;
        }
        float total = 0f;
        for (int i = 0; i < clips.Length; i++)
        {
            if (i == lastIndex)
            {
                continue;
            }
            total += clips[i].weight;
        }
        if (total <= 0f)
        {
            return (lastIndex + 1) % clips.Length; // degenerate weights: just advance off last
        }
        float r = Random.Range(0f, total);
        float cum = 0f;
        for (int i = 0; i < clips.Length; i++)
        {
            if (i == lastIndex)
            {
                continue;
            }
            cum += clips[i].weight;
            if (r < cum)
            {
                return i;
            }
        }
        return lastIndex == 0 ? 1 : 0;
    }
}
