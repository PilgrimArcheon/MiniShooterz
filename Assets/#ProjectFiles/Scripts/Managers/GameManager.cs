using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour
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
    public bool IsMultiplayer;

    void Awake()
    {
        Instance = this;

        timer = gameTimeLimit;
    }

    void Start()
    {
        CreateCharacterDetailsSlots();
        if (!IsMultiplayer) SpawnTeams();
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

        // Spawn the Red Team
        for (int i = 0; i < teamSize; i++)
        {
            if (i <= (playersForTeamOne - 1))  // Spawn the player in the red team
            {
                PlayerCharacterController player = Instantiate(SetUpManager.Instance.playerPrefab, GetAvailableSpawnPoint(SetUpManager.Instance.teamOneSpawnPoints).position, GetAvailableSpawnPoint(SetUpManager.Instance.teamOneSpawnPoints).rotation).GetComponent<PlayerCharacterController>();
                SetUpManager.Instance.occupier.transform.parent = GetAvailableSpawnPoint(SetUpManager.Instance.teamOneSpawnPoints);
                player.CharacterUserSetUp("Player_" + Random.Range(100, 999), SaveManager.Instance.state.charId, 0, i);
                player.name += $" Team-1 {i + 1}";
                playerTeam = 0;
                teamOne.Add(player.gameObject);
            }
            else  // Spawn an AI for the rest of the red team
            {
                AICharacterController ai = Instantiate(SetUpManager.Instance.aiPrefab, GetAvailableSpawnPoint(SetUpManager.Instance.teamOneSpawnPoints).position, GetAvailableSpawnPoint(SetUpManager.Instance.teamOneSpawnPoints).rotation).GetComponent<AICharacterController>();
                SetUpManager.Instance.occupier.transform.parent = GetAvailableSpawnPoint(SetUpManager.Instance.teamOneSpawnPoints);
                ai.name += $" Team-1 {i + 1}";
                ai.CharacterUserSetUp("Bot " + Random.Range(100, 999), Random.Range(0, characterSetUpLength), 0, i);
                teamOne.Add(ai.gameObject);
            }
        }

        // Spawn the Blue Team
        for (int i = 0; i < teamSize; i++)
        {
            if (i <= (playersForTeamTwo - 1))  // Spawn the player in the blue team
            {
                PlayerCharacterController player = Instantiate(SetUpManager.Instance.playerPrefab, GetAvailableSpawnPoint(SetUpManager.Instance.teamTwoSpawnPoints).position, GetAvailableSpawnPoint(SetUpManager.Instance.teamTwoSpawnPoints).rotation).GetComponent<PlayerCharacterController>();
                SetUpManager.Instance.occupier.transform.parent = GetAvailableSpawnPoint(SetUpManager.Instance.teamTwoSpawnPoints);
                player.CharacterUserSetUp("Player_" + Random.Range(100, 999), SaveManager.Instance.state.charId, 1, i);
                player.name += $" Team-2 {i + 1}";
                playerTeam = 1;
                teamTwo.Add(player.gameObject);
            }
            else  // Spawn an AI for the rest of the blue team
            {
                AICharacterController ai = Instantiate(SetUpManager.Instance.aiPrefab, GetAvailableSpawnPoint(SetUpManager.Instance.teamTwoSpawnPoints).position, GetAvailableSpawnPoint(SetUpManager.Instance.teamTwoSpawnPoints).rotation).GetComponent<AICharacterController>();
                SetUpManager.Instance.occupier.transform.parent = GetAvailableSpawnPoint(SetUpManager.Instance.teamTwoSpawnPoints);
                ai.name += $" Team-2 {i + 1}";
                ai.CharacterUserSetUp("Bot " + Random.Range(100, 999), Random.Range(0, characterSetUpLength), 1, i);
                teamTwo.Add(ai.gameObject);
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

    public void RegisterKill(int team, int playerId)
    {
        characterDetails[team][playerId].PlayerKills++;
        // Check for the end of the game based on kills
        if ((totalTeamTwoKills >= maxKills || totalTeamTwoKills >= maxKills) && !isGameOver) EndGame();
    }

    public void RegisterDeath(int team, int playerId)
    {
        characterDetails[team][playerId].PlayerDeaths++;
    }

    public void RegisterXP(int team, int playerId, int _xpAmount)
    {
        characterDetails[team][playerId].PlayerXP += _xpAmount;
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
    }
    private void PlayResultsAnimation(int current)
    {
        SetUpManager.Instance.characterAnimator.SetFloat("winStatus", current);
        SetUpManager.Instance.characterAnimator.Play("MatchResult");
    }


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

    public int GetPlayerId
    {
        get
        {
            // Loop through all the players
            // Get which of them has a PhotonView that has the IsMine tag
            // Get the playerId on it and return that value
            return 0;
        }
    }

    public int GetMyTeam
    {
        get
        {
            // Loop through all the players
            // Get which of them has a PhotonView that has the IsMine tag
            // Get the team index on it and return that value
            return 0;
        }
    }

    public int GetMyKills => characterDetails[GetMyTeam][GetPlayerId].PlayerKills;

    public int GetMyDeaths => characterDetails[GetMyTeam][GetPlayerId].PlayerDeaths;

    public int GetMyGameXP => characterDetails[GetMyTeam][GetPlayerId].PlayerXP;
}