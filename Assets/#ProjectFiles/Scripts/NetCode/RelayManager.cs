using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;


public class RelayManager : MonoBehaviour
{
    public static RelayManager Instance { get; set; }

    // Use this for initialization
    private void Awake() => Instance = this;

    public async Task<string> CreateRelay()
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(6);

            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            relayAllocationId = allocation.AllocationId;

            Debug.Log("Join Code: " + joinCode);

            try
            {
                RelayServerData relayServerData = new(allocation, "wss"); // dtls for PC and wss for mobile
                NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

                NetworkManager.Singleton.OnServerStarted += OnServerStarted;
                NetworkManager.Singleton.StartHost();

                LobbyManager.Instance.UpdatePlayerData("PlayerReadyState", "Ready");

                return joinCode;
            }
            catch (SocketException e)
            {
                Debug.LogError(e.Message);
                LobbyManager.Instance.ErrorOccured(e.Message);
                return null;
            }
        }
        catch (RelayServiceException e)
        {
            Debug.LogError(e.Message);
            LobbyManager.Instance.ErrorOccured(e.Message);
            return null;
        }
    }

    Guid relayAllocationId;
    public async void JoinRelay(string relayJoinCode)
    {
        try
        {
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(relayJoinCode);
            relayAllocationId = joinAllocation.AllocationId;

            try
            {
                RelayServerData relayServerData = new(joinAllocation, "wss"); // dtls for PC and wss for mobile

                NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
                NetworkManager.Singleton.StartClient();
            }
            catch (SocketException e)
            {
                Debug.LogError(e.Message);
                LobbyManager.Instance.ErrorOccured(e.Message);
            }
        }
        catch (RelayServiceException e) { Debug.LogError(e.Message); LobbyManager.Instance.ErrorOccured(e.Message); }
    }

    private void OnClientConnected(ulong clientId)
    {
        LobbyManager.Instance.UpdatePlayerData("PlayerReadyState", "Ready");
        Debug.Log("Joined Lobby");
    }

    private void OnServerStarted()
    {
        LobbyManager.Instance.UpdatePlayerData("PlayerReadyState", "Ready");
        Debug.Log("Server Started Lobby");
    }

}