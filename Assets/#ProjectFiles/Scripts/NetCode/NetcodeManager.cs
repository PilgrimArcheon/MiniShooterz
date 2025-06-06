using Unity.Netcode;
using UnityEngine;

public class NetcodeManager : NetworkBehaviour
{
    public static NetcodeManager Instance;
    public PlayerManager HostPlayer;

    private void Awake()
    {
        if (Instance) Destroy(gameObject);
        else Instance = this;//Instance the Script

        DontDestroyOnLoad(gameObject);
    }

    public void SpawnNetObject(GameObject gObject)
    {
        if (NetworkManager.Singleton.IsListening && IsServer)
        {
            NetworkObject netObject = gObject.GetComponent<NetworkObject>();
            if (!netObject.IsSpawned) netObject.Spawn();
        }
    }

    public void ShowConnectOverlay() => NetworkAPIManager.Instance.ShowConnectOverlay();
}
