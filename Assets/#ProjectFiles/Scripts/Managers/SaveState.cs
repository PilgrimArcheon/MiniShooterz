[System.Serializable]
public class SaveState
{
    //PlayerData
    public string userName;
    public int totalXP;
    public int totalSessions = 5;
    public int tokens = 1000;
    public int kills = 0;
    public int deaths = 0;
    public int charId;

    public UserData userData;
    public bool hasConnectedCred;

    public string walletAddress;

    // User Network Data
    public string initData;
    public string startParam;
    //Settings
    public float[] volumeSettings = new float[2] { 0.5f, 0.5f };
    public bool soundOn = true;
    public bool musicOn = true;
}