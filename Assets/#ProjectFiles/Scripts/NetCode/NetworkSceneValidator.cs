using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkSceneValidator : NetworkBehaviour
{
    private NetworkVariable<FixedString64Bytes> hostSceneName = new(
        "", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    void Start()
    {
        if (NetcodeManager.Instance) 
            NetcodeManager.Instance.SpawnNetObject(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            hostSceneName.Value = SceneManager.GetActiveScene().name;
        }

        // Check scene match when network is ready
        NetworkManager.Singleton.OnClientConnectedCallback += ValidateClientScene;
    }

    private void ValidateClientScene(ulong clientId)
    {
        if (!IsServer) return;

        Debug.Log($"Client {clientId} connected. Host scene: {hostSceneName.Value}");
    }

    void Update()
    {
        if (!IsClient || IsServer) return;

        string currentScene = SceneManager.GetActiveScene().name;
        if (currentScene != hostSceneName.Value.ToString())
        {
            Debug.LogWarning($"Scene mismatch! Reloading to: {hostSceneName.Value}");
            SceneManager.LoadScene(hostSceneName.Value.ToString());
        }
    }
}
