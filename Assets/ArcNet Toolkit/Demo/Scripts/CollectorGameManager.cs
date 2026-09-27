using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

namespace ArcNet.Demo
{
    public class CollectorGameManager : ArcNetCallbacks
    {
        public static CollectorGameManager Instance;

        [Header("Settings")]
        public GameObject floor;
        public int totalBalls = 50;
        public GameObject playerPrefab;

        [Header("UI")]
        public TMPro.TextMeshProUGUI timerText;
        public TMPro.TextMeshProUGUI scoreText;
        public TMPro.TextMeshProUGUI winnerText;
        public GameObject gameOverPanel;
        private Bounds _arenaBounds;
        private bool _gameEnded = false;

        private void Awake()
        {
            Instance = this;
            if (floor != null)
            {
                var col = floor.GetComponent<Collider>();
                if (col != null) _arenaBounds = col.bounds;
            }
        }

        private void Start()
        {
            bool isServerMode = ArcNetClient.Instance.settings.Mode == AuthorityMode.ServerAuthoritative;
            bool isMaster = ArcNetClient.Instance.IsMasterClient;

            // --- PLAYER SPAWNING LOGIC ---
            if (isServerMode)
            {
                // [SERVER AUTH MODE]
                // Clients wait. Server (Headless) does NOT spawn a body for itself.
                // Spawning happens in OnPlayerEnteredRoom.
                Debug.Log($"[Server] Waiting For Players To Join");
            }
            else
            {
                // [CLIENT HOSTED MODE]
                // Every player spawns themselves immediately.
                SpawnPlayerForSelf();
            }

            // // --- MASTER LOGIC (Runs on Dedicated Server OR Host Player) ---
            // if (isMaster)
            // {
            //     StartCoroutine(SpawnBallsRoutine());
            //     double startTime = ArcNetClient.Instance.ServerTime;
            //     double endTime = startTime + 120000;
            //     ArcNetClient.Instance.SetRoomProperty("endTime", endTime);
            // }
        }

        // Called when a remote player joins
        public override void OnPlayerEnteredRoom(ArcNetPlayer newPlayer)
        {
            bool isServerMode = ArcNetClient.Instance.settings.Mode == AuthorityMode.ServerAuthoritative;

            // In Server Auth Mode, the Server must spawn the avatar for the new player
            if (isServerMode && ArcNetClient.Instance.IsMasterClient)
            {
                Debug.Log($"[Server] Spawning Avatar for Player {newPlayer.ActorNumber}");

                Vector3 spawnPos = GetRandomPosition(0f);
                ArcNetClient.Instance.Instantiate(playerPrefab.name, spawnPos, Quaternion.identity, newPlayer.ActorNumber);
            }
        }

        private void SpawnPlayerForSelf()
        {
            Vector3 spawnPos = GetRandomPosition(0f);
            ArcNetClient.Instance.Instantiate(playerPrefab.name, spawnPos, Quaternion.identity);
        }

        private Vector3 GetRandomPosition(float yHeight)
        {
            if (floor == null) return new Vector3(0, yHeight, 0);
            float x = Random.Range(_arenaBounds.min.x + 1, _arenaBounds.max.x - 1);
            float z = Random.Range(_arenaBounds.min.z + 1, _arenaBounds.max.z - 1);
            return new Vector3(x, yHeight, z);
        }

        IEnumerator SpawnBallsRoutine()
        {
            for (int i = 0; i < totalBalls; i++)
            {
                Vector3 ballPos = GetRandomPosition(0.5f);
                ArcNetClient.Instance.Instantiate("BallPrefab", ballPos, Quaternion.identity);
                yield return new WaitForSeconds(0.05f);
            }
        }

        // --- UI UPDATES ---
        private void Update()
        {
            if (ArcNetClient.Instance.CurrentRoom == null) return;

            // Timer
            if (ArcNetClient.Instance.CurrentRoom.CustomProperties.TryGetValue("endTime", out object endObj))
            {
                double endTime = System.Convert.ToDouble(endObj);
                double remaining = endTime - ArcNetClient.Instance.ServerTime;

                if (remaining > 0) timerText.text = $"Time: {(remaining / 1000.0):F1}s";
                else
                {
                    timerText.text = "Time: 0.0s";
                    if (ArcNetClient.Instance.IsMasterClient && !_gameEnded)
                        ArcNetClient.Instance.SetRoomProperty("state", "ended");
                }
            }

            // Scoreboard (ArcNetClient filters out Headless Server automatically now)
            if (scoreText != null)
            {
                scoreText.text = "<b>Scores:</b>\n";
                foreach (var kvp in ArcNetClient.Instance.CurrentRoom.Players)
                {
                    int score = kvp.Value.GetProperty<int>("score", 0);
                    scoreText.text += $"{kvp.Value.DisplayName}: {score}\n";
                }
            }

            // Game Over
            if (!_gameEnded &&
                ArcNetClient.Instance.CurrentRoom.GetProperty<string>("state") == "ended")
            {
                EndGame();
            }
        }

        private void EndGame()
        {
            _gameEnded = true;
            int highScore = -1;
            string winnerName = "";

            foreach (var kvp in ArcNetClient.Instance.CurrentRoom.Players)
            {
                int score = kvp.Value.GetProperty<int>("score", 0);
                if (score > highScore)
                {
                    highScore = score;
                    winnerName = kvp.Value.DisplayName ?? $"Player {kvp.Value.ActorNumber}";
                }
            }

            gameOverPanel.SetActive(true);
            winnerText.text = $"WINNER: {winnerName}\nScore: {highScore}";
        }

        public void LeaveRoom() => ArcNetClient.Instance.LeaveRoom();
        public override void OnLeftRoom() => SceneManager.LoadScene("Lobby");
    }
}