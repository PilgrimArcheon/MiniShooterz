using System;
using Unity.Netcode;

[Serializable]
public class PlayerDetails
{
    public ulong ClientId;
    public string PlayerPosition;
    public string PlayerName;
    public int PlayerCharId;
    public int PlayerId;
    public int PlayerTeam;
    public int PlayerKills;
    public int PlayerDeaths;
    public int PlayerXP;
}