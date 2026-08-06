using System.Collections.Generic;
using UnityEngine;

// River / stream (CANOPY 2.4) — a single, discoverable, wandering river that behaves naturally.
//
// * One at a time. While none exists, it has a tunable chance to seed at a random nearby spot;
//   you only hear it once you wander into earshot (emitters fade in gradually) — you come upon it.
// * The course is a smooth seeded meander (windy, consistent if you backtrack) that winds on its
//   own — it does NOT follow the player.
// * Width varies along its length; thin stretches are a quiet trickle, wide ones roar. Where it
//   thins there's a chance it simply ENDS (a source/mouth), so it isn't infinite and won't keep
//   extending into newly generated terrain.
// * Leave earshot -> dormant (audio off, node memory freed, seed kept so returning finds the SAME
//   river). Get ~forgetGrids grids away -> forgotten for good; a new one can appear elsewhere.
//
// Emitters are static 3D looping sources (volume + radius scaled by local width, started at random
// loop offsets so they don't flange). Auto-created by ItemSpawner when the biome has a riverClip.
public class RiverSystem : MonoBehaviour
{
    [Header("Discovery (one river at a time)")]
    public bool riverActive = true;
    [Tooltip("Chance, each ~2s while no river exists, that one seeds nearby. Higher = find rivers more often.")]
    [Range(0f, 1f)] public float discoveryChance = 0.12f;
    public float discoverMin = 65f;
    public float discoverMax = 115f;
    [Tooltip("One grid = the spawn radius (150).")]
    public float gridSize = 150f;
    [Tooltip("Leave earshot by this and the river goes dormant (audio off, memory freed) but is re-findable.")]
    public float leaveDistance = 70f;
    [Tooltip("Get this many grids from a dormant river and it's forgotten for good.")]
    public float forgetGrids = 5f;
    public float rediscoverCooldown = 12f;

    [Header("Course shape (windiness)")]
    public float segmentLength = 12f;
    [Range(0f, 45f)] public float maxTurnDeg = 20f;
    [Tooltip("Node cap each direction — hard limit on river length.")]
    public int maxLength = 240;

    [Header("Width: wide/loud vs trickle, and ending")]
    [Range(0f, 1f)] public float widthMin = 0.3f;
    [Range(0f, 1f)] public float widthMax = 1f;
    public float widthNoiseScale = 0.11f;
    [Tooltip("Below this width the river may taper out and end.")]
    public float endThreshold = 0.34f;
    [Range(0f, 1f)] public float endChance = 0.3f;

    [Header("Sound")]
    public float baseVolume = 0.7f;
    public float baseRadius = 34f;
    public float groundY = 9f;
    [Tooltip("Seconds each emitter fades in (so the river is heard gradually, not instantly).")]
    public float fadeIn = 2.5f;
    [Tooltip("Seconds an emitter fades out as you move past it — crossfades with the next so the " +
             "river never hard-cuts and restarts.")]
    public float fadeOut = 2.5f;
    [Tooltip("Seconds each emitter overlaps ITSELF at the loop point — two copies crossfaded on the " +
             "audio clock so the water never seams/cuts when the clip loops (same trick as the beds).")]
    public float loopCrossfade = 2.5f;

    public AudioClip waterClip;
    public Transform player;

    private struct RNode { public Vector3 pos; public float headingRad; public float width; }
    // Each emitter is two copies of the clip (a, b) crossfaded at the loop point on the dsp clock so
    // the water loops seamlessly. `vol` is the outer level (width × segment move-fade); each copy's
    // final volume is vol × its loop envelope.
    private class Em { public AudioSource a, b; public double startA, startB; public float vol; public float target; public bool removing; }

    private readonly Dictionary<int, RNode> nodes = new Dictionary<int, RNode>();
    private readonly Dictionary<int, Em> emitters = new Dictionary<int, Em>();
    private readonly Stack<Em> pool = new Stack<Em>();
    private readonly List<int> scratch = new List<int>();
    private double clipDur; // seconds, from waterClip

    private enum State { None, Active, Dormant }
    private State state = State.None;
    private float riverSeed, baseHeadingRad;
    private Vector3 origin, dormantAnchor;
    private int nearIndex, endLow, endHigh;
    private float tick, discoverTick, cooldown;

    private float TurnRad(int i)
    {
        return (Mathf.PerlinNoise(riverSeed + i * 0.35f, 13.7f) * 2f - 1f) * (maxTurnDeg * Mathf.Deg2Rad);
    }

    private float WidthAt(int i)
    {
        return Mathf.Lerp(widthMin, widthMax, Mathf.PerlinNoise(riverSeed * 0.5f + i * widthNoiseScale, 71.3f));
    }

    private bool EndsAt(int i)
    {
        return WidthAt(i) < endThreshold && Mathf.PerlinNoise(riverSeed * 0.7f + i * 0.9f, 41f) < endChance;
    }

    private void ComputeEnds()
    {
        endHigh = maxLength;
        for (int i = 1; i <= maxLength; i++) { if (EndsAt(i)) { endHigh = i; break; } }
        endLow = -maxLength;
        for (int i = -1; i >= -maxLength; i--) { if (EndsAt(i)) { endLow = i; break; } }
    }

    private RNode Node(int i)
    {
        if (nodes.TryGetValue(i, out var n)) return n;
        RNode r;
        if (i == 0)
        {
            r.headingRad = baseHeadingRad;
            r.pos = origin;
        }
        else if (i > 0)
        {
            RNode p = Node(i - 1);
            r.headingRad = p.headingRad + TurnRad(i);
            r.pos = p.pos + new Vector3(Mathf.Cos(r.headingRad), 0f, Mathf.Sin(r.headingRad)) * segmentLength;
        }
        else
        {
            RNode p = Node(i + 1);
            r.headingRad = p.headingRad - TurnRad(i + 1);
            r.pos = p.pos - new Vector3(Mathf.Cos(p.headingRad), 0f, Mathf.Sin(p.headingRad)) * segmentLength;
        }
        r.pos.y = groundY;
        r.width = WidthAt(i);
        nodes[i] = r;
        return r;
    }

    void Update()
    {
        // Per-frame: keep each emitter's two copies crossfading at the loop point (seamless loop),
        // ease its outer volume toward target (segment-to-segment move crossfade), and recycle the
        // emitters the player has left once they're silent — so the river never hard-cuts/restarts.
        if (state == State.Active && clipDur > 0.0)
        {
            double now = AudioSettings.dspTime;
            scratch.Clear();
            foreach (var kv in emitters)
            {
                Em e = kv.Value;
                if (now >= e.startA + clipDur) { e.startA = e.startB + clipDur - loopCrossfade; e.a.PlayScheduled(e.startA); }
                if (now >= e.startB + clipDur) { e.startB = e.startA + clipDur - loopCrossfade; e.b.PlayScheduled(e.startB); }
                float secs = e.target > e.vol ? fadeIn : fadeOut;
                e.vol = Mathf.MoveTowards(e.vol, e.target, Time.deltaTime * baseVolume / Mathf.Max(0.05f, secs));
                e.a.volume = e.vol * LoopEnv(now, e.startA);
                e.b.volume = e.vol * LoopEnv(now, e.startB);
                if (e.removing && e.vol <= 0.005f) scratch.Add(kv.Key);
            }
            foreach (int k in scratch) { Recycle(emitters[k]); emitters.Remove(k); }
        }

        if (player == null || waterClip == null || !riverActive)
        {
            if (state != State.None && !riverActive) ForceNone();
            return;
        }

        tick += Time.deltaTime;
        if (tick < 0.25f) return;
        tick = 0f;

        switch (state)
        {
            case State.None:
                if (cooldown > 0f) { cooldown -= 0.25f; break; }
                discoverTick += 0.25f;
                if (discoverTick >= 2f) { discoverTick = 0f; if (Random.value < discoveryChance) Discover(); }
                break;

            case State.Active:
                UpdateNearIndex();
                float nd = Vector3.Distance(player.position, Node(Mathf.Clamp(nearIndex, endLow, endHigh)).pos);
                if (nd > leaveDistance) GoDormant();
                else ManageEmitters();
                break;

            case State.Dormant:
                float dd = Vector3.Distance(player.position, dormantAnchor);
                if (dd > forgetGrids * gridSize) { state = State.None; cooldown = rediscoverCooldown; }
                else if (dd < leaveDistance) Reactivate();
                break;
        }
    }

    private void ManageEmitters()
    {
        if (clipDur <= 0.0 && waterClip != null) clipDur = (double)waterClip.samples / waterClip.frequency;

        int span = Mathf.CeilToInt((baseRadius + segmentLength) / segmentLength) + 1;
        int lo = Mathf.Max(nearIndex - span, endLow);
        int hi = Mathf.Min(nearIndex + span, endHigh);
        for (int k = lo; k <= hi; k++)
        {
            RNode nd = Node(k);
            float radius = baseRadius * nd.width;
            float vol = baseVolume * nd.width;
            if (emitters.TryGetValue(k, out var e))
            {
                e.a.maxDistance = radius;
                e.b.maxDistance = radius;
                e.target = vol;
                e.removing = false; // back in the window before it faded out → let it rise again
            }
            else
            {
                Em ne = GetEmitter();
                ne.a.transform.position = nd.pos;
                ne.b.transform.position = nd.pos;
                ne.a.maxDistance = radius;
                ne.b.maxDistance = radius;
                ne.target = vol;
                ScheduleEmitter(ne);
                emitters[k] = ne;
            }
        }
        // Emitters outside the window fade out (recycled by Update once silent) rather than hard-stop.
        foreach (var kv in emitters)
        {
            if (kv.Key < lo || kv.Key > hi) { kv.Value.removing = true; kv.Value.target = 0f; }
        }
    }

    private void UpdateNearIndex()
    {
        for (int s = 0; s < 6; s++)
        {
            int c = Mathf.Clamp(nearIndex, endLow, endHigh);
            float d0 = (Node(c).pos - player.position).sqrMagnitude;
            float du = (nearIndex + 1 <= endHigh) ? (Node(nearIndex + 1).pos - player.position).sqrMagnitude : float.MaxValue;
            float dn = (nearIndex - 1 >= endLow) ? (Node(nearIndex - 1).pos - player.position).sqrMagnitude : float.MaxValue;
            if (du < d0 && du <= dn) nearIndex++;
            else if (dn < d0) nearIndex--;
            else break;
        }
        nearIndex = Mathf.Clamp(nearIndex, endLow, endHigh);
    }

    private void Discover()
    {
        float ang = Random.value * Mathf.PI * 2f;
        float dist = Random.Range(discoverMin, discoverMax);
        origin = player.position + new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * dist;
        origin.y = groundY;
        riverSeed = Random.value * 1000f;
        baseHeadingRad = Random.value * Mathf.PI * 2f;
        nodes.Clear();
        ComputeEnds();
        nearIndex = 0;
        state = State.Active;
    }

    private void GoDormant()
    {
        dormantAnchor = Node(Mathf.Clamp(nearIndex, endLow, endHigh)).pos;
        foreach (var kv in emitters) Recycle(kv.Value);
        emitters.Clear();
        nodes.Clear(); // free memory; seed kept so we regenerate the same river on return
        state = State.Dormant;
    }

    private void Reactivate()
    {
        nodes.Clear();
        ComputeEnds();
        state = State.Active; // same seed -> same course
    }

    private void ForceNone()
    {
        foreach (var kv in emitters) Recycle(kv.Value);
        emitters.Clear();
        nodes.Clear();
        state = State.None;
    }

    private Em GetEmitter()
    {
        Em e = pool.Count > 0 ? pool.Pop() : NewEmitter();
        e.a.gameObject.SetActive(true);
        e.b.gameObject.SetActive(true);
        e.removing = false;
        e.vol = 0f;
        return e;
    }

    // Two independent 3D sources (each on its own GameObject so each gets its own Steam Audio
    // spatializer). They alternate-crossfade to loop the clip seamlessly.
    private Em NewEmitter()
    {
        var goA = new GameObject("RiverEmitterA");
        var goB = new GameObject("RiverEmitterB");
        goA.transform.SetParent(transform);
        goB.transform.SetParent(transform);
        var a = AudioFactory.Add3D(goA, loop: false, minDistance: 4f); a.clip = waterClip; a.volume = 0f;
        var b = AudioFactory.Add3D(goB, loop: false, minDistance: 4f); b.clip = waterClip; b.volume = 0f;
        return new Em { a = a, b = b };
    }

    // Start an emitter's two copies so one begins as the other's tail fades (seamless loop). A small
    // random stagger desyncs neighbouring emitters so identical loops don't phase against each other.
    private void ScheduleEmitter(Em e)
    {
        if (clipDur < loopCrossfade * 2.0) loopCrossfade = (float)(clipDur * 0.25);
        double t0 = AudioSettings.dspTime + 0.1 + Random.Range(0f, 0.3f);
        e.startA = t0;
        e.a.PlayScheduled(t0);
        e.startB = t0 + clipDur - loopCrossfade;
        e.b.PlayScheduled(e.startB);
    }

    // Equal-power loop envelope for one copy: fade in over loopCrossfade, hold, fade out over the
    // last loopCrossfade. sqrt keeps constant power through the overlap (no dip at the seam).
    private float LoopEnv(double now, double start)
    {
        double t = now - start;
        if (t < 0.0 || t > clipDur) return 0f;
        float k;
        if (t < loopCrossfade) k = (float)(t / loopCrossfade);
        else if (t > clipDur - loopCrossfade) k = (float)((clipDur - t) / loopCrossfade);
        else k = 1f;
        // Linear (not equal-power): the two copies play the clip's end vs start of the SAME steady
        // water texture — near-correlated — so linear holds the seam at constant level (no doubling
        // bump, no dip), which is what we want here. Beds keep sqrt since they're quiet + varied.
        return Mathf.Clamp01(k);
    }

    private void Recycle(Em e)
    {
        e.a.Stop();
        e.b.Stop();
        e.a.gameObject.SetActive(false);
        e.b.gameObject.SetActive(false);
        pool.Push(e);
    }
}
