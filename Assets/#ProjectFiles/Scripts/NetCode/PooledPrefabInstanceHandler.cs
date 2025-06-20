using Unity.Netcode;
using UnityEngine;

public class PooledPrefabInstanceHandler : INetworkPrefabInstanceHandler
{
    private GameObject prefab;

    public PooledPrefabInstanceHandler(GameObject prefab)
    {
        this.prefab = prefab;
    }

    public NetworkObject Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation)
    {
        return NetworkObjectPool.Instance.GetNetworkObject(prefab, position, rotation);
    }

    public void Destroy(NetworkObject networkObject)
    {
        NetworkObjectPool.Instance.ReturnNetworkObject(networkObject);
    }
}