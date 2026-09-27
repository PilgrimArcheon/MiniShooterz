using UnityEngine;
using ArcNet;
using System.Collections.Generic;

public class NetGameManager : ArcNetCallbacks
{
    public static NetGameManager Instance;
    
    [Header("Spawns")]
    public Transform[] teamOneSpawnPoints;
    public Transform[] teamTwoSpawnPoints;
    public string playerPrefabName = "NetPlayerPrefab"; // Must match Resources folder name

    public int myTeam;
    public bool isGameOver;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (ArcNetClient.Instance.InRoom)
        {
            SpawnLocalPlayer();
        }
    }

    private void SpawnLocalPlayer()
    {
        // Simple team assignment based on Actor Number (Odds vs Evens)
        myTeam = ArcNetClient.Instance.LocalActorNumber % 2; 
        
        Transform[] spawnPoints = (myTeam == 0) ? teamOneSpawnPoints : teamTwoSpawnPoints;
        Transform mySpawn = GetAvailableSpawnPoint(spawnPoints);

        // Network Instantiate (Make sure prefab is in a "Resources" folder)
        GameObject myPlayerObj = ArcNetClient.Instance.Instantiate(
            playerPrefabName, 
            mySpawn.position, 
            mySpawn.rotation
        );

        // Setup the local instance
        NetPlayerController controller = myPlayerObj.GetComponent<NetPlayerController>();
        
        // Assuming SaveManager holds their chosen character ID
        int charId = SaveManager.Instance != null ? SaveManager.Instance.state.charId : 0;
        
        controller.CharacterUserSetUp(ArcNetClient.Instance.UserId, charId, myTeam, ArcNetClient.Instance.LocalActorNumber);
    }

    private Transform GetAvailableSpawnPoint(Transform[] spawnPoints)
    {
        // For simplicity, just pick a random one, or implement your availability logic
        return spawnPoints[Random.Range(0, spawnPoints.Length)];
    }

    // ArcNet Callback: Handle player disconnects
    public override void OnPlayerLeftRoom(ArcNetPlayer playerWhoLeft)
    {
        Debug.Log($"{playerWhoLeft.DisplayName} left the match.");
        // UI updates or forfeit logic here
    }
}