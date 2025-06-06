using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;

public class NetworkAPIManager : MonoBehaviour
{
    static NetworkAPIManager instance;
    public static NetworkAPIManager Instance
    {
        get
        {
            if (!instance)
            {
                instance = new GameObject("NetworkAPIManager").AddComponent<NetworkAPIManager>();
            }
            return instance;
        }
    }

    [SerializeField] string serverUrl;
    public string sessionToken;
    public List<string> netTaskIds = new();
    [SerializeField] InputField usernameInput;
    public TMP_Text info;
    public UnityEvent OnConnectionSuccess;
    public UnityEvent OnTryConnectClosed;
    public UnityEvent OnTryConnectWalletReown;
    public UnityEvent OnWalletConnectSuccess;
    public UnityEvent OnLoggedInSuccess;
    public UnityEvent OnLoggedInFailure;
    public UserData userData;
    public Task[] tasksDone;
    public Task[] tasksNotDone;
    public PowerUp[] powerUps = new PowerUp[4]
    {
        new("magnet", 0),
        new("rapid-fire", 0),
        new("shield", 0),
        new("health", 0)
    };
    public int rank;
    [SerializeField] bool isAdmin;
    public bool isLoggedIn;
    public bool isRunningNetTask;

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);

        DontDestroyOnLoad(gameObject);

        serverUrl = "https://api.berarumble.com/";
    }

    void Start() => WalletConnect.Instance.InitWalletConnect();
    public void ShowConnectOverlay()
    {
        OnTryConnectWalletReown.Invoke();
        info.text = "Connecting to Wallet...";
        WalletConnect.Instance.OpenModal();
    }
    public void UpdateInfoText(string text) => info.text = text;

    public void WalletConnectSuccess()
    {
        info.text = $"Wallet Connected: {WalletConnect.Instance.walletAddress}";
        OnWalletConnectSuccess.Invoke();
    }

    public void ConnectWalletToNetwork()//ButtonClick Input
    {
        if (string.IsNullOrEmpty(usernameInput.text))
        {
            info.text = "Field Empty. Please enter a username...";
            Debug.LogError("Username is empty");
            OnLoggedInFailure.Invoke();
            return;
        }

        // Connect wallet to network
        SaveManager.Instance.state.userName = usernameInput.text;
        SaveManager.Instance.state.walletAddress = WalletConnect.Instance.walletAddress;
        ConnectWallet();
    }

    public void ConnectWallet()
    {
        PlayFabNetManager.Login();//{ StartCoroutine(AuthUser(SaveManager.Instance?.state.userName, SaveManager.Instance.state.walletAddress)); }
        OnIntroConnectionSuccess();
    }


    #region Auth CALLS
    public IEnumerator AuthUser(string userName, string walletAddress)
    {
        UserAuth newAuthData = new() { username = userName, referrer_id = "", wallet_address = walletAddress };
        string authDataJson = JsonUtility.ToJson(newAuthData);

        yield return new WaitForSeconds(2f);
        info.text = "Retrieving Data from Network ...";
        string authUrl = serverUrl + "auth";
        Debug.Log(authDataJson);

        UnityWebRequest www = new(authUrl, "POST");

        byte[] bodyRaw = Encoding.UTF8.GetBytes(authDataJson);
        www.uploadHandler = new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = new DownloadHandlerBuffer();

        www.SetRequestHeader("Accept", "application/json");
        www.SetRequestHeader("Content-Type", "application/json");

        www.timeout = 15;
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.Log("Error!!! " + www.downloadHandler.text);

            if (www.downloadHandler.text.Contains("username"))
            {
                info.text = "Username already exists. Please choose a different username.";
            }
            else
            {
                info.text = "Log in Failed! Please Try Again With Valid Username!";
            }

            usernameInput.text = "";
            OnLoggedInFailure.Invoke(); // Call the event
        }
        else
        {
            Debug.Log("Logged In...");
            info.text = "Logged In Successfully!";

            SessionData sessionData = JsonUtility.FromJson<SessionData>(www.downloadHandler.text);
            sessionToken = sessionData.session_token;

            Debug.Log("Logged In: " + www.downloadHandler.text);

            if (!string.IsNullOrEmpty(sessionToken))
            {
                StartCoroutine(GetUserInfo());
                StartCoroutine(GetPowerUps());
                GetTasks();
                yield return new WaitForSeconds(1f);
                OnLoggedInSuccess.Invoke();
            }
        }
    }
    public void OnIntroConnectionSuccess()
    {
        // Do something when the connection is successful
        if (SceneManager.GetActiveScene().name.Contains("Intro"))
        {
            OnConnectionSuccess.Invoke();
        }
    }
    #endregion

    #region GET CALLS
    public void GetUser()
    {
        if (isLoggedIn)
            PlayFabNetManager.GetPlayerStats();
        //StartCoroutine(GetUserInfo());
    }
    public IEnumerator GetUserInfo()
    {
        string taskId = GenerateUniqueTaskId();
        while (isRunningNetTask && netTaskIds[0] != taskId) { yield return null; }
        netTaskIds.Remove(taskId);

        UnityWebRequest www = UnityWebRequest.Get(serverUrl + "user");

        www.SetRequestHeader("Accept", "application/json");
        www.SetRequestHeader("Content-Type", "application/json");
        www.SetRequestHeader("Authorization", "Bearer " + sessionToken);

        isRunningNetTask = true;

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.Log("Error!!! " + www.downloadHandler.text);
        }
        else
        {
            Debug.Log("GetUserInfo: " + www.downloadHandler.text);
            UserData _myData = JsonUtility.FromJson<UserData>(www.downloadHandler.text);
            UpdateUserData(_myData);
            if (!isLoggedIn)
            {
                int sessions = _myData.total_sessions >= 10 ? 0 : 10 - _myData.total_sessions;
                int tokens = _myData.total_tokens == 0 ? 1000 : 0;
                GameStartUpdateStats("total_sessions", sessions);
                GameStartUpdateStats("total_tokens", tokens);
            }
            isLoggedIn = true;
        }

        isRunningNetTask = false;
    }

    public void UpdateUserData(UserData data)
    {
        userData = data;
        Debug.Log("Updated User Data: " + JsonUtility.ToJson(data));
        //SaveManager.Instance.state.userData = data;
        SaveManager.Instance.state.walletAddress = userData.wallet_address;
        SaveManager.Instance.state.userName = userData.username;
        SaveManager.Instance.state.tokens = userData.total_tokens;
        SaveManager.Instance.state.totalSessions = userData.total_sessions;
        SaveManager.Instance.state.totalXP = userData.total_xp;
        SaveManager.Instance.state.kills = userData.total_kills;
        //SaveManager.Instance.state.referralId = userData.referrer_id;
        PowerUpWrapper powerUpsWrapper = new() { powerUps = powerUps };
        //SaveManager.Instance.state.powerUpWrapper = JsonUtility.ToJson(powerUpsWrapper);
        SaveManager.Instance?.Save();
    }

    bool onFirstUpdate;
    public IEnumerator GetPowerUps()
    {
        if (!isLoggedIn) yield return null;

        string taskId = GenerateUniqueTaskId();
        while (isRunningNetTask && netTaskIds[0] != taskId) { yield return null; }
        netTaskIds.Remove(taskId);

        UnityWebRequest www = UnityWebRequest.Get(serverUrl + "user/powerups");

        www.SetRequestHeader("Accept", "application/json");
        www.SetRequestHeader("Content-Type", "application/json");
        www.SetRequestHeader("Authorization", "Bearer " + sessionToken);

        isRunningNetTask = true;

        yield return www.SendWebRequest();
        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.Log("Error!!! " + www.downloadHandler.text);
        }
        else
        {
            Debug.Log("GetPowerUps: " + www.downloadHandler.text);
            string json = $"{{\"powerUps\":{www.downloadHandler.text}}}";

            PowerUpWrapper _powerUpWData = JsonUtility.FromJson<PowerUpWrapper>(@json);
            PowerUp[] _powerUpData = _powerUpWData.powerUps;

            Debug.Log("PowerupWrapper: " + JsonUtility.ToJson(_powerUpWData));
            Debug.Log("Power Ups: " + _powerUpData);

            if (_powerUpData == null || _powerUpData.Length <= 0)
            {
                onFirstUpdate = true;
                StartCoroutine(OnUpdatePowerUp("magnet", 0));
                StartCoroutine(OnUpdatePowerUp("rapid-fire", 0));
                StartCoroutine(OnUpdatePowerUp("shield", 0));
                StartCoroutine(OnUpdatePowerUp("health", 0));
            }
            else
            {
                for (int i = 0; i < 4; i++)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        if (powerUps[j].type == _powerUpData[i].type)
                        {
                            powerUps[j] = _powerUpData[i];
                            break;
                        }
                    }
                }

                GetUser();
            }
        }

        isRunningNetTask = false;
    }

    public IEnumerator GetRank()
    {
        if (!isLoggedIn) yield return null;

        string taskId = GenerateUniqueTaskId();
        while (isRunningNetTask && netTaskIds[0] != taskId) { yield return null; }
        netTaskIds.Remove(taskId);

        UnityWebRequest www = UnityWebRequest.Get(serverUrl + "user/rank");

        www.SetRequestHeader("Accept", "application/json");
        www.SetRequestHeader("Content-Type", "application/json");
        www.SetRequestHeader("Authorization", "Bearer " + sessionToken);

        isRunningNetTask = true;

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.Log("Error!!! " + www.downloadHandler.text);
        }
        else
        {
            Debug.Log("GetRank: " + www.downloadHandler.text);
            PlayerInfo _playerData = JsonUtility.FromJson<PlayerInfo>(www.downloadHandler.text);
            rank = _playerData.position;
        }

        isRunningNetTask = false;
    }

    public void GetTasks()
    {
        if (!isLoggedIn) return;
        StartCoroutine(GetTasksDone());
        StartCoroutine(GetTasksNotDone());
        GetUser();
    }

    public IEnumerator GetTasksDone()
    {
        if (!isLoggedIn) yield return null;

        string taskId = GenerateUniqueTaskId();
        while (isRunningNetTask && netTaskIds[0] != taskId) { yield return null; }
        netTaskIds.Remove(taskId);

        UnityWebRequest www = UnityWebRequest.Get(serverUrl + "user/tasks/completed");

        www.SetRequestHeader("Accept", "application/json");
        www.SetRequestHeader("Content-Type", "application/json");
        www.SetRequestHeader("Authorization", "Bearer " + sessionToken);

        isRunningNetTask = true;

        yield return www.SendWebRequest();
        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.Log("Error!!! " + www.downloadHandler.text);
        }
        else
        {
            Debug.Log("GetTasksDone: " + www.downloadHandler.text);
            PlayerTasksData _tasksData = JsonUtility.FromJson<PlayerTasksData>(www.downloadHandler.text);
            tasksDone = _tasksData.items;
        }

        isRunningNetTask = false;
    }

    public IEnumerator GetTasksNotDone()
    {
        if (!isLoggedIn) yield return null;

        string taskId = GenerateUniqueTaskId();
        while (isRunningNetTask && netTaskIds[0] != taskId) { yield return null; }
        netTaskIds.Remove(taskId);

        UnityWebRequest www = UnityWebRequest.Get(serverUrl + "user/tasks/uncompleted");

        www.SetRequestHeader("Accept", "application/json");
        www.SetRequestHeader("Content-Type", "application/json");
        www.SetRequestHeader("Authorization", "Bearer " + sessionToken);

        isRunningNetTask = true;

        yield return www.SendWebRequest();
        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.Log("Error!!! " + www.downloadHandler.text);
        }
        else
        {
            Debug.Log("GetTasksNotDone: " + www.downloadHandler.text);
            PlayerTasksData _tasksData = JsonUtility.FromJson<PlayerTasksData>(www.downloadHandler.text);
            tasksNotDone = _tasksData.items;
            //TaskCheck.SetTasks(tasksNotDone);
        }
        isRunningNetTask = false;
    }

    public void GetLeaderboard(Leaderboard leaderboard)
    {
        if (isLoggedIn)
            PlayFabNetManager.GetLeaderboardStats(leaderboard);
    }

    public IEnumerator GetLeaderboardInfo(Leaderboard leaderboard)
    {
        if (!isLoggedIn) yield return null;

        string taskId = GenerateUniqueTaskId();
        while (isRunningNetTask && netTaskIds[0] != taskId) { yield return null; }
        netTaskIds.Remove(taskId);

        UnityWebRequest www = UnityWebRequest.Get(serverUrl + "leaderboard");

        www.SetRequestHeader("Accept", "application/json");
        www.SetRequestHeader("Content-Type", "application/json");
        www.SetRequestHeader("Authorization", "Bearer " + sessionToken);

        isRunningNetTask = true;

        yield return www.SendWebRequest();
        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.Log("Error!!! " + www.downloadHandler.text);
        }
        else
        {
            Debug.Log("LEADERBOARD: " + www.downloadHandler.text);
            LeaderboardData leaderboardData = JsonUtility.FromJson<LeaderboardData>(www.downloadHandler.text);
            if (leaderboardData.items.Count < 0) yield return null;
            else
            {
                List<PlayerDetails> boardData = leaderboardData.items;
                leaderboard.UpdateLeaderBoard(boardData);
            }
        }
        isRunningNetTask = false;
    }
    #endregion

    #region POST CALLS
    public void UpdateProfileId()
    {
        if (isLoggedIn)
            PlayFabNetManager.UpdateProfileId();
    }
    public void MarkTaskAsDone(int taskID, int amount) => StartCoroutine(OnMarkTaskAsDone(taskID, amount));
    public IEnumerator OnMarkTaskAsDone(int taskID, int amount)
    {
        if (!isLoggedIn) yield return null;

        TaskDone newTaskDone = new() { task_id = taskID, accumulated_amount = amount };
        string statsUpdateJson = JsonUtility.ToJson(newTaskDone);

        string taskId = GenerateUniqueTaskId();
        while (isRunningNetTask && netTaskIds[0] != taskId) { yield return null; }
        netTaskIds.Remove(taskId);

        UnityWebRequest www = new UnityWebRequest(serverUrl + "user/task/mark", "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(statsUpdateJson);
        www.uploadHandler = new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = new DownloadHandlerBuffer();

        www.SetRequestHeader("Accept", "application/json");
        www.SetRequestHeader("Content-Type", "application/json");
        www.SetRequestHeader("Authorization", "Bearer " + sessionToken);

        isRunningNetTask = true;

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.Log("Error!!! " + www.downloadHandler.text);
        }
        else
        {
            Debug.Log("Success MarkTaskAsDone: " + www.downloadHandler.text);
            GetTasks();
        }

        isRunningNetTask = false;
    }
    #endregion

    #region PATCH CALLS
    public void UpdateWallet(string walletAddress) { StartCoroutine(DoUpdateWallet(walletAddress)); }
    public IEnumerator DoUpdateWallet(string walletAddress)
    {
        if (!isLoggedIn) yield return null;

        string statsUpdateJson = $"{{\"wallet\":\"{walletAddress}\"}}";

        string taskId = GenerateUniqueTaskId();
        while (isRunningNetTask && netTaskIds[0] != taskId) { yield return null; }
        netTaskIds.Remove(taskId);

        UnityWebRequest www = new UnityWebRequest(serverUrl + "user/update/stats", "PATCH");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(statsUpdateJson);
        www.uploadHandler = new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = new DownloadHandlerBuffer();

        www.SetRequestHeader("Accept", "application/json");
        www.SetRequestHeader("Content-Type", "application/json");
        www.SetRequestHeader("Authorization", "Bearer " + sessionToken);

        isRunningNetTask = true;

        yield return www.SendWebRequest();
        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.Log("Error!!! " + www.downloadHandler.text);
        }
        else
        {
            Debug.Log("Success UpdateStats: " + www.downloadHandler.text);
            UserData _myData = JsonUtility.FromJson<UserData>(www.downloadHandler.text);
            UpdateUserData(_myData);
        }
        isRunningNetTask = false;
    }

    void GameStartUpdateStats(string type, int newData)
    {
        StartCoroutine(OnUpdateStats(type, newData));
    }

    public void UpdateStats(string type, int newData)
    {
        if (!isLoggedIn) return;
        StartCoroutine(OnUpdateStats(type, newData));
    }

    public IEnumerator OnUpdateStats(string type, int newData)
    {
        string statsUpdateJson = $"{{\"{type}\":{newData}}}";

        string taskId = GenerateUniqueTaskId();
        while (isRunningNetTask && netTaskIds[0] != taskId) { yield return null; }
        netTaskIds.Remove(taskId);

        UnityWebRequest www = new UnityWebRequest(serverUrl + "user/update/stats", "PATCH");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(statsUpdateJson);
        www.uploadHandler = new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = new DownloadHandlerBuffer();

        www.SetRequestHeader("Accept", "application/json");
        www.SetRequestHeader("Content-Type", "application/json");
        www.SetRequestHeader("Authorization", "Bearer " + sessionToken);

        isRunningNetTask = true;

        yield return www.SendWebRequest();
        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.Log("Error!!! " + www.downloadHandler.text);
        }
        else
        {
            Debug.Log("Success UpdateStats: " + www.downloadHandler.text);
            UserData _myData = JsonUtility.FromJson<UserData>(www.downloadHandler.text);
            GetUser();
        }
        yield return new WaitForSeconds(2f);
        isRunningNetTask = false;
    }

    public void UpdatePowerUp(string type, int newData)
    {
        if (!isLoggedIn) return;
        StartCoroutine(OnUpdatePowerUp(type, newData));
    }

    public IEnumerator OnUpdatePowerUp(string type, int newData)
    {
        string powerUpsUpdateJson = $"{{\"type\":\"{type}\", \"tier\": {newData}}}";

        string taskId = GenerateUniqueTaskId();
        while (isRunningNetTask && netTaskIds[0] != taskId) { yield return null; }
        netTaskIds.Remove(taskId);

        UnityWebRequest www = new UnityWebRequest(serverUrl + "user/update/powerups", "PATCH");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(powerUpsUpdateJson);
        www.uploadHandler = new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = new DownloadHandlerBuffer();

        www.SetRequestHeader("Accept", "application/json");
        www.SetRequestHeader("Content-Type", "application/json");
        www.SetRequestHeader("Authorization", "Bearer " + sessionToken);

        isRunningNetTask = true;

        yield return www.SendWebRequest();
        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.Log("Error!!! " + www.downloadHandler.text);
        }
        else
        {
            Debug.Log("Success UpdatePowerUp: " + www.downloadHandler.text);
            if (!onFirstUpdate) StartCoroutine(GetPowerUps());
            else
            {
                if (type == "shield") onFirstUpdate = false;
                StartCoroutine(GetPowerUps());
            }
        }
        isRunningNetTask = false;
    }

    public void DeductSuitcase(int newData)
    {
        if (!isLoggedIn) return; //Remove later
        StartCoroutine(OnDeductSuitcase(newData));
    }

    public IEnumerator OnDeductSuitcase(int newData)
    {
        Debug.Log("Deduct Suitcase: " + newData);
        string powerUpsUpdateJson = $"{{\"no_of_totalSessions\":{newData}}}";

        string taskId = GenerateUniqueTaskId();
        while (isRunningNetTask && netTaskIds[0] != taskId) { yield return null; }
        netTaskIds.Remove(taskId);

        UnityWebRequest www = new UnityWebRequest(serverUrl + "user/deduct/suitcase", "PATCH");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(powerUpsUpdateJson);
        www.uploadHandler = new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = new DownloadHandlerBuffer();

        www.SetRequestHeader("Accept", "application/json");
        www.SetRequestHeader("Content-Type", "application/json");
        www.SetRequestHeader("Authorization", "Bearer " + sessionToken);

        isRunningNetTask = true;

        yield return www.SendWebRequest();
        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.Log("Error!!! " + www.downloadHandler.text);
        }
        else
        {
            Debug.Log("Success DeductSuitcase: " + www.downloadHandler.text);
            UserData _myData = JsonUtility.FromJson<UserData>(www.downloadHandler.text);
            UpdateUserData(_myData);
            GetUser();
        }
        isRunningNetTask = false;
    }
    #endregion

    #region Runtime Task Management
    private string GenerateUniqueTaskId()
    {
        string newId;
        do
        {
            newId = GenerateRandomTaskId();
        }
        while (netTaskIds.Contains(newId));

        netTaskIds.Add(newId);
        return newId;
    }

    private string GenerateRandomTaskId()
    {
        const string charsId = "ABCDEFGHIJKLMONPQRSTUVWXYZabcdefghijklmnopqrstuvwxzy0123456789";
        StringBuilder result = new(8);
        System.Random random = new();

        for (int i = 0; i < 8; i++)
        {
            result.Append(charsId[random.Next(charsId.Length)]);
        }

        return result.ToString();
    }
    #endregion
}
