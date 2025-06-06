using System.Collections.Generic;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine.SceneManagement;
using UnityEngine;
using TMPro;
using System.Threading.Tasks;
using System.Collections;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; set; }

    private List<Lobby> availableLobbies = new();
    [SerializeField] int minPlayersPerMatch = 1;
    [SerializeField] int maxPlayersPerMatch = 5;
    [SerializeField] Button retryAuthButton;
    [SerializeField] TMP_Text infoText;
    [SerializeField] TMP_Text playerCountText;

    private Lobby hostLobby;
    private Lobby joinedLobby;
    private string LobbyRelayCode;
    private bool hasRelayCode;
    string playerName;
    int playerXp;
    int playerChar;
    string playerReadyState;

    float heartBeatTimer;
    float lobbyUpdateTimer;
    InitializationOptions initializationOptions;
    // Use this for initialization
    private void Awake() => Instance = this;

    // Start is called before the first frame update
    private void Start() => Authenticate();

    private async void Authenticate()
    {
        string userName = SaveManager.Instance.state.userName;
        bool hasUserName = !string.IsNullOrEmpty(userName);
        playerName = hasUserName ? userName : "Player_" + Random.Range(100, 1000);
        playerXp = SaveManager.Instance.state.totalXP;
        SaveManager.Instance.state.userName = playerName;
        playerChar = 0;

        infoText.text = "CONNECTING TO MATCH MAKING SERVER....";

        initializationOptions = new();
        initializationOptions.SetProfile(playerName);

        retryAuthButton.onClick.AddListener(Authenticate);

        await UnityServices.InitializeAsync(initializationOptions);

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            AuthenticationService.Instance.SignedIn += OnSignedIn;

            AuthenticationService.Instance.SignInFailed += OnSignedInFail;

            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
        else OnSignedIn();
    }

    void OnSignedIn()
    {
        MenuManager.Instance.OpenMenu("lobbyLoad");
        Debug.Log("Signed in " + AuthenticationService.Instance.PlayerId);
        LookForMatch();
    }

    private void OnSignedInFail(RequestFailedException exception)
    {
        string mssg = exception.Message;
        ErrorOccured(mssg);
    }

    public void LookForMatch()
    {
        playerReadyState = "Searching";
        StartCoroutine(TryJumpingIntoLobby());
    }

    private IEnumerator TryJumpingIntoLobby()
    {
        infoText.text = "Searching For Available Lobbies...";

        yield return new WaitForSeconds(2f);

        var getLobbies = ListLobbies();

        yield return new WaitUntil(() => getLobbies.IsCompleted);

        if (getLobbies.IsFaulted || getLobbies.IsCanceled || getLobbies.Result == null) ErrorOccured("Error Finding Lobby!");
        else
        {
            availableLobbies = getLobbies.Result;

            if (availableLobbies != null)
                Debug.Log("Lobbies found: " + availableLobbies.Count);

            yield return new WaitForSeconds(2f);

            if (availableLobbies.Count > 0) JoinLobby();
            else CreateLobby();
        }
    }

    private void Update()
    {
        HandleLobbyHeartBeat();
        HandleLobbyPollForUpdates();

        if (hostLobby != null && joinedLobby != null)
        {
            playerCountText.text = hostLobby.Players.Count - 1 + "/" + (hostLobby.MaxPlayers - 1) + " Players Found";
        }
    }

    private async void HandleLobbyHeartBeat()
    {
        if (hostLobby != null)
        {
            if (hostLobby.HostId != AuthenticationService.Instance.PlayerId) return;

            heartBeatTimer -= Time.deltaTime;

            if (heartBeatTimer < 0f)
            {
                heartBeatTimer = 15f;

                await LobbyService.Instance.SendHeartbeatPingAsync(hostLobby.Id);
            }
        }
        else
        {
            if (joinedLobby != null)
            {
                if (joinedLobby.HostId == AuthenticationService.Instance.PlayerId)
                {
                    hostLobby = joinedLobby;
                }
            }
        }
    }

    private async void HandleLobbyPollForUpdates()
    {
        if (joinedLobby != null)
        {
            lobbyUpdateTimer -= Time.deltaTime;

            if (lobbyUpdateTimer < 0f)
            {
                lobbyUpdateTimer = 1.15f;

                Lobby lobby = await LobbyService.Instance.GetLobbyAsync(joinedLobby.Id);
                joinedLobby = lobby;
                hostLobby = lobby;

                string mLobbyRelayCode = lobby.Data["LobbyRelayCode"].Value;
                if (mLobbyRelayCode != "0" && !string.IsNullOrEmpty(mLobbyRelayCode) && !hasRelayCode)
                {
                    LobbyRelayCode = lobby.Data["LobbyRelayCode"].Value;
                    Debug.Log("LobbyRelayCode: " + LobbyRelayCode);

                    RelayManager.Instance.JoinRelay(LobbyRelayCode);
                    hasRelayCode = true;
                }
            }
        }
    }

    private async void CreateLobby()
    {
        try
        {
            string lobbyName = "Lobby_" + Random.Range(100, 1000);
            int maxPlayers = maxPlayersPerMatch;

            CreateLobbyOptions createLobbiesOptions = new()
            {
                Player = GetPlayer(),
                Data = new Dictionary<string, DataObject>
                {
                    { "PlayerXP", new DataObject(DataObject.VisibilityOptions.Public, $"{playerXp}", DataObject.IndexOptions.N1) },
                    { "LobbyRelayCode" , new DataObject(DataObject.VisibilityOptions.Member, "0")}
                }
            };

            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, createLobbiesOptions);

            if (lobby != null)
            {
                hostLobby = lobby;
                joinedLobby = lobby;

                UpdatePlayerData("PlayerName", playerName);

                infoText.text = "Created Lobby: " + lobby.Name + " - " + lobby.Data["PlayerXP"].Value;

                Debug.Log("Created Lobby! " + lobby.Name + " - " + lobby.MaxPlayers + " - " + lobby.Data["PlayerXP"].Value);

                LobbyRelayCode = await RelayManager.Instance.CreateRelay();

                UpdateLobbyData("LobbyRelayCode", LobbyRelayCode);

                hasRelayCode = true;

                PrintPlayersInfo(lobby);

                StartCoroutine(CheckIfPlayersAreReady());
            }
            else ErrorOccured();
        }
        catch (LobbyServiceException e) { Debug.Log(e); ErrorOccured(e.Message); }
    }

    private IEnumerator CheckIfPlayersAreReady()
    {
        const int maxTries = 3;

        int tryCount = 0;
        bool playersAreReady = false;

        if (hostLobby == null)
        {
            Debug.LogWarning("Host lobby is null.");
            yield break;
        }

        infoText.text = "Waiting for Players!!!";

        while (!playersAreReady && tryCount < maxTries)
        {
            yield return new WaitForSeconds(5f);

            int readyCount = 0;
            var players = hostLobby.Players;

            if (players.Count >= minPlayersPerMatch)
            {
                foreach (var player in players)
                {
                    if (player.Data.TryGetValue("PlayerReadyState", out var readyState)
                        && readyState.Value == "Ready")
                    {
                        readyCount++;
                    }
                }

                if (readyCount == hostLobby.MaxPlayers)
                {
                    playersAreReady = true;
                    break;
                }
            }

            yield return new WaitForSeconds(5f);
            tryCount++;
        }

        if (playersAreReady) infoText.text = "All players ready!";
        else infoText.text = "No more Players found. Match starting anyway...";

        MenuManager.Instance.OpenMenu("loadUp");
    }

    private async Task<List<Lobby>> ListLobbies()
    {
        try
        {
            QueryLobbiesOptions queryLobbiesOptions = new()
            {
                Count = 25,
                Filters = new List<QueryFilter> {
                    new(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT),
                    // new(QueryFilter.FieldOptions.N1, $"{playerXp - 100}", QueryFilter.OpOptions.GE),
                    // new(QueryFilter.FieldOptions.N1, $"{playerXp + 100}", QueryFilter.OpOptions.LE)
                },
                Order = new List<QueryOrder> {
                    new(false, QueryOrder.FieldOptions.Created)
                }
            };

            QueryResponse queryResponse = await Lobbies.Instance.QueryLobbiesAsync(queryLobbiesOptions);

            return queryResponse.Results;
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
            ErrorOccured(e.Message);
            return null;
        }
    }

    private async void JoinLobby()
    {
        try
        {
            JoinLobbyByIdOptions joinLobbyByIdOptions = new() { Player = GetPlayer() };

            Lobby lobby = await Lobbies.Instance.JoinLobbyByIdAsync(availableLobbies[0].Id, joinLobbyByIdOptions);

            if (lobby != null)
            {
                joinedLobby = lobby;

                infoText.text = "Joined Lobby: " + lobby.Name;

                UpdatePlayerData("PlayerName", playerName);

                PrintPlayersInfo(joinedLobby);

                StartCoroutine(Wait());

                IEnumerator Wait()
                {
                    yield return new WaitForSeconds(1.5f);

                    infoText.text = "Waiting For Other Players!!!";
                }
            }
            else ErrorOccured();
        }
        catch (LobbyServiceException e) { Debug.Log(e); ErrorOccured(e.Message); }
    }

    private void PrintPlayersInfo(Lobby lobby)
    {
        Debug.Log("LobbyStats: " + lobby.Name + " " + lobby.Data["LobbyRelayCode"].Value);

        foreach (var player in lobby.Players)
        {
            Debug.Log(player.Id + " " + player.Data["PlayerName"].Value + " " + player.Data["PlayerXP"].Value);
        }
    }

    private Unity.Services.Lobbies.Models.Player GetPlayer()
    {
        return new Unity.Services.Lobbies.Models.Player
        {
            Data = new Dictionary<string, PlayerDataObject>
            {
                {"PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerName)},
                {"PlayerXP", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, $"{playerXp}")},
                {"PlayerChar", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, $"{playerChar}")},
                {"PlayerReadyState", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerReadyState)}
            }
        };
    }

    public async void UpdatePlayerData(string data, string value)
    {
        try
        {
            await LobbyService.Instance.UpdatePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId, new UpdatePlayerOptions
            {
                Data = new Dictionary<string, PlayerDataObject> {
                    { data, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, value)}
                }
            });
        }
        catch (LobbyServiceException e) { Debug.Log(e); ErrorOccured(e.Message); }
    }

    public void StartMatch(string sceneToLoad)
    {
        NetworkManager.Singleton.SceneManager.LoadScene(sceneToLoad, LoadSceneMode.Single);
    }

    private async void UpdateLobbyData(string data, string value)
    {
        try
        {
            await LobbyService.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject> {
                    { data, new DataObject(DataObject.VisibilityOptions.Member, value) }
                }
            });
        }
        catch (LobbyServiceException e) { Debug.Log(e); ErrorOccured(e.Message); }
    }


    private async void LeaveLobby()
    {
        try
        {
            await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId);
        }
        catch (LobbyServiceException e) { Debug.Log(e); ErrorOccured(e.Message); }
    }

    IEnumerator NoSessionsCantPlay()
    {
        MenuManager.Instance.OpenMenu("cantPlay");
        yield return new WaitForSecondsRealtime(2f);
        MenuManager.Instance.OpenMenu("main");
        StopCoroutine(NoSessionsCantPlay());
    }

    public bool IsLobbyHost()
    {
        return joinedLobby.HostId == AuthenticationService.Instance.PlayerId;
    }

    public void ErrorOccured(string error = "")
    {
        MenuManager.Instance.OpenMenu("lobbyError-Overlay");
        infoText.text = error;
    }

    void OApplicationQuit()
    {
        // Clean up when the application quits
        if (IsLobbyHost() && joinedLobby.AvailableSlots <= 1) LobbyService.Instance.DeleteLobbyAsync(joinedLobby.Id);
    }
}