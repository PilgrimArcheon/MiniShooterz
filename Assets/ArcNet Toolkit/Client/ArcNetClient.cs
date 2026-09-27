using UnityEngine;
using NativeWebSocket;
using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;

namespace ArcNet
{
    public class ArcNetClient : MonoBehaviour
    {
        #region Singleton & Initialization

        static ArcNetClient instance;
        public static ArcNetClient Instance
        {
            get
            {
                if (!instance)
                {
                    instance = new GameObject("ArcNetClient").AddComponent<ArcNetClient>();
                }
                return instance;
            }
        }
        private WebSocket _socket;

        void Awake()
        {
            if (instance == null) { instance = this; DontDestroyOnLoad(gameObject); }
            else Destroy(gameObject);
        }

        #endregion

        #region Configuration
        [Header("Network Settings")]
        public ArcNetSettings settings;
        [HideInInspector]
        public string UserId;

        [Header("Connection Resilience")]
        [SerializeField] private int maxReconnectAttempts = 5;
        [SerializeField] private float initialReconnectDelay = 1f;
        [SerializeField] private float maxReconnectDelay = 30f;
        [SerializeField] private float heartbeatInterval = 30f;

        #endregion

        #region Network State

        public bool IsConnected => _socket != null && _socket.State == WebSocketState.Open;
        public ArcNetRoom CurrentRoom { get; private set; }
        public bool InRoom => CurrentRoom != null;
        public int LocalActorNumber { get; private set; }

        public ArcNetPlayer LocalPlayer
        {
            get
            {
                if (InRoom && CurrentRoom.Players.TryGetValue(LocalActorNumber, out ArcNetPlayer lp))
                    return lp;

                // Fallback: If I am the hidden server, return a temporary dummy object 
                // so code calling 'LocalPlayer.DisplayName' doesn't crash.
                if (settings.Mode == AuthorityMode.ServerAuthoritative && IsMasterClient)
                {
                    return new ArcNetPlayer(LocalActorNumber, "SERVER");
                }

                return null;
            }
        }

        public string CurrentRoomId => CurrentRoom?.Id;
        public bool IsMasterClient => InRoom && CurrentRoom.MasterClientId == LocalActorNumber;
        public int Ping { get; private set; }
        #endregion

        #region Reconnection State

        private int _reconnectAttempts = 0;
        private float _lastHeartbeatTime = 0f;
        private bool _isReconnecting = false;
        private Coroutine _reconnectCoroutine = null;

        #endregion

        #region Events

        public static event Action OnConnectedToMaster;
        public static event Action OnDisconnected;
        public static event Action<int, string> OnOperationFailed;

        public static event Action<ArcNetRoom> OnJoinedRoom;
        public static event Action OnLeftRoom;
        public static event Action<List<RoomInfo>> OnRoomListUpdate;

        public static event Action<ArcNetPlayer> OnPlayerEnteredRoom;
        public static event Action<ArcNetPlayer> OnPlayerLeftRoom;
        public static event Action<ArcNetPlayer> OnMasterClientSwitched;
        public static event Action<Dictionary<string, object>> OnRoomPropertiesUpdate;

        #endregion

        #region Connection Management

        public async void ConnectToServer()
        {
            string targetUrl = settings.GetTargetUrl();
            if (string.IsNullOrEmpty(targetUrl))
            {
                Debug.LogError("[ArcNet] Target URL is empty! Check settings.");
                return;
            }

            if (string.IsNullOrEmpty(UserId)) UserId = "Player_" + UnityEngine.Random.Range(100, 999);
            
            try
            {
                _socket = new WebSocket(targetUrl);
                _socket.OnOpen += OnWebSocketOpen;
                _socket.OnClose += OnWebSocketClose;
                _socket.OnMessage += HandleMessage;

                await _socket.Connect();
            }
            catch (Exception e)
            {
                Debug.LogError($"[ArcNet] Connection failed: {e.Message}");
                HandleConnectionFailure();
            }
        }

        private void OnWebSocketOpen()
        {
            Debug.Log("[ArcNet] WebSocket Connected");
            _reconnectAttempts = 0; // Reset on successful connection
            _isReconnecting = false;
            
            // Stop any pending reconnection attempts
            if (_reconnectCoroutine != null)
            {
                StopCoroutine(_reconnectCoroutine);
                _reconnectCoroutine = null;
            }

            // Send authentication
            SendOp(1, new { userId = UserId, appVersion = settings.AppVersion });
            SyncTime();
            
            // Initialize heartbeat timer
            _lastHeartbeatTime = Time.time;
        }

        private void OnWebSocketClose(WebSocketCloseCode closeCode)
        {
            Debug.LogWarning($"[ArcNet] WebSocket Closed. Code: {closeCode}");
            HandleLocalDisconnect();
            OnDisconnected?.Invoke();
            HandleConnectionFailure();
        }

        private void HandleConnectionFailure()
        {
            // Only attempt reconnection if we haven't exceeded max attempts
            if (_reconnectAttempts < maxReconnectAttempts)
            {
                if (_reconnectCoroutine != null)
                {
                    StopCoroutine(_reconnectCoroutine);
                }
                _reconnectCoroutine = StartCoroutine(AttemptReconnectWithBackoff());
            }
            else
            {
                Debug.LogError($"[ArcNet] Max reconnection attempts ({maxReconnectAttempts}) reached. Giving up.");
            }
        }

        private IEnumerator AttemptReconnectWithBackoff()
        {
            _isReconnecting = true;
            _reconnectAttempts++;

            // Exponential backoff: 1s, 2s, 4s, 8s, 16s (capped at 30s)
            float delayWithBackoff = Mathf.Min(initialReconnectDelay * Mathf.Pow(2, _reconnectAttempts - 1), maxReconnectDelay);
            
            Debug.Log($"[ArcNet] Reconnection attempt {_reconnectAttempts}/{maxReconnectAttempts} in {delayWithBackoff:F1}s...");
            yield return new WaitForSeconds(delayWithBackoff);

            if (!IsConnected)
            {
                ConnectToServer();
            }

            _isReconnecting = false;
            _reconnectCoroutine = null;
        }

        public async void Disconnect()
        {
            // Prevent auto-reconnect by setting attempts to max
            _reconnectAttempts = maxReconnectAttempts;
            
            // Stop any pending reconnection coroutine
            if (_reconnectCoroutine != null)
            {
                StopCoroutine(_reconnectCoroutine);
                _reconnectCoroutine = null;
            }

            if (_socket != null) 
            {
                await _socket.Close();
            }
        }

        void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
        void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            string loadedSceneName = scene.name;

            // Identify objects waiting for THIS scene
            var toSpawn = _pendingSpawns.FindAll(x => string.Equals(x.sceneName, loadedSceneName, StringComparison.OrdinalIgnoreCase));

            if (toSpawn.Count == 0) return;

            Debug.Log($"[ArcNet] Scene '{loadedSceneName}' loaded. Spawning {toSpawn.Count} pending objects.");

            // Remove them from the list BEFORE calling PerformSpawn
            // If we don't, PerformSpawn will see them in the list and reject them as "Already Pending"
            _pendingSpawns.RemoveAll(x => string.Equals(x.sceneName, loadedSceneName, StringComparison.OrdinalIgnoreCase));

            // Now spawn them
            foreach (var data in toSpawn)
            {
                Vector3 pos = new Vector3((float)data.pos.x, (float)data.pos.y, (float)data.pos.z);
                Quaternion rot = Quaternion.Euler((float)data.rot.x, (float)data.rot.y, (float)data.rot.z);

                // We pass the ownerId from the data
                PerformSpawn(data.prefab, data.viewId, data.ownerId, pos, rot, data.sceneName);
            }
        }

        private void HandleLocalDisconnect()
        {
            Debug.LogWarning("[ArcNet] Connection Lost.");
            CurrentRoom = null;
            LocalActorNumber = 0;
            _pendingSpawns.Clear();
        }

        void Update()
        {
// Dispatch WebSocket messages
#if !UNITY_WEBGL || UNITY_EDITOR
            // Native platforms and the Editor run WebSockets on background threads,
            // so they require manual dispatching to the main thread.
            // WebGL handles this automatically via browser JS callbacks.
            _socket?.DispatchMessageQueue();
#endif

            // Send heartbeat to keep connection alive
            if (IsConnected && Time.time - _lastHeartbeatTime >= heartbeatInterval)
            {
                SendHeartbeat();
                _lastHeartbeatTime = Time.time;
            }
        }

        private void SendHeartbeat()
        {
            if (IsConnected)
            {
                SendOp(99, new { heartbeat = true }); // OpCode 99 reserved for heartbeat
                // Debug.Log("[ArcNet] Heartbeat sent");
            }
        }

        #endregion

        #region Matchmaking API

        public void CreateRoom(string roomName, RoomOptions options = null)
        {
            if (options == null) options = new RoomOptions();
            var settings = new
            {
                maxPlayers = options.maxPlayers,
                isVisible = options.isVisible,
                isOpen = options.isOpen,
                props = options.customRoomProperties
            };
            if (!options.customRoomProperties.ContainsKey("name"))
                options.customRoomProperties.Add("name", roomName);

            SendOp(2, new { roomId = (string)null, settings });
        }

        public void JoinRoom(string roomId) => SendOp(3, new { roomId });
        public void JoinRandomRoom() => SendOp(4, null);
        public void JoinRandomOrCreateRoom(string roomName = "#ArcGameRoom", RoomOptions options = null)
        {
            if (options == null) options = new RoomOptions();
            if (roomName == "#ArcGameRoom") roomName = "#ArcGameRoom_" + UnityEngine.Random.Range(1000, 9999);
            var settings = new
            {
                maxPlayers = options.maxPlayers,
                isVisible = options.isVisible,
                isOpen = options.isOpen,
                props = new { name = roomName }
            };
            SendOp(10, new { settings });
        }

        public void LeaveRoom() => SendOp(5, null);
        public void GetRoomList() => SendOp(9, null);

        #endregion

        #region Property Management

        public void SetProperties(int targetId, Dictionary<string, object> properties)
        {
            if (!IsConnected || properties == null || properties.Count == 0) return;
            SendOp(7, new { target = targetId, props = properties });
        }

        public void SetRoomProperty(string key, object value)
        {
            if (!InRoom) return;
            try
            {
                if (key == "isOpen") CurrentRoom.Options.isOpen = Convert.ToBoolean(value);
                else if (key == "isVisible") CurrentRoom.Options.isVisible = Convert.ToBoolean(value);
                else if (key == "maxPlayers") CurrentRoom.Options.maxPlayers = Convert.ToInt32(value);
            }
            catch { }

            if (CurrentRoom.CustomProperties.ContainsKey(key)) CurrentRoom.CustomProperties[key] = value;
            else CurrentRoom.CustomProperties.Add(key, value);

            var update = new Dictionary<string, object> { { key, value } };
            SetProperties(0, update);
        }

        public void SetRoomProperties(Dictionary<string, object> props)
        {
            if (!InRoom || props == null || props.Count == 0) return;
            foreach (var kvp in props)
            {
                string key = kvp.Key;
                object value = kvp.Value;
                try
                {
                    if (key == "isOpen") CurrentRoom.Options.isOpen = Convert.ToBoolean(value);
                    else if (key == "isVisible") CurrentRoom.Options.isVisible = Convert.ToBoolean(value);
                    else if (key == "maxPlayers") CurrentRoom.Options.maxPlayers = Convert.ToInt32(value);
                }
                catch { }

                if (CurrentRoom.CustomProperties.ContainsKey(key)) CurrentRoom.CustomProperties[key] = value;
                else CurrentRoom.CustomProperties.Add(key, value);
            }
            SetProperties(0, props);
        }

        public void SetDisplayName(string newName)
        {
            UserId = newName;
            SetPlayerProperty("displayName", newName);
        }

        public void SetPlayerProperty(string key, object value)
        {
            if (LocalPlayer == null) return;
            if (key == "displayName") LocalPlayer.DisplayName = value.ToString();
            if (LocalPlayer.CustomProperties.ContainsKey(key)) LocalPlayer.CustomProperties[key] = value;
            else LocalPlayer.CustomProperties.Add(key, value);

            var update = new Dictionary<string, object> { { key, value } };
            SetProperties(LocalActorNumber, update);
        }

        public void SetPlayerProperties(Dictionary<string, object> props)
        {
            if (LocalPlayer == null || props == null) return;
            foreach (var kvp in props)
            {
                if (kvp.Key == "displayName") LocalPlayer.DisplayName = kvp.Value.ToString();
                LocalPlayer.CustomProperties[kvp.Key] = kvp.Value;
            }
            SetProperties(LocalActorNumber, props);
        }

        #endregion

        #region In-Game Features (Instantiate & RPC)
        private int _spawnCounter = 0;
        private List<InstantiateData> _pendingSpawns = new List<InstantiateData>();

        public GameObject Instantiate(string prefabName, Vector3 position, Quaternion rotation, int ownerId = -1)
        {
            if (!InRoom)
            {
                Debug.LogError("[ArcNet] Cannot Instantiate while not in a room.");
                return null;
            }

            // Determine Owner (Default to Self)
            int finalOwnerId = (ownerId == -1) ? LocalActorNumber : ownerId;

            // Generate ID
            int newViewId = (finalOwnerId * 1000) + (++_spawnCounter);

            // Capture Current Scene Name (Source of Truth for this object)
            string currentSceneName = SceneManager.GetActiveScene().name;

            // Perform Local Spawn (Optimistic)
            GameObject go = PerformSpawn(prefabName, newViewId, finalOwnerId, position, rotation, currentSceneName);

            // Send to Server
            if (go != null || currentSceneName != null)
            {
                var data = new
                {
                    prefab = prefabName,
                    viewId = newViewId,
                    ownerId = finalOwnerId,
                    pos = new { x = position.x, y = position.y, z = position.z },
                    rot = new { x = rotation.eulerAngles.x, y = rotation.eulerAngles.y, z = rotation.eulerAngles.z },
                    sceneName = currentSceneName // Mandatory
                };
                SendOp(8, data);
            }
            return go;
        }

        /// <summary>
        /// The Single Source of Truth for Spawning.
        /// Handles Duplicate Checks, Pending Checks, and Scene Checks.
        /// </summary>
        private GameObject PerformSpawn(string prefabName, int viewId, int ownerId, Vector3 pos, Quaternion rot, string targetSceneName)
        {
            // CHECK 1: DOES IT ALREADY EXIST? (Prevents Echo Duplicates)
            if (ArcNetEventManager.GetView(viewId, out ArcNetView existingView))
            {
                // It exists. We don't spawn a clone. We just return the existing one.
                // Optional: Snap position if it's drifted?
                return existingView.gameObject;
            }

            // CHECK 2: IS IT ALREADY PENDING? (Prevents Queue Flooding)
            // If the server sends the spawn packet 3 times (lag), we don't want 3 pending entries.
            var existingPending = _pendingSpawns.Find(x => x.viewId == viewId);
            if (existingPending != null)
            {
                // Update the existing pending entry with latest data, but don't add a new one.
                existingPending.pos = new Vec3DTO { x = pos.x, y = pos.y, z = pos.z };
                existingPending.rot = new Vec3DTO { x = rot.eulerAngles.x, y = rot.eulerAngles.y, z = rot.eulerAngles.z };
                return null;
            }

            // CHECK 3: SCENE VALIDATION (Prevents Ghosts)
            if (string.IsNullOrEmpty(targetSceneName))
            {
                Debug.LogWarning($"[ArcNet] REJECTED Spawn ViewID {viewId}: Missing SceneName.");
                return null;
            }

            string currentSceneName = SceneManager.GetActiveScene().name;
            bool sceneMatch = string.Equals(currentSceneName, targetSceneName, StringComparison.OrdinalIgnoreCase);

            if (!sceneMatch)
            {
                // Queue for later
                var pending = new InstantiateData
                {
                    prefab = prefabName,
                    viewId = viewId,
                    ownerId = ownerId,
                    sceneName = targetSceneName,
                    pos = new Vec3DTO { x = pos.x, y = pos.y, z = pos.z },
                    rot = new Vec3DTO { x = rot.eulerAngles.x, y = rot.eulerAngles.y, z = rot.eulerAngles.z }
                };
                _pendingSpawns.Add(pending);

                // Debug.Log($"[ArcNet] Deferred Spawn ViewID {viewId}. Target: {targetSceneName} | Current: {currentSceneName}");
                return null;
            }

            // CHECK 4: PREFAB VALIDATION
            GameObject prefab = Resources.Load<GameObject>(prefabName);
            if (prefab == null)
            {
                Debug.LogError($"[ArcNet] Prefab '{prefabName}' not found in Resources folder.");
                return null;
            }

            // EXECUTE SPAWN
            GameObject obj = Instantiate(prefab, pos, rot);
            ArcNetView view = obj.GetComponent<ArcNetView>();
            if (view == null) view = obj.AddComponent<ArcNetView>();

            view.SetIdentity(viewId, ownerId);

            // Allow immediate cleanup if we are loading scenes rapidly
            if (viewId == 0) Debug.LogWarning("[ArcNet] Spawned object with ViewID 0. This is invalid.");

            return obj;
        }

        public void Destroy(GameObject target, float delay = 0f)
        {
            if (!InRoom || target == null) return;
            ArcNetView view = target.GetComponent<ArcNetView>();
            if (view == null) return;

            // Remove from local immediately so UI feels responsive
            if (delay == 0) ArcNetEventManager.DestroyView(view.viewId);
            else StartCoroutine(DelayedDestroy(view.viewId, delay));

            // Tell server
            SendOp(11, new { viewId = view.viewId });
        }

        IEnumerator DelayedDestroy(int viewId, float delay)
        {
            yield return new WaitForSeconds(delay);
            ArcNetEventManager.DestroyView(viewId);
        }
        #endregion

        #region Time Sync

        public double ServerTime => (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds + _timeOffset;
        private double _timeOffset = 0;

        public void SyncTime()
        {
            SendOp(12, new { t = DateTime.UtcNow.Ticks });
        }

        #endregion

        #region Protocol & Serialization

        public void RaiseEvent(byte eventCode, object content)
        {
            if (!InRoom) return;
            var payload = new { code = eventCode, data = content };
            SendOp(6, payload);
        }

        public async void SendOp(int opCode, object payload)
        {
            if (!IsConnected) return;
            string payloadJson = payload != null ? JsonConvert.SerializeObject(payload) : "{}";
            string finalPacket = $"{{\"op\":{opCode},\"ver\":\"{settings.AppVersion}\",\"p\":{payloadJson}}}";
            await _socket.SendText(finalPacket);
        }

        #endregion

        #region Incoming Message Handling
        private void HandleMessage(byte[] bytes)
        {
            try
            {
                string json = System.Text.Encoding.UTF8.GetString(bytes);
                var header = JsonConvert.DeserializeObject<PacketHeader>(json);
                if (header == null) return;

                // Ignore heartbeat responses (OpCode 99)
                if (header.op == 99) return;

                if (header.result != 0)
                {
                    Debug.LogError($"[ArcNet] OpCode {header.op} Failed: {header.msg}");
                    OnOperationFailed?.Invoke(header.op, header.msg);
                    return;
                }

                switch (header.op)
                {
                    case 1: // Authenticate
                        OnConnectedToMaster?.Invoke();
                        break;
                    case 3: // Join Room
                    case 10: // JoinRandomOrCreate
                        HandleJoinRoom(json);
                        break;
                    case 5: // Leave Room
                        CurrentRoom = null;
                        OnLeftRoom?.Invoke();
                        break;
                    case 6: // Raise Event
                        HandleEvent(json);
                        break;
                    case 9: // Room List
                        var listPacket = JsonConvert.DeserializeObject<Packet_RoomList>(json);
                        OnRoomListUpdate?.Invoke(listPacket.list);
                        break;
                    case 12: // Time Response
                        var pTime = JsonConvert.DeserializeObject<Packet_Time>(json);
                        long serverTimeMs = pTime.p.t;
                        long clientSentTicks = pTime.p.ct;
                        long rttTicks = DateTime.UtcNow.Ticks - clientSentTicks;
                        double rttMs = rttTicks / 10000.0;
                        double serverTimeNowMs = serverTimeMs + (rttMs / 2.0);
                        double clientTimeNowMs = (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds;
                        _timeOffset = serverTimeNowMs - clientTimeNowMs;
                        Ping = (int)rttMs;
                        break;
                }
            }
            catch
            // (Exception e)
            {
                string json = System.Text.Encoding.UTF8.GetString(bytes);
                //Debug.LogError($"[ArcNet] {json} - Message Handler Error: {e.Message}\n{e.StackTrace}");
            }
        }

        private void HandleJoinRoom(string json)
        {
            var joinPacket = JsonConvert.DeserializeObject<Packet_Join>(json);
            this.LocalActorNumber = joinPacket.actorId;

            var initialProps = joinPacket.props ?? new Dictionary<string, object>();

            CurrentRoom = new ArcNetRoom(joinPacket.roomId, joinPacket.masterId, joinPacket.props);

            // CHECK: Are we in Server Mode?
            bool isServerMode = settings.Mode == AuthorityMode.ServerAuthoritative;

            // Process Remote Peers (Clients seeing others)
            if (joinPacket.peers != null)
            {
                foreach (var peerDto in joinPacket.peers)
                {
                    // FILTER: If this peer is the Master Client (Server) and we are in Server Mode, HIDE IT.
                    if (isServerMode && peerDto.id == joinPacket.masterId) continue;

                    ArcNetPlayer newPlayer = new ArcNetPlayer(peerDto.id, peerDto.name);
                    Debug.Log($"[ArcNet] Player {newPlayer.DisplayName} (ID: {newPlayer.ActorNumber}) joined the room.");
                    newPlayer.CustomProperties = peerDto.props ?? new Dictionary<string, object>();

                    if (!CurrentRoom.Players.ContainsKey(peerDto.id))
                        CurrentRoom.Players.Add(peerDto.id, newPlayer);
                }
            }

            // Process Objects (Spawned Entities)
            if (joinPacket.objects != null)
            {
                // Inside HandleJoinRoom, inside the "objects" loop:
                foreach (var objData in joinPacket.objects)
                {
                    Vector3 pos = new Vector3((float)objData.pos.x, (float)objData.pos.y, (float)objData.pos.z);
                    Quaternion rot = Quaternion.Euler((float)objData.rot.x, (float)objData.rot.y, (float)objData.rot.z);

                    // Pass objData.sceneName
                    PerformSpawn(objData.prefab, objData.viewId, objData.ownerId, pos, rot, objData.sceneName);
                }
            }

            // Process Local Player (Me)
            // FILTER: If I am the Master Client (Server) in Server Mode, DO NOT Add myself to the list.
            bool iAmTheServer = isServerMode && LocalActorNumber == joinPacket.masterId;

            if (!CurrentRoom.Players.ContainsKey(LocalActorNumber) && !iAmTheServer)
            {
                CurrentRoom.Players.Add(LocalActorNumber, new ArcNetPlayer(LocalActorNumber, UserId));
            }

            // Process RPCs
            if (joinPacket.bufferedRpcs != null)
            {
                foreach (var rpc in joinPacket.bufferedRpcs)
                {
                    ArcNetEventManager.HandleRPC(rpc.viewId, rpc.methodName, rpc.parameters);
                }
            }

            OnJoinedRoom?.Invoke(CurrentRoom);
        }

        private void HandleEvent(string json)
        {
            var eventHeader = JsonConvert.DeserializeObject<Packet_EventHeader>(json);

            switch (eventHeader.code)
            {
                case 101: // Player Joined
                    var pJoin = JsonConvert.DeserializeObject<Packet_Event_Actor>(json);
                    int newActorId = pJoin.data.actorId;
                    ArcNetPlayer joinedPlayer = null;
                    if (CurrentRoom != null)
                    {
                        if (!CurrentRoom.Players.ContainsKey(newActorId))
                        {
                            joinedPlayer = new ArcNetPlayer(newActorId, pJoin.data.name);
                            if (pJoin.data.props != null) joinedPlayer.CustomProperties = pJoin.data.props;
                            CurrentRoom.Players.Add(newActorId, joinedPlayer);
                        }
                        else joinedPlayer = CurrentRoom.Players[newActorId];
                    }
                    if (joinedPlayer != null) OnPlayerEnteredRoom?.Invoke(joinedPlayer);
                    break;

                case 102: // Player Left
                    var pLeft = JsonConvert.DeserializeObject<Packet_Event_Actor>(json);
                    int leftActorId = pLeft.data.actorId;
                    ArcNetPlayer leftPlayer = null;
                    if (CurrentRoom != null && CurrentRoom.Players.TryGetValue(leftActorId, out leftPlayer))
                        CurrentRoom.Players.Remove(leftActorId);
                    if (leftPlayer == null) leftPlayer = new ArcNetPlayer(leftActorId, "Unknown");
                    OnPlayerLeftRoom?.Invoke(leftPlayer);
                    break;

                case 103: // Properties
                    var pProps = JsonConvert.DeserializeObject<Packet_Event_Props>(json);
                    var data = pProps.data;
                    if (data != null && data.props != null)
                    {
                        if (data.targetId == 0)
                        {
                            if (data.props.ContainsKey("maxPlayers")) CurrentRoom.Options.maxPlayers = Convert.ToInt32(data.props["maxPlayers"]);
                            if (data.props.ContainsKey("isOpen")) CurrentRoom.Options.isOpen = Convert.ToBoolean(data.props["isOpen"]);
                            ApplyDelta(null, CurrentRoom.CustomProperties, data.props);

                            OnRoomPropertiesUpdate?.Invoke(CurrentRoom.CustomProperties);
                        }
                        else if (CurrentRoom.Players.TryGetValue(data.targetId, out ArcNetPlayer targetPlayer))
                        {
                            ApplyDelta(targetPlayer, targetPlayer.CustomProperties, data.props);
                        }
                    }
                    break;

                case 104: // RPC
                    var pRpc = JsonConvert.DeserializeObject<Packet_Event_RPC>(json);
                    ArcNetEventManager.HandleRPC(pRpc.data.viewId, pRpc.data.methodName, pRpc.data.parameters);
                    break;

                case 105: // Instantiate
                    var pInst = JsonConvert.DeserializeObject<Packet_Event_Instantiate>(json);
                    var iData = pInst.data;
                    Vector3 pos = new Vector3((float)iData.pos.x, (float)iData.pos.y, (float)iData.pos.z);
                    Quaternion rot = Quaternion.Euler((float)iData.rot.x, (float)iData.rot.y, (float)iData.rot.z);

                    // Pass iData.sceneName
                    PerformSpawn(iData.prefab, iData.viewId, iData.ownerId, pos, rot, iData.sceneName);
                    break;

                case 106: // Master Switch
                    var pMaster = JsonConvert.DeserializeObject<Packet_Event_Master>(json);
                    int newMasterId = pMaster.data.masterId;
                    CurrentRoom.MasterClientId = newMasterId;
                    CurrentRoom.Players.TryGetValue(newMasterId, out ArcNetPlayer newMaster);
                    if (newMaster == null) newMaster = new ArcNetPlayer(newMasterId, "Unknown");
                    OnMasterClientSwitched?.Invoke(newMaster);
                    break;

                case 107: // Destroy
                    var pDestroy = JsonConvert.DeserializeObject<Packet_Event_Destroy>(json);
                    int destroyId = pDestroy.data.viewId;

                    // Check if it was waiting to spawn. If so, just remove it from the list.
                    int removed = _pendingSpawns.RemoveAll(x => x.viewId == destroyId);

                    if (removed > 0)
                    {
                        // It was pending, so we just deleted the "plans" to spawn it. No need to call DestroyView.
                        // Debug.Log($"[ArcNet] Cancelled pending spawn for ViewID {destroyId}");
                    }
                    else
                    {
                        // It wasn't pending, so it must be in the scene. Destroy it.
                        ArcNetEventManager.DestroyView(destroyId);
                    }
                    break;

                case 108: // Ownership Changed
                    try
                    {
                        JObject root = JObject.Parse(json);
                        JToken dataNode = root["data"];
                        int tViewId = dataNode["viewId"]?.Value<int>() ?? 0;
                        int nOwner = dataNode["newOwnerId"]?.Value<int>() ?? 0;
                        if (tViewId != 0 && ArcNetEventManager.GetView(tViewId, out ArcNetView tView))
                        {
                            tView.OnOwnershipChanged(nOwner);
                        }
                    }
                    catch (Exception e) { Debug.LogError($"[ArcNet] Ownership Error: {e.Message}"); }
                    break;

                default:
                    ArcNetEventManager.HandleEvent(eventHeader.code, json);
                    break;
            }
        }

        private void ApplyDelta(ArcNetPlayer player, Dictionary<string, object> targetDict, Dictionary<string, object> delta)
        {
            foreach (var kvp in delta)
            {
                // Update the Dictionary
                if (targetDict.ContainsKey(kvp.Key)) targetDict[kvp.Key] = kvp.Value;
                else targetDict.Add(kvp.Key, kvp.Value);

                // Update Player specific
                if (player != null && kvp.Key == "displayName")
                    player.DisplayName = kvp.Value.ToString();

                // Update Room Options if this is the Room Dictionary
                if (player == null && CurrentRoom != null)
                {
                    if (kvp.Key == "isOpen") CurrentRoom.Options.isOpen = Convert.ToBoolean(kvp.Value);
                    else if (kvp.Key == "isVisible") CurrentRoom.Options.isVisible = Convert.ToBoolean(kvp.Value);
                    else if (kvp.Key == "maxPlayers") CurrentRoom.Options.maxPlayers = Convert.ToInt32(kvp.Value);
                }
            }
        }

        #endregion

        #region DTOs (Internal)
        [Serializable] class PacketHeader { public int op; public int result; public string msg; }
        [Serializable] class Packet_Join { public string roomId; public int actorId; public int masterId; public List<PeerDTO> peers; public Dictionary<string, object> props; public List<InstantiateData> objects; public List<BufferedRpcDTO> bufferedRpcs; }
        [Serializable] class BufferedRpcDTO { public int viewId; public string methodName; public string parameters; public int sender; }
        [Serializable] class PeerDTO { public int id; public string name; public Dictionary<string, object> props; }
        [Serializable] class Packet_RoomList { public List<RoomInfo> list; }
        [Serializable] class Packet_EventHeader { public int code; }

        // Wrappers
        [Serializable] class Packet_Event_Actor { public int code; public ActorData data; }
        [Serializable] class Packet_Event_Master { public int code; public MasterData data; }
        [Serializable] class Packet_Event_Props { public int code; public PropsData data; }
        [Serializable] class Packet_Event_RPC { public int code; public RPCData data; }
        [Serializable] class Packet_Event_Instantiate { public int code; public InstantiateData data; }
        [Serializable] class Packet_Event_Destroy { public int code; public DestroyData data; }

        // Containers
        [Serializable] class ActorData { public int actorId; public string name; public Dictionary<string, object> props; }
        [Serializable] class MasterData { public int masterId; }
        [Serializable] class PropsData { public int targetId; public Dictionary<string, object> props; }
        [Serializable] class RPCData { public int viewId; public string methodName; public string parameters; }
        [Serializable] class InstantiateData { public string prefab; public int viewId; public int ownerId; public Vec3DTO pos; public Vec3DTO rot; public string sceneName; }
        [Serializable] class DestroyData { public int viewId; }
        [Serializable] class Vec3DTO { public double x; public double y; public double z; }
        [Serializable] class Packet_Time { public TimePayload p; }
        [Serializable] class TimePayload { public long t; public long ct; }
        #endregion
    }

    #region External Types & Extensions
    [Serializable]
    public class ArcNetPlayer
    {
        public int ActorNumber; 
        public string DisplayName;
        public Dictionary<string, object> CustomProperties = new Dictionary<string, object>();
        public ArcNetPlayer(int actorNumber, string displayName) { this.ActorNumber = actorNumber; this.DisplayName = displayName; }

        // Safe Property Getter
        public T GetProperty<T>(string key, T defaultValue = default)
        {
            if (CustomProperties.TryGetValue(key, out object val))
            {
                try { return (T)Convert.ChangeType(val, typeof(T)); }
                catch { return defaultValue; }
            }
            return defaultValue;
        }
    }
    [Serializable]
    public class ArcNetRoom
    {
        public string Id;
        public int MasterClientId;
        public RoomOptions Options;
        public Dictionary<int, ArcNetPlayer> Players = new Dictionary<int, ArcNetPlayer>();

        // This is the main dictionary your game logic reads
        public Dictionary<string, object> CustomProperties = new Dictionary<string, object>();

        public int PlayerCount => Players.Count;

        public ArcNetRoom(string id, int masterId, Dictionary<string, object> props)
        {
            this.Id = id;
            this.MasterClientId = masterId;

            // Assign the incoming props to the main Dictionary immediately
            if (props != null) this.CustomProperties = new Dictionary<string, object>(props);
            else this.CustomProperties = new Dictionary<string, object>();

            // Ensure Options also has a reference or copy
            this.Options = new RoomOptions
            {
                customRoomProperties = this.CustomProperties
            };

            // Sync Options variables (MaxPlayers, isOpen, etc) from the dictionary
            if (this.CustomProperties.ContainsKey("maxPlayers"))
                this.Options.maxPlayers = Convert.ToInt32(this.CustomProperties["maxPlayers"]);

            if (this.CustomProperties.ContainsKey("isOpen"))
                this.Options.isOpen = Convert.ToBoolean(this.CustomProperties["isOpen"]);

            if (this.CustomProperties.ContainsKey("isVisible"))
                this.Options.isVisible = Convert.ToBoolean(this.CustomProperties["isVisible"]);
        }

        public T GetProperty<T>(string key, T defaultValue = default)
        {
            if (CustomProperties.TryGetValue(key, out object val))
            {
                try { return (T)Convert.ChangeType(val, typeof(T)); }
                catch { return defaultValue; }
            }
            return defaultValue;
        }
    }
    [Serializable] public class RoomInfo { public string id; public int max; public RoomOptions options; }
    [Serializable]
    public class RoomOptions
    {
        public bool isVisible = true; public bool isOpen = true; public int maxPlayers = 4;
        public Dictionary<string, object> customRoomProperties = new Dictionary<string, object>();
        [JsonIgnore] public bool IsVisible { get => isVisible; set => isVisible = value; }
        [JsonIgnore] public bool IsOpen { get => isOpen; set => isOpen = value; }
        [JsonIgnore] public int MaxPlayers { get => maxPlayers; set => maxPlayers = value; }
        [JsonIgnore] public Dictionary<string, object> CustomRoomProperties { get => customRoomProperties; set => customRoomProperties = value; }
    }
    #endregion
}