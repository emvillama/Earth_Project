using System;
using System.Collections.Generic;
using UnityEngine;

// Caps how many spawned sound-objects play at once. Every evaluateInterval it ranks
// all active ItemAudioManagers by distance to the listener and lets the nearest
// `maxVoices` play, pausing the rest (so they resume mid-sound when you approach).
// Auto-creates itself the first time an ItemAudioManager registers — nothing to wire
// in the scene.
public class VoiceManager : MonoBehaviour
{
    // The one number that matters. ~20 stays under the mobile voice ceiling while
    // keeping the forest full. Tune here if we want it denser/sparser.
    public int maxVoices = 20;
    public float evaluateInterval = 0.25f;

    private static VoiceManager _instance;
    public static VoiceManager Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("VoiceManager");
                _instance = go.AddComponent<VoiceManager>();
            }
            return _instance;
        }
    }

    private readonly List<ItemAudioManager> active = new List<ItemAudioManager>(128);
    private Transform listener;
    private Vector3 listenerPos;
    private float timer;
    private Comparison<ItemAudioManager> byDistance;

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        byDistance = CompareByDistance;
    }

    public void Register(ItemAudioManager m)
    {
        if (!active.Contains(m))
        {
            active.Add(m);
        }
    }

    // Static + null-guarded so callers in OnDisable/teardown never resurrect the manager.
    public static void UnregisterSafe(ItemAudioManager m)
    {
        if (_instance != null)
        {
            _instance.active.Remove(m);
        }
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer < evaluateInterval)
        {
            return;
        }
        timer = 0f;
        Evaluate();
    }

    private void Evaluate()
    {
        if (listener == null)
        {
            if (Camera.main == null)
            {
                return;
            }
            listener = Camera.main.transform;
        }
        listenerPos = listener.position;

        // Drop any destroyed entries defensively.
        for (int i = active.Count - 1; i >= 0; i--)
        {
            if (active[i] == null)
            {
                active.RemoveAt(i);
            }
        }

        active.Sort(byDistance); // nearest first

        for (int i = 0; i < active.Count; i++)
        {
            active[i].SetAudible(i < maxVoices);
        }
    }

    private int CompareByDistance(ItemAudioManager a, ItemAudioManager b)
    {
        float da = (a.transform.position - listenerPos).sqrMagnitude;
        float db = (b.transform.position - listenerPos).sqrMagnitude;
        return da.CompareTo(db);
    }
}
