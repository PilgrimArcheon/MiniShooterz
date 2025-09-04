using System.Collections.Generic;
using UnityEngine;

public class ItemSpawner : MonoBehaviour
{
    public static ItemSpawner Instance;
    [SerializeField] private float spawnInterval = 5f;// time between each spawn
    private List<ItemBox> itemBoxes = new(); // List of item boxes spawned
    private List<Transform> spawnPoints; // List of spawn points
    private ItemData[] ItemsToSpawn; // List of spawn items
    private List<Transform> spawnedTransList = new(); // List of spawned points with items
    void Awake() => Instance = this; // Set instance to this script

    public void Start()
    {
        spawnedTransList = new(); // Clear list on spawn
        ItemsToSpawn = SetUpManager.Instance.itemsToSpawn;
        spawnPoints = SetUpManager.Instance.spawnPoints;// Get the spawn points from the setup manager

        InvokeRepeating(nameof(SpawnItems), 1f, spawnInterval);
    }

    public void SpawnItems()
    {
        if (itemBoxes.Count >= 3) return;

        // spawn a random item from the array of items to spawn
        float totalChance = 0f;
        foreach (var item in ItemsToSpawn)
        {
            totalChance += item.spawnChance;
        }

        float randomValue = Random.Range(0f, totalChance);
        float cumulative = 0f;

        foreach (var item in ItemsToSpawn)
        {
            cumulative += item.spawnChance;
            if (randomValue <= cumulative)
            {
                Spawn(item);
                return;
            }
        }
    }

    void Spawn(ItemData item)
    {
        // Spawn the item at a random spawn point
        if (spawnPoints.Count == 0 || item.itemPrefab == null)
            return;

        int index = Random.Range(0, spawnPoints.Count);

        GameObject spawnedItem = Instantiate(item.itemPrefab.gameObject, spawnPoints[index].position, spawnPoints[index].rotation);

        itemBoxes.Add(spawnedItem.GetComponent<ItemBox>());
        spawnedTransList.Add(spawnPoints[index]);
        spawnPoints.Remove(spawnPoints[index]);
    }

    public void RemoveItemBox(ItemBox itemBox)
    {
        int index = itemBoxes.IndexOf(itemBox);
        if (spawnedTransList.Count > index)
        {
            spawnPoints.Add(spawnedTransList[index]);
            spawnedTransList.Remove(spawnedTransList[index]);
        }
        
        Destroy(itemBox.gameObject);
    }
}

[System.Serializable]
public class ItemData
{
    public ItemBox itemPrefab;
    [Range(0f, 100f)]
    public float spawnChance; // As a percentage (e.g. 40%)
}