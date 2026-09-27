using UnityEngine;
using System.Collections.Generic;

namespace ArcNet
{
    public class ArcNetCallbacks : MonoBehaviour
    {
        protected virtual void OnEnable()
        {
            ArcNetClient.OnConnectedToMaster += OnConnectedToMaster;
            ArcNetClient.OnDisconnected += OnDisconnected;
            ArcNetClient.OnOperationFailed += OnOperationFailed;

            ArcNetClient.OnJoinedRoom += OnJoinedRoom;
            ArcNetClient.OnLeftRoom += OnLeftRoom;
            ArcNetClient.OnRoomListUpdate += OnRoomListUpdate;

            ArcNetClient.OnPlayerEnteredRoom += OnPlayerEnteredRoom;
            ArcNetClient.OnPlayerLeftRoom += OnPlayerLeftRoom;
            ArcNetClient.OnMasterClientSwitched += OnMasterClientSwitched;
            ArcNetClient.OnRoomPropertiesUpdate += OnRoomPropertiesUpdate;
        }

        protected virtual void OnDisable()
        {
            ArcNetClient.OnConnectedToMaster -= OnConnectedToMaster;
            ArcNetClient.OnDisconnected -= OnDisconnected;
            ArcNetClient.OnOperationFailed -= OnOperationFailed;

            ArcNetClient.OnJoinedRoom -= OnJoinedRoom;
            ArcNetClient.OnLeftRoom -= OnLeftRoom;
            ArcNetClient.OnRoomListUpdate -= OnRoomListUpdate;

            ArcNetClient.OnPlayerEnteredRoom -= OnPlayerEnteredRoom;
            ArcNetClient.OnPlayerLeftRoom -= OnPlayerLeftRoom;
            ArcNetClient.OnMasterClientSwitched -= OnMasterClientSwitched;
            ArcNetClient.OnRoomPropertiesUpdate -= OnRoomPropertiesUpdate;
        }

        // ---------------------------------------------------
        // Virtual Methods (Override these in your Game Manager)
        // ---------------------------------------------------

        public virtual void OnConnectedToMaster() { }

        public virtual void OnDisconnected() { }

        public virtual void OnOperationFailed(int opCode, string message)
        {
            Debug.LogError($"[ArcNet] Error {opCode}: {message}");
        }

        public virtual void OnJoinedRoom(ArcNetRoom room) { }

        public virtual void OnLeftRoom() { }

        public virtual void OnRoomListUpdate(List<RoomInfo> roomList) { }

        /// <summary>
        /// A remote player entered the room.
        /// </summary>
        /// <param name="newPlayer">The full player object, including Name and Properties.</param>
        public virtual void OnPlayerEnteredRoom(ArcNetPlayer newPlayer) { }

        /// <summary>
        /// A remote player left the room.
        /// Note: This player object is a cached copy of the data at the moment they left.
        /// </summary>
        /// <param name="playerWhoLeft">The player object of the user who left.</param>
        public virtual void OnPlayerLeftRoom(ArcNetPlayer playerWhoLeft) { }

        /// <summary>
        /// The Master Client (Host) has changed. Use this to transfer authority.
        /// </summary>
        /// <param name="newMasterClient">The player who is now the Master Client.</param>
        public virtual void OnMasterClientSwitched(ArcNetPlayer newMasterClient) { }

        /// <summary>
        /// Called when the Room's Custom Properties are updated.
        /// contains ONLY the properties that changed.
        /// </summary>
        public virtual void OnRoomPropertiesUpdate(Dictionary<string, object> propertiesChanged) { }
    }
}