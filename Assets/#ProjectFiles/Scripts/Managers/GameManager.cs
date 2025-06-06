using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using Unity.Netcode;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;
    [Header("Game Settings")]
    public int maxKills = 10;  // Max kills for a team to win
    public float gameTimeLimit = 300f;  // 5 minutes
    public int teamSize = 3;  // 1v1 or 3v3
    public int characterSetUpLength = 3;

    public List<GameObject> teamOne = new();
    public List<GameObject> teamTwo = new();
    public List<PlayerDetails[]> characterDetails = new();
    public int playerTeam;
    public int playerPoints;

    public Vector2[] MovePoints;

    [Header("Game Stats")]
    public int playersForTeamOne;
    public int playersForTeamTwo;

    public int totalTeamOneKills;
    public int totalTeamTwoKills;
    public float timer;

    public bool gameStarted = false;
    public bool isGameOver = false;
    public bool isInFinalEliminationMode = false;

    [Header("Other Settings")]
    public bool forcedMobile;
    public bool NetCodeActive;

    void Awake()
    {
        Instance = this;

        NetCodeActive = NetcodeManager.Instance;

        timer = gameTimeLimit;
    }

    private void Start()
    {
        if (!NetCodeActive) SpawnTeams();
    }

    public override void OnNetworkSpawn()
    {
        if (NetCodeActive) StartCoroutine(TrySpawnTeams()); // Spawn Net Object
    }

    public Transform GetAvailableSpawnPoint(Transform[] spawnPoints)
    {
        foreach (var point in spawnPoints)
        {
            if (point.childCount == 0)
            {
                return point;
            }
        }
        return spawnPoints[0];
    }

    private IEnumerator TrySpawnTeams()
    {
        CreateCharacterDetailsSlots();

        if (NetCodeActive)
        {
            int clientsLoaded = NetworkManager.Singleton.ConnectedClientsIds.Count;

            int playersLoaded = 0;
            while (playersLoaded != clientsLoaded)
            {
                playersLoaded = FindObjectsOfType<PlayerManager>().Length;
            }

            yield return new WaitForSeconds(1.5f);
            SpawnTeams();
        }
    }

    void CreateCharacterDetailsSlots()
    {
        for (int i = 0; i < 2; i++)
        {
            PlayerDetails[] details = new PlayerDetails[teamSize];
            characterDetails.Add(details);
        }
    }

    // Spawn teams based on available players
    private void SpawnTeams()
    {
        MenuManager.Instance.OpenMenu("mainGame");

        if (NetCodeActive)
        {
            if (!IsHost) return;

            AssignTeamId();
        }

        // Spawn the Red Team
        for (int i = 0; i < teamSize; i++)
        {
            if (i <= (playersForTeamOne - 1))  // Spawn the player in the red team
            {
                if (NetCodeActive) HandleNetCodePlayerSpawn(0, i);
                else
                {
                    PlayerCharacterController player = Instantiate(SetUpManager.Instance.playerPrefab, GetAvailableSpawnPoint(SetUpManager.Instance.teamOneSpawnPoints).position, GetAvailableSpawnPoint(SetUpManager.Instance.teamOneSpawnPoints).rotation).GetComponent<PlayerCharacterController>();
                    SetUpManager.Instance.occupier.transform.parent = GetAvailableSpawnPoint(SetUpManager.Instance.teamOneSpawnPoints);
                    player.name += $" Team-1 {i + 1}";
                    player.CharacterUserSetUp("Player_" + Random.Range(100, 999), SaveManager.Instance.state.charId, 0, i);
                    playerTeam = 0;
                    teamOne.Add(player.gameObject);
                }
            }
            else  // Spawn an AI for the rest of the red team
            {
                AICharacterController ai = Instantiate(SetUpManager.Instance.aiPrefab, GetAvailableSpawnPoint(SetUpManager.Instance.teamOneSpawnPoints).position, GetAvailableSpawnPoint(SetUpManager.Instance.teamOneSpawnPoints).rotation).GetComponent<AICharacterController>();
                SetUpManager.Instance.occupier.transform.parent = GetAvailableSpawnPoint(SetUpManager.Instance.teamOneSpawnPoints);
                ai.name += $" Team-1 {i + 1}";
                ai.CharacterUserSetUp("Bot " + Random.Range(100, 999), Random.Range(0, characterSetUpLength), 0, i);
                NetworkSpawn(ai.gameObject);
                teamOne.Add(ai.gameObject);
            }
        }

        // Spawn the Blue Team
        for (int i = 0; i < teamSize; i++)
        {
            if (i <= (playersForTeamTwo - 1))  // Spawn the player in the blue team
            {
                if (NetCodeActive) HandleNetCodePlayerSpawn(1, i);
                else
                {
                    PlayerCharacterController player = Instantiate(SetUpManager.Instance.playerPrefab, GetAvailableSpawnPoint(SetUpManager.Instance.teamTwoSpawnPoints).position, GetAvailableSpawnPoint(SetUpManager.Instance.teamTwoSpawnPoints).rotation).GetComponent<PlayerCharacterController>();
                    SetUpManager.Instance.occupier.transform.parent = GetAvailableSpawnPoint(SetUpManager.Instance.teamTwoSpawnPoints);
                    player.name += $" Team-2 {i + 1}";
                    player.CharacterUserSetUp("Player_" + Random.Range(100, 999), SaveManager.Instance.state.charId, 1, i);
                    playerTeam = 1;
                    teamTwo.Add(player.gameObject);
                }
            }
            else  // Spawn an AI for the rest of the blue team
            {
                AICharacterController ai = Instantiate(SetUpManager.Instance.aiPrefab, GetAvailableSpawnPoint(SetUpManager.Instance.teamTwoSpawnPoints).position, GetAvailableSpawnPoint(SetUpManager.Instance.teamTwoSpawnPoints).rotation).GetComponent<AICharacterController>();
                SetUpManager.Instance.occupier.transform.parent = GetAvailableSpawnPoint(SetUpManager.Instance.teamTwoSpawnPoints);
                ai.name += $" Team-2 {i + 1}";
                ai.CharacterUserSetUp("Bot " + Random.Range(100, 999), Random.Range(0, characterSetUpLength), 1, i);
                NetworkSpawn(ai.gameObject);
                teamTwo.Add(ai.gameObject);
            }
        }
    }

    public void NetworkSpawn(GameObject netObject)
    {
        if (NetcodeManager.Instance)
            NetcodeManager.Instance.SpawnNetObject(netObject);
    }

    void AssignTeamId()
    {
        if (IsHost)
        {
            playersForTeamOne = 0;
            playersForTeamTwo = 0;

            PlayerManager[] players = FindObjectsOfType<PlayerManager>();

            for (int i = 0; i < players.Length; i++)
            {
                int teamId = i % 2; // Alternate between 0 (TeamOne) and 1 (TeamTwo)

                if (teamId == 0)
                {
                    players[i].AssignTeam(0, playersForTeamOne);
                    playersForTeamOne++;
                }
                else
                {
                    players[i].AssignTeam(1, playersForTeamTwo);
                    playersForTeamTwo++;
                }
            }
        }
    }

    int playersRegistered;
    public void AddToDetails(PlayerDetails playerDetails)
    {
        if (playersRegistered != teamSize * 2)
        {
            characterDetails[playerDetails.PlayerTeam][playerDetails.PlayerId] = playerDetails;
            playersRegistered++;

            if (playersRegistered == teamSize * 2) gameStarted = true;
        }
    }

    void HandleNetCodePlayerSpawn(int team, int teamId)
    {
        PlayerManager[] players = FindObjectsOfType<PlayerManager>();

        foreach (PlayerManager player in players)
        {
            if (player.GetTeam == team && player.GetTeamID == teamId)
            {
                player.CreatePlayerController();
                break;
            }
        }
    }

    private void Update()
    {
        if (isGameOver && !gameStarted) return;

        // Countdown timer
        if (timer > 0)
        {
            timer -= Time.deltaTime;
            int min = (int)timer / 60;
            int secs = (int)(timer % 60);

            SetUpManager.Instance.timerText.text = $"{min:00}:{secs:00}";

            SetUpManager.Instance.scoreBoard[0].text = GetMyTeam == 0 ? $"{totalTeamTwoKills}" : $"{totalTeamOneKills}";
            SetUpManager.Instance.scoreBoard[1].text = GetMyTeam == 0 ? $"{totalTeamOneKills}" : $"{totalTeamTwoKills}";

            SetUpManager.Instance.gameKills.text = "Kills: " + GetMyKills.ToString();
            SetUpManager.Instance.gameXp.text = "XP: " + GetMyGameXP.ToString();
        }
        else
        {
            // If the timer runs out, check for a winner
            if (totalTeamTwoKills == totalTeamOneKills) EnterFinalEliminationMode();
            else EndGame();
        }

        if (characterDetails.Count == 2)
        {
            totalTeamTwoKills = GetAllTeamKills(characterDetails[1]);
            totalTeamOneKills = GetAllTeamKills(characterDetails[0]);

            // Check for team kills
            if (!isGameOver && (totalTeamTwoKills >= maxKills || totalTeamOneKills >= maxKills))
            {
                EndGame();
            }
        }
    }

    // Call this method when a kill is made
    public void RegisterKill(int team, int playerId)
    {
        if (IsServer || !NetcodeManager.Instance) RegisterUpdateKillRPC(team, playerId);
    }

    public void RegisterDeath(int team, int playerId)
    {
        if (IsServer || !NetcodeManager.Instance) RegisterUpdateDeathRPC(team, playerId);
    }

    public void RegisterXP(int team, int playerId, int xpAmount)
    {
        if (IsServer || !NetcodeManager.Instance) RegisterUpdateXpAmountRPC(team, playerId, xpAmount);
    }

    [Rpc(SendTo.Everyone)]
    public void RegisterUpdateKillRPC(int team, int playerId)
    {
        characterDetails[team][playerId].PlayerKills++;

        UpdatePlayerStatsCount(team, playerId, 1, "kills");

        // Check for the end of the game based on kills
        if ((totalTeamTwoKills >= maxKills || totalTeamTwoKills >= maxKills) && !isGameOver) EndGame();
    }

    [Rpc(SendTo.Everyone)]
    public void RegisterUpdateDeathRPC(int team, int playerId)
    {
        characterDetails[team][playerId].PlayerDeaths++;

        UpdatePlayerStatsCount(team, playerId, 1, "deaths");
    }

    [Rpc(SendTo.Everyone)]
    public void RegisterUpdateXpAmountRPC(int team, int playerId, int _xpAmount)
    {
        characterDetails[team][playerId].PlayerXP += _xpAmount;

        UpdatePlayerStatsCount(team, playerId, _xpAmount, "xpAmount");//XP per Kill
    }

    public void UpdatePlayerStatsCount(int team, int playerId, int value, string statType)
    {
        PlayerManager[] players = FindObjectsOfType<PlayerManager>();

        foreach (PlayerManager player in players)
        {
            if (player.GetTeam == team && player.GetTeamID == playerId)
            {
                player.UpdateStats(statType, value);
                break;
            }
        }
    }

    private int GetAllTeamKills(PlayerDetails[] players)
    {
        int allKills = 0;
        foreach (var player in players)
        {
            if (player != null) allKills += player.PlayerKills;
        }
        return allKills;
    }

    // Handle game end (either time-based or kill-based)
    private void EndGame()
    {
        if (totalTeamTwoKills > totalTeamOneKills) SetWinner(0);
        else if (totalTeamOneKills > totalTeamTwoKills) SetWinner(1);
        else
        {
            Debug.Log("Draw! Transitioning to Final Elimination Mode.");
            EnterFinalEliminationMode();
        }

        isGameOver = true;
    }

    void SetWinner(int teamCheck)
    {
        string WinText = GetMyTeam == teamCheck ? "DEFEAT!" : "VICTORY!";
        Color WinColor = GetMyTeam == teamCheck ? Color.red : Color.blue;
        Debug.Log(WinText);
        SetUpManager.Instance.endScoreBoard.text = GetMyTeam == teamCheck ? $"{totalTeamTwoKills} - {totalTeamOneKills}" : $"{totalTeamOneKills} - {totalTeamTwoKills}";
        StartCoroutine(ShowGameEndScreen(WinText, WinColor));
    }

    IEnumerator ShowGameEndScreen(string winnerString, Color color)
    {
        yield return new WaitForSeconds(1.5f);

        SetUpManager.Instance.timerText.text = $"~";
        SetUpManager.Instance.scoreBoard[0].text = $"~";
        SetUpManager.Instance.scoreBoard[1].text = $"~";

        SetUpManager.Instance.winnerText.text = winnerString;
        SetUpManager.Instance.winnerStatus.color = color;

        MenuManager.Instance.OpenMenu("endGameStats");
        SetUpManager.Instance.GameUIParent.SetActive(false);

        yield return new WaitForSeconds(2.5f);

        SetUpManager.Instance.ResultGameScreen.SetActive(true);
        SetUpManager.Instance.finalKills.text = GetMyKills.ToString();
        SetUpManager.Instance.finalXp.text = GetMyGameXP.ToString();
        SetUpManager.Instance.resultText.text = winnerString;

        int status = winnerString == "VICTORY!" ? 0 : 1;
        PlayResultsAnimation(status);

        if (NetworkAPIManager.Instance.isLoggedIn)
        {
            PlayFabNetManager.UpdateStats(GetMyGameXP, GetMyKills, GetMyDeaths, Random.Range(10, 25));
        }

        NetManagerCheck[] netManagers = FindObjectsOfType<NetManagerCheck>();

        foreach (var manager in netManagers)
        {
            Destroy(manager.gameObject);
        }
    }
    private void PlayResultsAnimation(int current)
    {
        SetUpManager.Instance.characterAnimator.SetFloat("winStatus", current);
        SetUpManager.Instance.characterAnimator.Play("MatchResult");
    }

    public void CloseGame() => NetworkManager.Singleton.Shutdown();

    // Enter final elimination mode if it's a draw after the timer runs out
    private void EnterFinalEliminationMode()
    {
        isInFinalEliminationMode = true;
        Debug.Log("Final Elimination Mode! Next Kill Wins!");
    }

    public void Respawn(Transform player, int team)
    {
        switch (team)
        {
            case 0:
                player.position = GetAvailableSpawnPoint(SetUpManager.Instance.teamOneSpawnPoints).position;
                SetUpManager.Instance.occupier.transform.parent = GetAvailableSpawnPoint(SetUpManager.Instance.teamOneSpawnPoints);
                break;
            case 1:
                player.position = GetAvailableSpawnPoint(SetUpManager.Instance.teamTwoSpawnPoints).position;
                SetUpManager.Instance.occupier.transform.parent = GetAvailableSpawnPoint(SetUpManager.Instance.teamTwoSpawnPoints);
                break;
            default: break;
        }
    }

    public int GetMyTeam
    {
        get
        {
            PlayerManager[] players = FindObjectsOfType<PlayerManager>();

            foreach (var player in players)
            {
                if (player.GetComponent<NetworkObject>().IsOwner)
                    return player.GetTeam;
            }

            return 0;
        }
    }

    public int GetMyKills
    {
        get
        {
            PlayerManager[] players = FindObjectsOfType<PlayerManager>();

            foreach (var player in players)
            {
                if (player.GetComponent<NetworkObject>().IsOwner)
                    return player.GetKills;
            }
            return 0;
        }
    }

    public int GetMyDeaths
    {
        get
        {
            PlayerManager[] players = FindObjectsOfType<PlayerManager>();

            foreach (var player in players)
            {
                if (player.GetComponent<NetworkObject>().IsOwner)
                    return player.GetDeaths;
            }
            return 0;
        }
    }

    public int GetMyGameXP
    {
        get
        {
            PlayerManager[] players = FindObjectsOfType<PlayerManager>();

            foreach (var player in players)
            {
                if (player.GetComponent<NetworkObject>().IsOwner)
                    return player.GetXpAmount;
            }
            return 0;
        }
    }
}