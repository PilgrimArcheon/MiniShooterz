using UnityEngine;
public class WalletConnectUI : MonoBehaviour
{
    [SerializeField] GameObject connectButton;
    [SerializeField] GameObject walletConnectedGO;
    
    void Update()
    {
        if (!NetworkAPIManager.Instance.isLoggedIn) return;

        bool walletNotConnected = string.IsNullOrEmpty(SaveManager.Instance.state.walletAddress) && string.IsNullOrEmpty(SaveManager.Instance.state.userName);
        connectButton.SetActive(walletNotConnected);
        walletConnectedGO.SetActive(!walletNotConnected);
    }
}