using Unity.Netcode;
using UnityEngine;

public class DestroyOnLifeTime : NetworkBehaviour
{
    [SerializeField] float lifeTime = 1f;
    float timeToStay;

    bool started;
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            started = true;
            timeToStay = Time.time + lifeTime;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer) started = false;// Reset started flag
    }

    void Update()
    {
        if (!started) return;

        if (timeToStay < Time.time)
        {
            // Time to return to the pool from whence it came.
            var networkObject = gameObject.GetComponent<NetworkObject>();
            networkObject.Despawn();
            return;
        }
    }
}