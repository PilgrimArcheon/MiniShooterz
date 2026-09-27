using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

namespace ArcNet
{
    public static class ArcNetInput
    {
        // Internal delegate for our wrapper
        private delegate void InputProcessor(int senderId, string json);

        // Map: Behaviour Instance ID -> Processor Wrapper
        private static Dictionary<int, InputProcessor> _listeners = new Dictionary<int, InputProcessor>();

        /// <summary>
        /// Registers a callback for a specific input type. 
        /// <para>Automatically handles JSON deserialization and Owner Authorization.</para>
        /// </summary>
        /// <typeparam name="T">The struct/class defining your input.</typeparam>
        /// <param name="behaviour">The ArcNetBehaviour listening (usually 'this').</param>
        /// <param name="callback">The method to call when valid input arrives.</param>
        public static void Register<T>(ArcNetBehaviour behaviour, Action<T> callback)
        {
            if (behaviour == null) return;
            int instanceId = behaviour.GetInstanceID();

            if (_listeners.ContainsKey(instanceId))
            {
                Debug.LogWarning($"[ArcNet] {behaviour.name} is already registered for input.");
                return;
            }

            // Create a "Smart Wrapper" for this callback
            InputProcessor wrapper = (senderId, json) =>
            {
                // DIAGNOSTIC LOG: See what the server thinks is happening
                // Debug.Log($"[ArcNet] Input Check: Sender={senderId} vs Owner={behaviour.OwnerId} on {behaviour.name}");

                // Security Check: Does the sender OWN this object?
                if (behaviour.OwnerId != senderId)
                {
                    // If this logs, your OwnerId is wrong on the Server.
                    // Common Cause: OwnerId defaulted to 0 or wasn't passed during Instantiate.
                    // Debug.LogWarning($"[ArcNet] Input Rejected. Sender: {senderId} != Owner: {behaviour.OwnerId}");
                    return;
                }

                // Type-Safe Deserialization
                try
                {
                    T data = JsonConvert.DeserializeObject<T>(json);
                    callback(data);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[ArcNet] Input Parse Error on {behaviour.name}: {e.Message}");
                }
            };

            _listeners.Add(instanceId, wrapper);
        }

        /// <summary>
        /// Stops listening for input. Call this in OnDisable.
        /// </summary>
        public static void Unregister(ArcNetBehaviour behaviour)
        {
            if (behaviour == null) return;
            int instanceId = behaviour.GetInstanceID();

            if (_listeners.ContainsKey(instanceId))
            {
                _listeners.Remove(instanceId);
            }
        }

        /// <summary>
        /// Called internally by the Network Manager when a packet arrives.
        /// </summary>
        internal static void Dispatch(int senderId, string json)
        {
            // Broadcast to all active listeners. 
            // The listeners themselves (the wrappers above) perform the "Is this for me?" check.
            foreach (var processor in _listeners.Values)
            {
                processor(senderId, json);
            }
        }
    }
}