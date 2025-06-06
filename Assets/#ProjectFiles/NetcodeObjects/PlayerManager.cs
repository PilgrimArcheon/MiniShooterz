using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerManager : NetworkBehaviour
{
    public string playerId;
    [SerializeField] GameObject GameManagerObject;
    private int team;
    private int teamId;
    private int charId;
    private int kills;
    private int deaths;
    private int xpAmount;

    void Awake() => DontDestroyOnLoad(gameObject);

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        playerId = SaveManager.Instance.state.userName;

        if (IsOwner) GetOwnerCharRpc(SaveManager.Instance.state.charId);

        if (IsHost)
        {
            NetcodeManager.Instance.HostPlayer = this;
            GetHostRpc(OwnerClientId);
        }
    }

    void OnEnable() => SceneManager.activeSceneChanged += OnSceneChanged;

    void OnDisable() => SceneManager.activeSceneChanged -= OnSceneChanged;

    private void OnSceneChanged(Scene prevScene, Scene newScene)
    {
        if (newScene.name.Contains("BattleArena"))
        {
            if (IsNetworkOwned)
            {
                GameObject GameManager = Instantiate(GameManagerObject); // Instantiate the player

                if (NetcodeManager.Instance)
                    NetcodeManager.Instance.SpawnNetObject(GameManager);

                Debug.Log("Has Spawned Network Objects");
            }
        }
    }

    public void CreatePlayerController()
    {
        Transform[] spawnTransforms = team == 0 ? SetUpManager.Instance.teamOneSpawnPoints : SetUpManager.Instance.teamTwoSpawnPoints;
        Transform spawnPoint = GameManager.Instance.GetAvailableSpawnPoint(spawnTransforms);

        GameObject gObject = HandleSpawn(SetUpManager.Instance.playerPrefab, spawnPoint);

        PlayerCharacterController player = gObject.GetComponent<PlayerCharacterController>();
        player.playerId = playerId;
        SetUpManager.Instance.occupier.transform.parent = spawnPoint;

        player.CharacterUserSetUp(playerId, charId, team, teamId);

        switch (team)
        {
            case 0:
                GameManager.Instance.teamOne.Add(player.gameObject);
                break;
            case 1:
                GameManager.Instance.teamTwo.Add(player.gameObject);
                break;
            default: break;
        }
        //GameManager.Instance.NetworkSpawn(gObject);
    }

    public GameObject HandleSpawn(GameObject gObjectPrefab, Transform spawnPoint)
    {
        GameObject gObject = Instantiate(gObjectPrefab, spawnPoint.position, spawnPoint.rotation);
        gObject.GetComponent<NetworkObject>().SpawnWithOwnership(OwnerClientId);

        return gObject;
    }

    public void UpdateStats(string statType, int value)
    {
        switch (statType)
        {
            case "kills":
                kills += value;
                break;
            case "deaths":
                deaths += value;
                break;
            case "xpAmount":
                xpAmount += value;
                break;
            default: break;
        }

        UpdatePlayerStatRPC(kills, deaths, xpAmount);

        Debug.Log("Stats Update: " + statType + ": " + value);
    }

    private bool IsNetworkOwned { get { return IsServer && IsOwner; } }// Check if the object is owned by the server and the client

    public void AssignTeam(int _team, int _teamId)
    {
        if (IsOwner) PlayerTeamRpc(_team, _teamId, OwnerClientId);
    }

    public void LoadMenu(string sceneToLoad)
    {
        // Load the menu scene
        // If we're the host, start the game
        if (IsHost) LoadScreenRpc(sceneToLoad);
    }

    [Rpc(SendTo.Everyone)] // Get the player's team info
    public void PlayerTeamRpc(int _team, int _teamId, ulong _ownerId)
    {
        if (_ownerId != OwnerClientId) return;

        team = _team;
        teamId = _teamId;
    }

    [Rpc(SendTo.Everyone)]
    public void LoadScreenRpc(string sceneToLoad)
    {
        MenuManager.Instance.OpenMenu("loadUp");
        FindObjectOfType<LoadUp>().UpdateSceneToOpen(sceneToLoad);
    }

    [Rpc(SendTo.Everyone)]
    public void GetHostRpc(ulong ownerId)
    {
        PlayerManager[] players = FindObjectsOfType<PlayerManager>();
        foreach (var player in players)
        {
            if (player.GetComponent<NetworkObject>().OwnerClientId == ownerId)
            {
                NetcodeManager.Instance.HostPlayer = player;
                break;
            }
        }
    }

    [Rpc(SendTo.Everyone)]
    public void UpdatePlayerStatRPC(int _kills, int _deaths, int _xpAmount)
    {
        kills = _kills;
        deaths = _deaths;
        xpAmount = _xpAmount;
    }
    
    [Rpc(SendTo.Everyone)]
    public void GetOwnerCharRpc(int _charId) => charId = _charId;

    public int GetTeam { get { return team; } }
    public int GetTeamID { get { return teamId; } }
    public int GetKills { get { return kills; } }
    public int GetDeaths { get { return deaths; } }
    public int GetXpAmount { get { return xpAmount; } }
}


public struct CharacterVariables
{
    public string username;
    public int team;
    public int charId;
    public int _id;
}