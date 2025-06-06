using UnityEngine;
using UnityEngine.UI;

public class MobileWalletConnect : MonoBehaviour
{
    public InputField walletAddressText;
    public Button button; 

    public void ConnectToWallet()
    {
        NetworkAPIManager.Instance.UpdateInfoText("Connecting Wallet...");
        string walletAddress = walletAddressText.text;
        if (string.IsNullOrEmpty(walletAddress))
            NetworkAPIManager.Instance.UpdateInfoText("Field Empty!!! Please Add Wallet Address.");
        else
            WalletConnect.Instance.CheckConnectWalletWithAddress(walletAddress);
        // Connect to the mobile wallet
    }
}