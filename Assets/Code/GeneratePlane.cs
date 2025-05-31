using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Voxel : MonoBehaviour
{
    public static int length = 150;
    public GameObject cube;
    public GameObject player;
    public GameObject item;
    public int detailScale = 8;
    public int noiseHeight = 3;
    private Vector3 startPos = Vector3.zero;
    private Dictionary<Vector3, Tile> cubePos;
    private Dictionary<Vector3, Tile> itemPos;
    public int itemChance = 1;
    public static int itemDivider = 40;
    private int itemMax = 100; //(length * length) / itemDivider;
    private static int yOffset = 5;

    private bool IsInGrid(Vector3 position)
    {
        int xMin = XPlayerLocation - length;
        int xMax = XPlayerLocation + length;
        int zMin = ZPlayerLocation - length;
        int zMax = ZPlayerLocation + length;

        return (position.x >= xMin && position.x <= xMax && position.z >= zMin && position.z <= zMax);
    }

    private int XPlayerMove => (int)(player.transform.position.x - startPos.x);
    private int ZPlayerMove => (int)(player.transform.position.z - startPos.z);

    private int XPlayerLocation => (int)Mathf.Floor(player.transform.position.x);
    private int ZPlayerLocation => (int)Mathf.Floor(player.transform.position.z);

    void Start()
    {
        cubePos = new Dictionary<Vector3, Tile>();
        itemPos = new Dictionary<Vector3, Tile>();
        GenerateTerrain(length);
    }

    private void Update()
    {
        if (Mathf.Abs(XPlayerMove) >= 1 || Mathf.Abs(ZPlayerMove) >= 1)
        {
            GenerateTerrain(length);
        }
    }

    private void GenerateTerrain(int length)
    {
        float cTime = Time.realtimeSinceStartup;
        ManageItems(cTime);
    }

    private void ManageItems(float cTime)
    {
        var newItemPos = new Dictionary<Vector3, Tile>();
        var itemsToRemove = new List<Vector3>();

        foreach (var kvp in itemPos)
        {
            Vector3 loc = kvp.Key;
            GameObject itemObject = kvp.Value.tileObject;
            Vector3 itemPosition = itemObject.transform.position;

            bool shouldDestroy = false;

            if (!IsInGrid(itemPosition) || kvp.Value.GetActiveDuration() >= kvp.Value.rndTime)
            {
                shouldDestroy = true;
            }

            if (shouldDestroy)
            {
                itemsToRemove.Add(loc);
                Destroy(itemObject);
            }
            else
            {
                kvp.Value.cTimestamp = cTime;
                newItemPos[loc] = kvp.Value;
            }
        }

        foreach (Vector3 loc in itemsToRemove)
        {
            itemPos.Remove(loc);
        }

        int currentItemCount = newItemPos.Count;
        int itemsToSpawn = itemMax - currentItemCount;

        for (int i = 0; i < itemsToSpawn; i++)
        {
            int x = Random.Range(-length, length);
            int z = Random.Range(-length, length);
            Vector3 loc = new Vector3(x + XPlayerLocation,
                (yNoise(x + XPlayerLocation, z + ZPlayerLocation, detailScale) * noiseHeight) + 10f,
                z + ZPlayerLocation);

            if (!newItemPos.ContainsKey(loc))
            {
                int rndIndex = Random.Range(0, 100);
                if (rndIndex <= itemChance)
                {
                    Quaternion randomRotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);

                    GameObject itemInstance = Instantiate(item, loc, randomRotation, transform);

                    // Ensure ItemAudioManager is added and initialized
                    var audioManager = itemInstance.GetComponent<ItemAudioManager>();
                    if (audioManager == null)
                    {
                        audioManager = itemInstance.AddComponent<ItemAudioManager>();
                    }
                    audioManager.listener = Camera.main.transform;

                    Tile o = new Tile(cTime, itemInstance);
                    newItemPos[loc] = o;
                }
            }
        }

        itemPos = newItemPos;
    }

    private class Tile
    {
        public float cTimestamp;
        public GameObject tileObject;
        public float activationTime;
        public int rndTime;

        public Tile(float cTimestamp, GameObject tileObject)
        {
            this.tileObject = tileObject;
            this.cTimestamp = cTimestamp;
            this.activationTime = Time.time;
            this.rndTime = Random.Range(5, 10);
        }

        public float GetActiveDuration()
        {
            return Time.time - activationTime;
        }
    }

    private float yNoise(int x, int z, float detailScale)
    {
        float xNoise = (x + transform.position.x) / detailScale;
        float zNoise = (z + transform.position.y) / detailScale;
        return Mathf.PerlinNoise(xNoise, zNoise);
    }
}