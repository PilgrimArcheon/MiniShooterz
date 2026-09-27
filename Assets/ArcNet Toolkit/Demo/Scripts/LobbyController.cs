using UnityEngine;
using ArcNet;

namespace ArcNet.Demo
{
    public class LobbyController : ArcNetCallbacks
    {
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject loadingGamePanel;

        public void StartGame()
        {
            Debug.Log("Connecting to Server...");
            if (!ArcNetClient.Instance.IsConnected)
                ArcNetClient.Instance.ConnectToServer();
            else
                ArcNetClient.Instance.JoinRandomOrCreateRoom();
        }

        public void LeaveRoom()
        {
            if (ArcNetClient.Instance.InRoom) ArcNetClient.Instance.LeaveRoom();
        }

        public override void OnConnectedToMaster()
        {
            Debug.Log("Connected! Joining Random Room...");
            ArcNetClient.Instance.JoinRandomOrCreateRoom();
        }

        public override void OnJoinedRoom(ArcNetRoom room)
        {
            Debug.Log($"Joined Room: {room.Id}. Waiting for Scene Sync...");
            loadingGamePanel.SetActive(true);
            mainPanel.SetActive(false);
        }

        public override void OnPlayerEnteredRoom(ArcNetPlayer newPlayer)
        {
            base.OnPlayerEnteredRoom(newPlayer);
            
            // NOTE: If this is Client-Hosted mode (no dedicated server), 
            // the Master Client needs to trigger the first scene load.
            if (ArcNetClient.Instance.IsMasterClient && ArcNetClient.Instance.settings.Mode == AuthorityMode.ClientHosted)
            {
                var props = new System.Collections.Generic.Dictionary<string, object>
                {
                    { "time", 120 },
                    { "state", "playing" }
                };
                ArcNetClient.Instance.SetRoomProperties(props);
                ArcNetSceneManager.Instance.LoadScene("GameScene");
            }
        }

        public override void OnLeftRoom()
        {
            loadingGamePanel.SetActive(false);
            mainPanel.SetActive(true);
        }
    }
}