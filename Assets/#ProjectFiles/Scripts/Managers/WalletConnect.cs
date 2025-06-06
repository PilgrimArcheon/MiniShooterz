using System.Collections;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Reown.AppKit.Unity;
using Reown.Sign.Models;
using UnityEngine;
using UnityEngine.Networking;

public class WalletConnect : MonoBehaviour
{
    [DllImport("__Internal")]
    private static extern bool IsMobile();
    static WalletConnect instance;
    public static WalletConnect Instance
    {
        get
        {
            if (!instance)
            {
                instance = new GameObject("WalletConnect").AddComponent<WalletConnect>();
            }
            return instance;
        }
    }
    readonly string etherscancCheck = "ZWDMARQKWA7VK3AMP6TIMDB6KHCH1Y7FM9";

    bool isAdminMobile;
    bool mobile;

    void Awake()
    {
        instance = this;
        DontDestroyOnLoad(gameObject);
    }
    public string walletAddress = ""; //0xbfB184FBBFE3a678ca53440a7e5d2c30e2D42713
    public void InitWalletConnect()
    {
        isAdminMobile = true;
        mobile = isAdminMobile || IsWebMobile();

        // Initialize WalletConnect
        AppKitInitConfig();
    }

    public void ConnectWallet()
    {
        NetworkAPIManager.Instance.ShowConnectOverlay();
        // Do Connect Wallet Logic Here 
    }

    public void OpenModal() => OpenAppKitModal();
    MobileWalletConnect mobileConnect;
    async void OpenAppKitModal()
    {
        if (mobile)
        {
            mobileConnect = Instantiate(Resources.Load<GameObject>("MobileWalletConnect")).GetComponent<MobileWalletConnect>();
        }
        else
        {
            if (AppKit.IsAccountConnected) await AppKit.DisconnectAsync();
            AppKit.OpenModal(ViewType.WalletSearch);
        }
    }

    public void CheckConnectWalletWithAddress(string wAddress)
    {
        StartCoroutine(CheckWalletCoroutine(wAddress));
    }

    IEnumerator CheckWalletCoroutine(string walletAddress)
    {
        string url = $"https://api.etherscan.io/api?module=account&action=balance&address={walletAddress}&tag=latest&apikey={etherscancCheck}";

        using UnityWebRequest request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError("API Error: " + request.error);
        }
        else
        {
            string json = request.downloadHandler.text;
            Debug.Log("API Response: " + json);

            if (json.Contains("\"status\":\"1\""))
            {
                Debug.Log("✅ Wallet address exists or has activity.");
                ConnectWalletAccount(new Account(walletAddress, "1"));
                Destroy(mobileConnect.gameObject);
            }
            else
            {
                Debug.Log("❌ Wallet address is likely unused or invalid.");
            }
        }
    }

    async void AppKitInitConfig()
    {
        var config = new AppKitConfig(
            projectId: "fc522c7f6c26399b394ef4448483d80c",
            new Metadata(
                name: "BeraRumble",
                description: "This is the Bera Rumble app",
                url: "https://berarumble.com",
                iconUrl: "https://example.com/logo.png"
        ));

        await AppKit.InitializeAsync(config);

        Debug.Log("Network Init!!!");

        if (AppKit.IsAccountConnected) await AppKit.DisconnectAsync();

        AppKit.ModalController.OpenStateChanged += OnModalControllerOpenStateChanged;
        string wAddress = SaveManager.Instance.state.walletAddress;
        string uName = SaveManager.Instance.state.userName;

        if (!string.IsNullOrEmpty(wAddress) && !string.IsNullOrEmpty(uName))
        {
            ConnectWalletAccount(new Account(wAddress, "1"));
        }
        else
        {
            NetworkAPIManager.Instance.ConnectWallet();

            if (mobile) return;

            AppKit.AccountConnected += (sender, eventArgs) =>
            {
                Task<Account> activeAccount = eventArgs.GetAccountAsync();

                activeAccount.ContinueWith(task =>
                {
                    if (task.IsCanceled) NetworkAPIManager.Instance.OnTryConnectClosed.Invoke();

                    if (task.IsCompleted) ConnectWalletAccount(task.Result);
                });
            };
        }
    }
    private void ConnectWalletAccount(Account account)
    {
        walletAddress = account.Address;
        Debug.Log("WalletConnected: " + walletAddress);

        if (string.IsNullOrEmpty(SaveManager.Instance.state.walletAddress))
        {
            if (!string.IsNullOrEmpty(walletAddress))
                NetworkAPIManager.Instance.WalletConnectSuccess();
            else AppKit.OpenModal(ViewType.WalletSearch);
        }
        else NetworkAPIManager.Instance.ConnectWallet();
    }
    private void OnModalControllerOpenStateChanged(object sender, ModalOpenStateChangedEventArgs e)
    {
        if (!e.IsOpen) NetworkAPIManager.Instance.OnTryConnectClosed.Invoke();
    }

    public bool IsWebMobile()
    {
#if !UNITY_EDITOR && UNITY_WEBGL
            return IsMobile();
#endif
        return false;
    }
}