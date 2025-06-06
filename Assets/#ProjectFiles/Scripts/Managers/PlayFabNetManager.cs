using PlayFab;
using PlayFab.ClientModels;
using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayFabNetManager : MonoBehaviour
{
    public static string PlayFabId { get; private set; }

    #region Auth
    public static void Login()
    {
        if (string.IsNullOrEmpty(SaveManager.Instance.state.walletAddress)) return;

        var request = new LoginWithCustomIDRequest
        {
            CustomId = SaveManager.Instance.state.walletAddress,
            CreateAccount = true
        };

        PlayFabClientAPI.LoginWithCustomID(request, OnLoginSuccess, OnLoginFailed);
    }

    static void OnLoginSuccess(LoginResult result)
    {
        PlayFabId = result.PlayFabId;
        Debug.Log("PlayFab login successful. ID: " + PlayFabId);

        NetworkAPIManager.Instance.OnLoggedInSuccess.AddListener(() => NetworkAPIManager.Instance.isLoggedIn = true);
        NetworkAPIManager.Instance.OnLoggedInSuccess.Invoke();
        GetPlayerStats();
    }

    static void OnLoginFailed(PlayFabError error)
    {
        Debug.LogError("PlayFab login failed: " + error.GenerateErrorReport());
        NetworkAPIManager.Instance.OnLoggedInFailure.Invoke();
    }
    #endregion

    #region Stats
    public static void UpdateStats(int xp, int kills, int deaths, int tokens)
    {
        SaveManager.Instance.state.totalXP += xp;
        SaveManager.Instance.state.kills += kills;
        SaveManager.Instance.state.deaths += deaths;
        SaveManager.Instance.state.tokens += tokens;

        var stats = new List<StatisticUpdate>
        {
            new() { StatisticName = "XP", Value = SaveManager.Instance.state.totalXP },
            new() { StatisticName = "Kills", Value = SaveManager.Instance.state.kills },
            new() { StatisticName = "Deaths", Value = SaveManager.Instance.state.deaths },
            new() { StatisticName = "Tokens", Value = SaveManager.Instance.state.tokens }
        };

        PlayFabClientAPI.UpdatePlayerStatistics(new UpdatePlayerStatisticsRequest
        {
            Statistics = stats
        },
        result => Debug.Log("Stats updated"),
        error => Debug.LogError("Failed to update stats: " + error.GenerateErrorReport()));
    }

    public static void UpdateProfileId()
    {
        var stats = new List<StatisticUpdate>
        {
            new() { StatisticName = "CharId", Value = SaveManager.Instance.state.charId },
        };

        PlayFabClientAPI.UpdatePlayerStatistics(new UpdatePlayerStatisticsRequest
        {
            Statistics = stats
        },
        result => Debug.Log("Stats updated"),
        error => Debug.LogError("Failed to update stats: " + error.GenerateErrorReport()));
    }

    public static void GetPlayerStats()
    {
        GetStats(
            stats =>
            {
                int xp = stats.ContainsKey("XP") ? stats["XP"] : 0;
                int kills = stats.ContainsKey("Kills") ? stats["Kills"] : 0;
                int deaths = stats.ContainsKey("Deaths") ? stats["Deaths"] : 0;
                int tokens = stats.ContainsKey("Tokens") ? stats["Tokens"] : 0;
                int charId = stats.ContainsKey("CharId") ? stats["CharId"] : 0;
                Debug.Log("XP: " + xp + ", Tokens: " + tokens);

                SaveManager.Instance.state.totalXP = xp;
                SaveManager.Instance.state.kills = kills;
                SaveManager.Instance.state.deaths = deaths;
                SaveManager.Instance.state.tokens = tokens;
                SaveManager.Instance.state.charId = charId;
                SaveManager.Instance.Save();
            },
            error =>
            {
                Debug.LogError("Error fetching stats: " + error.GenerateErrorReport());
            });
    }

    static void GetStats(Action<Dictionary<string, int>> onSuccess, Action<PlayFabError> onError = null)
    {
        PlayFabClientAPI.GetPlayerStatistics(new GetPlayerStatisticsRequest(),
            result =>
            {
                Dictionary<string, int> stats = new();
                foreach (var stat in result.Statistics)
                {
                    stats[stat.StatisticName] = stat.Value;
                    Debug.Log($"Stat: {stat.StatisticName} = {stat.Value}");
                }

                onSuccess?.Invoke(stats);
            },
            error =>
            {
                Debug.LogError("Failed to get player stats: " + error.GenerateErrorReport());
                onError?.Invoke(error);
            }
        );
    }

    public static void GetLeaderboard()
    {
        PlayFabClientAPI.GetLeaderboard(new GetLeaderboardRequest
        {
            StatisticName = "XP",
            StartPosition = 0,
            MaxResultsCount = 10
        },
        OnLeaderboardReceived,
        error => Debug.LogError("Leaderboard error: " + error.GenerateErrorReport()));
    }

    static void OnLeaderboardReceived(GetLeaderboardResult result)
    {
        foreach (var entry in result.Leaderboard)
        {
            Debug.Log($"{entry.Position + 1}: {entry.DisplayName ?? entry.PlayFabId} - {entry.StatValue}");
        }
    }

    static int statsCount;
    static Leaderboard leaderboard;
    public static void GetLeaderboardStats(Leaderboard _leaderboard)
    {
        switch (statsCount)
        {
            case 0:
                GetCenteredLeaderboard("XP");
                break;
            case 1:
                GetCenteredLeaderboard("Kills");
                break;
            case 2:
                GetCenteredLeaderboard("Deaths");
                break;
            case 3:
                GetCenteredLeaderboard("CharId");
                break;
            case 4:
                GetCenteredLeaderboard("Name");
                break;
            default: break;
        }

        leaderboard = _leaderboard;
    }

    static string statsType;
    static void GetCenteredLeaderboard(string _statsType)
    {
        statsType = _statsType;
        PlayFabClientAPI.GetLeaderboardAroundPlayer(new GetLeaderboardAroundPlayerRequest
        {
            StatisticName = statsType,
            MaxResultsCount = 5
        },
        OnCenteredLeaderboardReceived,
        error => Debug.LogError("Leaderboard error: " + error.GenerateErrorReport()));
    }

    static List<int> position = new();
    static List<string> displayName = new();
    static List<int> xpAmount = new();
    static List<int> kills = new();
    static List<int> deaths = new();
    static List<int> charId = new();
    private static void OnCenteredLeaderboardReceived(GetLeaderboardAroundPlayerResult result)
    {
        foreach (var entry in result.Leaderboard)
        {
            switch (statsType)
            {
                case "XP":
                    xpAmount.Add(entry.StatValue);
                    break;
                case "Kills":
                    kills.Add(entry.StatValue);
                    break;
                case "Deaths":
                    deaths.Add(entry.StatValue);
                    break;
                case "CharId":
                    charId.Add(entry.StatValue);
                    break;
                case "Name":
                    displayName.Add(entry.DisplayName);
                    position.Add(entry.Position);
                    break;
                default: break;
            }
        }
        statsCount++;

        if (statsCount < 5)
            GetLeaderboardStats(leaderboard);

        if (statsCount == 5) ShowLeaderboard();
    }

    static List<PlayerDetails> playerDetails = new();
    static void ShowLeaderboard()
    {
        for (int i = 0; i < displayName.Count; i++)
        {
            PlayerDetails player = new()
            {
                PlayerName = displayName[i],
                PlayerKills = kills[i],
                PlayerDeaths = deaths[i],
                PlayerXP = xpAmount[i]
            };

            playerDetails.Add(player);
            leaderboard.UpdateLeaderBoard(playerDetails);
        }

        statsCount = 0;

        displayName.Clear();
        xpAmount.Clear();
        kills.Clear();
        deaths.Clear();
    }
    #endregion
}
