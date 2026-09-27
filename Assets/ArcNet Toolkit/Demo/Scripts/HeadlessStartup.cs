using UnityEngine;
using ArcNet;
using UnityEngine.SceneManagement;

namespace ArcNet.Demo
{
    public class HeadlessStartup : MonoBehaviour
    {
        void Start()
        {
            if (Application.isBatchMode)
            {
                Debug.Log("--- STARTING DEDICATED SERVER ---");
                ArcNetClient.Instance.UserId = "ARCNET_SERVER_" + Random.Range(1000, 9999);
                ArcNetClient.Instance.settings.Mode = AuthorityMode.ServerAuthoritative;
                ArcNetClient.Instance.settings.IsLocal = true; // Or false if deploying

                ArcNetClient.Instance.ConnectToServer();
                ArcNetClient.OnConnectedToMaster += OnConnected;
                ArcNetClient.OnJoinedRoom += JoinedRoom;
            }
            else SceneManager.LoadScene("Lobby");
        }

        private void OnConnected()
        {
            Debug.Log("Connected! Creating Room 'Match' ....");
            ArcNetClient.Instance.CreateRoom($"Match_{Random.Range(100, 999)}");
        }

        private void JoinedRoom(ArcNetRoom room)
        {
            Debug.Log($"Created to {room.Id}");
            // Initialize Game State
            var props = new System.Collections.Generic.Dictionary<string, object>()
            {
                { "time", 120 },
                { "state", "playing" }
            };
            
            ArcNetClient.Instance.SetRoomProperties(props);

            // THIS is what pulls clients into the game:
            ArcNetSceneManager.Instance.LoadScene("GameScene");
        }
    }
}