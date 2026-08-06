using System.Collections.Generic;
using UnityEngine;

// CANOPY/FLEDGE 3.3b-ii — invisible acoustic geometry for occlusion. Earth has no visual world, so
// there's nothing for sound to be blocked by. This scatters lightweight, world-anchored trunk
// colliders (triggers — they never physically block the player) using Perlin noise, so a forest of
// "trees" exists in the same spots every time you pass. ItemAudioManager raycasts source→listener
// against these (AcousticTrees.TreeLayerMask) and muffles anything with a trunk in the way.
//
// Trees within `despawnRadius` of the player are removed, so you never end up standing inside a
// trunk (which would occlude everything oddly). Auto-created by ItemSpawner.
public class AcousticTrees : MonoBehaviour
{
    public Transform player;

    [Header("Field")]
    [Tooltip("Trees exist out to here — keep near how far sounds are heard so occlusion covers them.")]
    public float activeRadius = 60f;
    [Tooltip("Grid spacing (m) of potential tree spots. Smaller = denser forest (more colliders).")]
    public float gridSize = 8f;
    [Tooltip("Fraction of grid cells that hold a tree (clumps into thickets and clearings).")]
    [Range(0f, 1f)] public float treeDensity = 0.45f;
    [Tooltip("Trees within this of the player despawn, so you never stand inside a trunk.")]
    public float despawnRadius = 1f;
    public float groundY = 9f;

    [Header("Trunk")]
    public float trunkRadius = 0.4f;
    public float trunkHeight = 6f;
    [Tooltip("Physics layer used for trunks; ItemAudioManager raycasts occlusion against this.")]
    public int treeLayer = 30;

    // Exposed so ItemAudioManager knows which layer to raycast for occlusion.
    public static int TreeLayerMask { get; private set; }

    private readonly Dictionary<Vector2Int, GameObject> trees = new Dictionary<Vector2Int, GameObject>();
    private readonly Stack<GameObject> pool = new Stack<GameObject>();
    private readonly List<Vector2Int> scratch = new List<Vector2Int>();
    private float tick;

    void Start()
    {
        TreeLayerMask = 1 << treeLayer;
    }

    void Update()
    {
        if (player == null) return;
        tick += Time.deltaTime;
        if (tick < 0.3f) return; // the field changes slowly as you walk; no need per-frame
        tick = 0f;

        Vector3 p = player.position;
        int cx = Mathf.FloorToInt(p.x / gridSize);
        int cz = Mathf.FloorToInt(p.z / gridSize);
        int span = Mathf.CeilToInt(activeRadius / gridSize);
        float active2 = activeRadius * activeRadius;
        float despawn2 = despawnRadius * despawnRadius;

        // Add trees that have come into range.
        for (int gx = cx - span; gx <= cx + span; gx++)
        {
            for (int gz = cz - span; gz <= cz + span; gz++)
            {
                var cell = new Vector2Int(gx, gz);
                if (trees.ContainsKey(cell)) continue;
                if (!TreeAt(gx, gz, out Vector3 pos)) continue;
                float dx = pos.x - p.x, dz = pos.z - p.z;
                float d2 = dx * dx + dz * dz;
                if (d2 > active2 || d2 < despawn2) continue;
                trees[cell] = GetTree(pos);
            }
        }

        // Remove trees out of range OR too close to the player.
        scratch.Clear();
        foreach (var kv in trees)
        {
            Vector3 pos = kv.Value.transform.position;
            float dx = pos.x - p.x, dz = pos.z - p.z;
            float d2 = dx * dx + dz * dz;
            if (d2 > active2 || d2 < despawn2) scratch.Add(kv.Key);
        }
        foreach (var cell in scratch) { Recycle(trees[cell]); trees.Remove(cell); }
    }

    // Deterministic per world cell: does it hold a tree, and where (with in-cell jitter so trunks
    // aren't on a rigid grid). Perlin makes the presence clump into thickets and clearings.
    private bool TreeAt(int gx, int gz, out Vector3 pos)
    {
        pos = Vector3.zero;
        float presence = Mathf.PerlinNoise(gx * 0.37f + 1000f, gz * 0.37f + 1000f);
        if (presence > treeDensity) return false;
        float ox = Mathf.PerlinNoise(gx * 1.7f + 50f, gz * 0.9f + 50f);
        float oz = Mathf.PerlinNoise(gx * 0.9f + 90f, gz * 1.7f + 90f);
        pos = new Vector3((gx + ox) * gridSize, groundY, (gz + oz) * gridSize);
        return true;
    }

    private GameObject GetTree(Vector3 pos)
    {
        GameObject t = pool.Count > 0 ? pool.Pop() : NewTree();
        t.transform.position = pos;
        t.SetActive(true);
        return t;
    }

    private GameObject NewTree()
    {
        var go = new GameObject("AcousticTree");
        go.layer = treeLayer;
        go.transform.SetParent(transform);
        var c = go.AddComponent<CapsuleCollider>();
        c.isTrigger = true; // occlusion-only — never physically blocks the player
        c.radius = trunkRadius;
        c.height = trunkHeight;
        c.center = new Vector3(0f, trunkHeight * 0.5f, 0f);
        return go;
    }

    private void Recycle(GameObject t)
    {
        t.SetActive(false);
        pool.Push(t);
    }
}
