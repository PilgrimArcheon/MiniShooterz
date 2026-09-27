using UnityEngine;

namespace ArcNet
{
    public enum AuthorityMode
    {
        ClientHosted,       // Standard (Among Us style) - Owner controls position
        ServerAuthoritative // Competitive (CS:GO/Fortnite) - Server controls position via Inputs
    }

    [CreateAssetMenu(fileName = "ArcNetSettings", menuName = "ArcNet/Settings Asset")]
    public class ArcNetSettings : ScriptableObject
    {
        [Header("General")]
        public string AppVersion = "1.0";
        public AuthorityMode Mode = AuthorityMode.ClientHosted; // Select Default Here
        
        [Header("Connection")]
        public bool IsLocal = true;
        public string LocalUrl = "ws://localhost:8080";
        public string CloudUrl = "wss://your-app.onrender.com"; // Your Node.js Relay

        [Header("Performance")]
        public float SerializationRate = 0.1f;

        public string GetTargetUrl() => IsLocal ? LocalUrl : CloudUrl;
    }
}