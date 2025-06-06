using System;
using System.Collections.Generic;

[Serializable]
public class UserData
{
    public string id;
    public string username;
    public int total_kills;
    public int total_sessions;
    public string wallet_address;
    public int total_tokens;
    public int total_xp;
    public int onchain_shares;
    public string referrer_id;
    public bool banned;
    public string created_at;
}

[Serializable]
public class UserAuth
{
    public string username;
    public string referrer_id;
    public string wallet_address;
}

[Serializable]
public class ReferralLinks
{
    public string facebook;
    public string X;
    public string discord;
}
[Serializable]
public class PowerUpWrapper
{
    public PowerUp[] powerUps;
}

[Serializable]
public class PowerUp
{
    public int id;
    public int user_id;
    public string type;
    public int tier;

    public PowerUp(string type, int tier)
    {
        this.type = type;
        this.tier = tier;
    }
}

[Serializable]
public class PlayerInfo
{
    public int position;
    public int total_score;
    public int total_tokens;
    public string username;
}

[Serializable] public class TelegramData { public string initData; }
[Serializable] public class SessionData { public string message; public string session_token; }
[Serializable]
public class LeaderboardData
{
    public List<PlayerDetails> items;
    public int total_pages;
    public int current_page;
    public int last_page;
}

[Serializable]
public class ScoreboardData
{
    public List<PlayerInfo> items;
    public int total_pages;
    public int current_page;
    public int last_page;
}

[Serializable]
public class TokenboardData
{
    public List<PlayerInfo> items;
    public int total_pages;
    public int current_page;
    public int last_page;
}

[Serializable]
public class TaskDone
{
    public int task_id;
    public int accumulated_amount;
}

[Serializable]
public class Task
{
    public int id;
    public string reference_id;
    public string user_id;
    public string type;
    public string title;
    public string description;
    public string duration;
    public int accumulated_amount;
    public int required_amount;
    public string reward_type;
    public int reward_amount;
    public bool done;
}

[Serializable]
public class PlayerTasksData
{
    public Task[] items;
    public int total_pages;
    public int current_page;
    public int last_page;
}