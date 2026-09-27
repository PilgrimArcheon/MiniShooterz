using UnityEngine;
using ArcNet;

public class LobbyManager : ArcNetCallbacks
{
    [Header("UI References")]
    public GameObject connectingPanel;
    public GameObject matchMakingPanel;

    private void Start()
    {
        connectingPanel.SetActive(true);
        matchMakingPanel.SetActive(false);
        
        // Ensure ArcNet is set to ClientHosted for P2P
        ArcNetClient.Instance.settings.Mode = AuthorityMode.ClientHosted;
        ArcNetClient.Instance.ConnectToServer();
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to ArcNet Server.");
        connectingPanel.SetActive(false);
        matchMakingPanel.SetActive(true);
    }

    // Called via a UI Button
    public void FindMatch()
    {
        Debug.Log("Searching for a match...");
        RoomOptions options = new RoomOptions { maxPlayers = 6 }; // 3v3
        ArcNetClient.Instance.JoinRandomOrCreateRoom("BeraRumbleMatch", options);
    }

    public override void OnJoinedRoom(ArcNetRoom room)
    {
        Debug.Log($"Joined Room: {room.Id}. Waiting for players...");
        
        // If we are the host, we load the scene. ArcNetSceneManager will sync this to clients.
        if (ArcNetClient.Instance.IsMasterClient)
        {
            // Load your actual gameplay scene name here
            ArcNetSceneManager.Instance.LoadScene("MultiplayerMap"); 
        }
    }
}