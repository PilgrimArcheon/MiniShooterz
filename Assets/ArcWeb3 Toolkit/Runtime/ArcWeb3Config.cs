using UnityEngine;

namespace ArcWeb3
{
    [CreateAssetMenu(fileName = "ArcWeb3Config", menuName = "ArcWeb3/Network Config")]
    public class ArcWeb3Config : ScriptableObject
    {
        [Header("Network Details")]
        public string chainIdHex = ""; // Default: Abstract Testnet (11124)
        public string chainName = ""; // "Abstract Testnet";

        [Header("Native Currency")]
        public string currencyName = "ETH";
        public string currencySymbol = "ETH";
        public int currencyDecimals = 18;

        [Header("Endpoints")]
        public string rpcUrl = "https://api.network_link.abc"; 
        public string blockExplorerUrl = "https://explorer.network_link";

        [Header("Smart Contract Details")]
        public string contractAddress = "0xYourContractAddressHere";
        [TextArea(5, 10)]
        public string contractABI = @"[]";
    }
} 