using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace ArcNet
{
    public static class ArcNetEventManager
    {
        private static Dictionary<int, ArcNetView> _registeredViews = new Dictionary<int, ArcNetView>();

        public delegate void NetworkEventDelegate(object data);

        // 2. Storage for Listeners (Code -> Actions)
        private static Dictionary<int, NetworkEventDelegate> _listeners = new Dictionary<int, NetworkEventDelegate>();

        public static void RegisterView(int viewId, ArcNetView view)
        {
            if (!_registeredViews.ContainsKey(viewId))
                _registeredViews.Add(viewId, view);
        }

        public static void UnregisterView(int viewId)
        {
            if (_registeredViews.ContainsKey(viewId))
                _registeredViews.Remove(viewId);
        }

        /// <summary>
        /// Called by ArcNetClient when a generic event (OpCode 6) arrives.
        /// </summary>
        public static void HandleEvent(int eventCode, string jsonPayload)
        {
            // Parse the JSON once
            JObject root = JObject.Parse(jsonPayload);
            JToken dataToken = root["data"];

            // Handle Internal Engine Events (200+)
            if (eventCode >= 200)
            {
                HandleInternalEvents(eventCode, dataToken);
                return;
            }

            // Dispatch Game Events (0-199) to Listeners
            if (_listeners.TryGetValue(eventCode, out NetworkEventDelegate callback))
            {
                // Convert JToken to a friendly Dictionary or primitive
                object content = dataToken?.ToObject<object>();
                callback?.Invoke(content);
            }

            switch (eventCode)
            {
                // CODE 110: Predicted Input (Client -> Server)
                // Payload: { tick: 100, input: {...} }
                case 110: 
                    {
                        // Note: Because this is an input sent TO the server, the 'sender' ID 
                        // is usually at the root of the packet, 
                        // NOT inside the data object.
                        int sender = (int?)root["sender"] ?? (int?)dataToken?["sender"] ?? 0;
                        int tick = (int)dataToken["tick"];
                        JToken actualInput = dataToken["input"];

                        // Find the View owned by this sender
                        foreach (var view in _registeredViews.Values)
                        {
                            if (view.OwnerActorNr == sender)
                            {
                                var invoker = view.GetComponent<IArcNetPredictorInvoker>();
                                invoker?.InvokeServerInput(tick, actualInput);
                            }
                        }
                    }
                    break;

                // CODE 112: Reconciliation State (Server -> Client)
                // Payload: { tick: 100, state: {...}, viewId: 5 }
                case 112:
                    {
                        int vId = (int)dataToken["viewId"];
                        int tick = (int)dataToken["tick"];
                        JToken stateData = dataToken["state"];

                        if (GetView(vId, out ArcNetView view))
                        {
                            var invoker = view.GetComponent<IArcNetPredictorInvoker>();
                            invoker?.InvokeReconciliation(tick, stateData);
                        }
                    }
                    break;
            }
        }

        private static void HandleInternalEvents(int code, JToken dataToken)
        {
            if (dataToken == null) return;

            if (code == 201) // Object Sync
            {
                int viewId = (int)dataToken["viewId"];
                if (_registeredViews.TryGetValue(viewId, out ArcNetView view))
                {
                    var content = dataToken["content"].ToObject<Dictionary<string, object>>();
                    view.OnDeserialize(content, 0);
                }
            }
            else if (code == 202) // RPC
            {
                int viewId = (int)dataToken["viewId"];
                string method = (string)dataToken["methodName"];
                string args = (string)dataToken["parameters"];

                if (_registeredViews.TryGetValue(viewId, out ArcNetView view))
                {
                    view.InvokeRPC(method, args);
                }
            }
        }

        public static void HandleRPC(int viewId, string methodName, string jsonArgs)
        {
            if (_registeredViews.TryGetValue(viewId, out ArcNetView view))
            {
                view.InvokeRPC(methodName, jsonArgs);
            }
            else
            {
                Debug.LogWarning($"[ArcNet] Received RPC '{methodName}' for unknown ViewID {viewId}");
            }
        }

        public static void AddListener(byte eventCode, NetworkEventDelegate callback)
        {
            int code = (int)eventCode;
            if (!_listeners.ContainsKey(code))
                _listeners[code] = callback;
            else
                _listeners[code] += callback;
        }

        public static void RemoveListener(byte eventCode, NetworkEventDelegate callback)
        {
            int code = (int)eventCode;
            if (_listeners.ContainsKey(code))
            {
                _listeners[code] -= callback;
                if (_listeners[code] == null) _listeners.Remove(code);
            }
        }

        public static bool GetView(int viewId, out ArcNetView view)
        {
            return _registeredViews.TryGetValue(viewId, out view);
        }

        public static void DestroyView(int viewId)
        {
            if (_registeredViews.TryGetValue(viewId, out ArcNetView view))
            {
                // Unregister first to prevent dictionary errors
                UnregisterView(viewId);

                // Destroy the GameObject
                if (view != null && view.gameObject != null)
                {
                    Object.Destroy(view.gameObject);
                }
            }
            else
            {
                Debug.LogWarning($"[ArcNet] Set to destroy ViewID {viewId} but it was not found.");
            }
        }
    }
}