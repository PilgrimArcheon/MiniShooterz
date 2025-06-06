using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;
using System;

public class GameStatsUI : MonoBehaviour
{
    [Header("Team Stats Details")]
    [SerializeField] Sprite[] charImageSprites;
    [SerializeField] PlayerStatsUI[] teamOnePlayerStatsUIs;
    [SerializeField] PlayerStatsUI[] teamTwoPlayerStatsUIs;
    [SerializeField] TMP_Text[] teamTotalKills;

    [Header("Player Stats Details")]
    [SerializeField] TMP_Text playerName;
    [SerializeField] TMP_Text playerKills;
    [SerializeField] TMP_Text playerDeaths;
    [SerializeField] TMP_Text xpAmount;

    [Header("Team Stats Details")]
    [SerializeField] GameObject inGameMenuButtons;
    [SerializeField] GameObject endGameBG, endGameMenuButtons;

    void Update()
    {
        inGameMenuButtons.SetActive(!GameManager.Instance.isGameOver);
        endGameMenuButtons.SetActive(GameManager.Instance.isGameOver);
        endGameBG.SetActive(GameManager.Instance.isGameOver);

        UpdateTeamsInfo();
    }

    public void UpdateTeamsInfo()
    {
        if (GameManager.Instance == null || !GameManager.Instance.gameStarted) return;

        PlayerDetails[] myTeamDetails = GameManager.Instance.GetMyTeam == 0 ? GameManager.Instance.characterDetails[0] : GameManager.Instance.characterDetails[1];
        PlayerDetails[] oppTeamDetails = GameManager.Instance.GetMyTeam == 1 ? GameManager.Instance.characterDetails[0] : GameManager.Instance.characterDetails[1];

        int myTeamTotalKills = GameManager.Instance.GetMyTeam == 0 ? GameManager.Instance.totalTeamOneKills : GameManager.Instance.totalTeamTwoKills;
        int oppTeamTotalKills = GameManager.Instance.GetMyTeam == 1 ? GameManager.Instance.totalTeamOneKills : GameManager.Instance.totalTeamTwoKills;

        for (int i = 0; i < teamOnePlayerStatsUIs.Length; i++)
        {
            if (i < GameManager.Instance.teamSize)
            {
                teamOnePlayerStatsUIs[i].playerStatsGO.SetActive(true);
                teamOnePlayerStatsUIs[i].PlayerName.text = oppTeamDetails[i].PlayerName;
                teamOnePlayerStatsUIs[i].PlayerCharImage.sprite = charImageSprites[oppTeamDetails[i].PlayerCharId];
                teamOnePlayerStatsUIs[i].KillDeathAmount.text = $"{oppTeamDetails[i].PlayerKills}/{oppTeamDetails[i].PlayerDeaths}";
                teamOnePlayerStatsUIs[i].XPRating.text = oppTeamDetails[i].PlayerXP.ToString();
            }
            else teamOnePlayerStatsUIs[i].playerStatsGO.SetActive(false);
        }

        for (int i = 0; i < teamTwoPlayerStatsUIs.Length; i++)
        {
            if (i < GameManager.Instance.teamSize)
            {
                teamTwoPlayerStatsUIs[i].playerStatsGO.SetActive(true);
                teamTwoPlayerStatsUIs[i].PlayerName.text = myTeamDetails[i].PlayerName;
                teamTwoPlayerStatsUIs[i].PlayerCharImage.sprite = charImageSprites[myTeamDetails[i].PlayerCharId];
                teamTwoPlayerStatsUIs[i].KillDeathAmount.text = $"{myTeamDetails[i].PlayerKills}/{myTeamDetails[i].PlayerDeaths}";
                teamTwoPlayerStatsUIs[i].XPRating.text = myTeamDetails[i].PlayerXP.ToString();
            }
            else teamTwoPlayerStatsUIs[i].playerStatsGO.SetActive(false);
        }

        teamTotalKills[0].text = oppTeamTotalKills.ToString();
        teamTotalKills[1].text = myTeamTotalKills.ToString();

        playerName.text = SaveManager.Instance.state.userName;
        playerKills.text = GameManager.Instance.GetMyKills.ToString();
        playerDeaths.text = GameManager.Instance.GetMyDeaths.ToString();
        xpAmount.text = GameManager.Instance.GetMyGameXP.ToString();
    }
}

[Serializable]
public class PlayerStatsUI
{
    public GameObject playerStatsGO;
    public Image PlayerCharImage;
    public TMP_Text PlayerName;
    public TMP_Text KillDeathAmount;
    public TMP_Text XPRating;
}

[Serializable]
public class PlayerDetails
{
    public string PlayerPosition;
    public string PlayerName;
    public int PlayerCharId;
    public int PlayerId;
    public int PlayerTeam;
    public int PlayerKills;
    public int PlayerDeaths;
    public int PlayerXP;
}