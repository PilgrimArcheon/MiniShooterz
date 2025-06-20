using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using System;

public class NetworkObjectPool : NetworkBehaviour
{
    public static NetworkObjectPool Instance { get; private set; }
    [SerializeField] PoolObject[] networkSpawnObjects;
    private Dictionary<uint, Queue<NetworkObject>> pooledObjects = new Dictionary<uint, Queue<NetworkObject>>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        RegisterAllPoolObjects();
    }

    public void RegisterAllPoolObjects()
    {
        foreach (var obj in networkSpawnObjects)
        {
            RegisterPrefab(obj.gObject, obj.poolSize);
        }
    }

    public void RegisterPrefab(GameObject prefab, int initialSize)
    {
        uint prefabHash = prefab.GetComponent<NetworkObject>().PrefabIdHash;

        if (!pooledObjects.ContainsKey(prefabHash))
        {
            pooledObjects[prefabHash] = new Queue<NetworkObject>();

            for (int i = 0; i < initialSize; i++)
            {
                GameObject obj = Instantiate(prefab);
                NetworkObject netObj = obj.GetComponent<NetworkObject>();
                netObj.gameObject.SetActive(false);
                netObj.Spawn();
                pooledObjects[prefabHash].Enqueue(netObj);
            }

            NetworkManager.Singleton.PrefabHandler.AddHandler(prefab, new PooledPrefabInstanceHandler(prefab));
        }
    }

    public NetworkObject GetNetworkObject(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        uint prefabHash = prefab.GetComponent<NetworkObject>().PrefabIdHash;

        if (pooledObjects.TryGetValue(prefabHash, out var queue) && queue.Count > 0)
        {
            NetworkObject obj = queue.Dequeue();
            obj.transform.SetPositionAndRotation(position, rotation);
            obj.gameObject.SetActive(true);
            return obj;
        }
        else
        {
            GameObject obj = Instantiate(prefab, position, rotation);
            return obj.GetComponent<NetworkObject>();
        }
    }

    public void ReturnNetworkObject(NetworkObject obj)
    {
        obj.gameObject.SetActive(false);
        uint prefabHash = obj.PrefabIdHash;
        if (pooledObjects.ContainsKey(prefabHash))
        {
            pooledObjects[prefabHash].Enqueue(obj);
        }
        else
        {
            Destroy(obj.gameObject);
        }
    }
}


[Serializable]
public struct PoolObject
{
    public GameObject gObject;
    public int poolSize;
}