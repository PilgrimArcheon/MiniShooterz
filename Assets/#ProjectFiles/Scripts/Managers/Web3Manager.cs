using UnityEngine;
using TMPro;
using ArcWeb3;
using System;
using UnityEngine.Events; // Use the new toolkit namespace

public class Web3Manager : MonoBehaviour
{
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private UnityEvent onConnected; // New UnityEvent for connection success

    private void OnEnable()
    {
        // Subscribe to the ArcWeb3 global events
        ArcWeb3Manager.OnWalletConnected += HandleWalletConnected;
        ArcWeb3Manager.OnReadComplete += HandleOwnershipCheck;
        ArcWeb3Manager.OnError += HandleError;
    }

    private void OnDisable()
    {
        // Unsubscribe to prevent memory leaks if this object is destroyed
        ArcWeb3Manager.OnWalletConnected -= HandleWalletConnected;
        ArcWeb3Manager.OnReadComplete -= HandleOwnershipCheck;
        ArcWeb3Manager.OnError -= HandleError;
    }

    void Start()
    {
        statusText.text = "Please connect your wallet to continue.";
        Connect();
    }

    public void Connect()
    {
        ArcWeb3Manager.Instance.ConnectWallet();
    }

    private void HandleWalletConnected(string address)
    {
        Debug.Log("Connected: " + address);
        statusText.text = "Connected: " + address;
        onConnected?.Invoke();
    }

    private void HandleOwnershipCheck(string method, string result)
    {
        // Ensure we are responding to the specific 'balanceOf' call
        if (method == "balanceOf")
        {
            if (int.TryParse(result, out int balance) && balance > 0)
            {
                statusText.text = "Ownership verified!";
                Debug.Log("Ownership verified!");
            }
            else
            {
                statusText.text = "No Hatchlings found. Access denied.";
                Debug.Log("No Hatchlings found. Access denied.");
            }
        }
    }

    private void HandleError(string errorMsg)
    {
        statusText.text = "Error: " + errorMsg;
        Debug.Log("Error: " + errorMsg);    
    }
}