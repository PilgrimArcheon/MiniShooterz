using UnityEngine;
using System;
using System.Runtime.InteropServices;

namespace ArcWeb3
{
    public class ArcWeb3Manager : MonoBehaviour
    {
        public static ArcWeb3Manager Instance { get; private set; }

        [Header("Configuration")]
        public ArcWeb3Config activeConfig;

        // --- PUBLIC EVENTS FOR YOUR GAME SCRIPTS ---
        public static event Action<string> OnWalletConnected;
        public static event Action<string> OnError;
        public static event Action<string, string> OnReadComplete;     // (MethodName, Result)
        public static event Action<string, string> OnTransactionSuccess; // (MethodName, TxHash)

        public string CurrentAddress { get; private set; }

        // --- JS INTEROP ---
        [DllImport("__Internal")] private static extern void InitArcWeb3JS(string configJson);
        [DllImport("__Internal")] private static extern void ConnectArcWalletJS();
        [DllImport("__Internal")] private static extern void ArcGenericReadJS(string contract, string abi, string method, string args);
        [DllImport("__Internal")] private static extern void ArcGenericWriteJS(string contract, string abi, string method, string args, string value);

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeBridge();
            }
            else Destroy(gameObject);
        }

        private void InitializeBridge()
        {
            if (activeConfig == null)
            {
                Debug.LogError("[ArcWeb3] Fatal: No ArcWeb3Config assigned!");
                return;
            }

            string jsonConfig = JsonUtility.ToJson(activeConfig);
#if !UNITY_EDITOR && UNITY_WEBGL
            InitArcWeb3JS(jsonConfig);
#else
            Debug.Log("[ArcWeb3] Editor Mode: Bridge initialized with Mock Data ready.");
#endif
        }

        // --- API METHODS ---
        public void ConnectWallet()
        {
#if !UNITY_EDITOR && UNITY_WEBGL
            ConnectArcWalletJS();
#else
            // Mock login for Editor
            JSCallback_WalletConnected("0xMockAddress123456789");
#endif
        }

        public void ContractRead(string contractAddress, string abiSignature, string methodName, string commaSeparatedArgs = "")
        {
#if !UNITY_EDITOR && UNITY_WEBGL
            ArcGenericReadJS(contractAddress, abiSignature, methodName, commaSeparatedArgs);
#else
            Debug.Log($"[ArcWeb3 Mock] Read {methodName} called.");
#endif
        }

        public void ContractWrite(string contractAddress, string abiSignature, string methodName, string commaSeparatedArgs = "", string ethValue = "0")
        {
#if !UNITY_EDITOR && UNITY_WEBGL
            ArcGenericWriteJS(contractAddress, abiSignature, methodName, commaSeparatedArgs, ethValue);
#else
            Debug.Log($"[ArcWeb3 Mock] Write {methodName} called.");
#endif
        }

        // --- TOOLKIT HELPERS (Specific Wrappers) ---
        public void GetNFTBalance(string contractAddress, string walletAddress)
        {
            ContractRead(contractAddress, "function balanceOf(address) view returns (uint256)", "balanceOf", walletAddress);
        }

        // --- CALLBACKS FROM JAVASCRIPT ---
        // Warning: Do not rename these without updating the .jslib SendMessage calls!
        public void JSCallback_WalletConnected(string address)
        {
            CurrentAddress = address;
            OnWalletConnected?.Invoke(address);
        }

        public void JSCallback_Error(string errorMsg)
        {
            Debug.LogError($"[ArcWeb3 Bridge Error] {errorMsg}");
            OnError?.Invoke(errorMsg);
        }

        public void JSCallback_ReadResult(string payload)
        {
            // Payload format: "methodName|resultString"
            string[] split = payload.Split(new char[] { '|' }, 2);
            if(split.Length == 2) OnReadComplete?.Invoke(split[0], split[1]);
        }

        public void JSCallback_TxResult(string payload)
        {
            // Payload format: "methodName|txHash"
            string[] split = payload.Split(new char[] { '|' }, 2);
            if (split.Length == 2) OnTransactionSuccess?.Invoke(split[0], split[1]);
        }
    }
}