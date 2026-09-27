using UnityEngine;
using ArcNet;
using System;

public class HeadlessStartup : MonoBehaviour
{
    void Start()
    {
        // Check if running in BatchMode (Headless Linux Server)
        if (Application.isBatchMode)
        {
            Debug.Log("[Headless] Server Mode Detected. Initializing...");

            // Force settings for Server Mode
            ArcNetClient.Instance.settings.Mode = AuthorityMode.ServerAuthoritative;

            // Parse Command Line Arguments passed by Orchestrator
            // Example: ./MyGame.x86_64 -roomName "Match_123"
            string roomName = GetArg("-roomName");

            if (string.IsNullOrEmpty(roomName))
            {
                roomName = "Dedicated_" + Guid.NewGuid().ToString().Substring(0, 5);
            }

            // Connect and Create
            StartCoroutine(AutoHostSequence(roomName));
        }
    }

    void Update()
    {
        if (Application.isBatchMode && ArcNetClient.Instance.InRoom)
        {
            // Check if room is empty (after initial grace period)
            if (Time.time > 60f && ArcNetClient.Instance.CurrentRoom.PlayerCount <= 1)
            {
                // Only the server is left (Count == 1)
                Debug.Log("[Headless] Room Empty. Shutting down.");

                ArcNetClient.Instance.Disconnect();
                Application.Quit(); // This kills the process
            }
        }
    }

    private System.Collections.IEnumerator AutoHostSequence(string roomName)
    {
        // Connect to Relay
        ArcNetClient.Instance.ConnectToServer();

        // Wait for connection
        while (!ArcNetClient.Instance.IsConnected) yield return null;

        Debug.Log($"[Headless] Connected. Creating Room: {roomName}");

        // Create Room (Dedicated Server is ALWAYS Master Client)
        ArcNetClient.Instance.CreateRoom(roomName, new RoomOptions { maxPlayers = 10, isVisible = true });
    }

    // Helper to read command line args
    private string GetArg(string name)
    {
        var args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == name && args.Length > i + 1)
            {
                return args[i + 1];
            }
        }
        return null;
    }
}