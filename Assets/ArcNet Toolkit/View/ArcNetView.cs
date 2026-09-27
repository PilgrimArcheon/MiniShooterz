using System;
using UnityEngine;
using System.Reflection;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace ArcNet
{
    [AddComponentMenu("ArcNet/ArcNet View")]
    public class ArcNetView : MonoBehaviour
    {
        [Header("Network Identity")]
        public int viewId;
        public int OwnerActorNr;

        private bool _hasPredictor;
        private IArcNetPredictorInvoker _predictor;
        public bool IsMine
        {
            get
            {
                // If I own it directly, it's mine.
                if (OwnerActorNr == ArcNetClient.Instance.LocalActorNumber) return true;

                // If it's a Room Object (0), and I am the Master Client, it's mine (effectively).
                if (OwnerActorNr == 0 && ArcNetClient.Instance.IsMasterClient) return true;

                return false;
            }
        }

        public bool IsPredicting => IsMine && _hasPredictor;

        [Header("State Synchronization")]
        public List<Component> observedComponents = new List<Component>();
        public float serializationRate = 0.1f;
        private float _lastSyncTime;

        private void Awake()
        {
            // Cache Predictor
            _predictor = GetComponent<IArcNetPredictorInvoker>();
            _hasPredictor = _predictor != null;
            
            // This prevents the "Inspector Override" bug where dragging one script 
            // accidentally excludes the TransformView.
            observedComponents.Clear();

            var observables = GetComponents<IArcNetObservable>();
            foreach (var obs in observables)
            {
                if (obs is Component comp)
                {
                    observedComponents.Add(comp);
                }
            }

            // Optional Debug to confirm it found both scripts
            // Debug.Log($"[ArcNetView] Observing {observedComponents.Count} components on {gameObject.name}");
        }

        private void OnEnable()
        {
            if (viewId != 0) ArcNetEventManager.RegisterView(viewId, this);
        }

        public void SetIdentity(int id, int owner)
        {
            this.viewId = id;
            this.OwnerActorNr = owner;
            ArcNetEventManager.RegisterView(viewId, this);
        }

        private void OnDisable()
        {
            ArcNetEventManager.UnregisterView(viewId);
        }

        public void TransferOwnership(int newOwnerActorNr)
        {
            ArcNetClient.Instance.SendOp(13, new { viewId = this.viewId, targetActorId = newOwnerActorNr });
        }

        public void OnOwnershipChanged(int newOwnerId)
        {
            if (newOwnerId == 0) return;
            this.OwnerActorNr = newOwnerId;
            Debug.Log($"[ArcNet] View {viewId} ownership transferred to Actor {newOwnerId}");
        }

        private void Update()
        {
            if (viewId == 0 || !ArcNetClient.Instance.IsConnected) return;

            // --- PURE SIMULATION MODE ---
            // If we are predicting locally, we DO NOT send standard observable data.
            // Exception: The Master Client MUST send observable data for predicted objects 
            // so that NON-owners can see them.
            if (IsPredicting && !ArcNetClient.Instance.IsMasterClient) return;

            // --- STANDARD AUTHORITY CHECK ---
            // Standard Mode: Only Owner writes.
            // Server Mode: Master Client writes for everyone.
            bool isServerMode = ArcNetClient.Instance.settings.Mode == AuthorityMode.ServerAuthoritative;
            bool canWrite = isServerMode ? ArcNetClient.Instance.IsMasterClient : IsMine;

            if (!canWrite) return;
            
            if (Time.time - _lastSyncTime > serializationRate)
            {
                _lastSyncTime = Time.time;
                if (observedComponents != null && observedComponents.Count > 0) SerializeAndSend();
            }
        }

        private void SerializeAndSend()
        {
            var stream = new ArcNetStream(true);
            var info = new ArcNetMessageInfo { timestamp = Time.time, senderActorNr = OwnerActorNr };

            foreach (var comp in observedComponents)
            {
                if (comp is IArcNetObservable observable) observable.OnSerializeArcNetView(stream, info);
            }

            var data = stream.GetData();
            if (data.Count > 0)
            {
                ArcNetClient.Instance.SendOp(6, new { code = 201, data = new { viewId = this.viewId, content = data } });
            }
        }

        public void OnDeserialize(Dictionary<string, object> incomingData, int senderActorNr)
        {
            var stream = new ArcNetStream(false, incomingData);
            var info = new ArcNetMessageInfo { timestamp = Time.time, senderActorNr = senderActorNr };

            foreach (var comp in observedComponents)
            {
                if (comp is IArcNetObservable observable) observable.OnSerializeArcNetView(stream, info);
            }
        }

        // ... RPC code remains the same ...
        public void RPC(string methodName, RpcTarget target, params object[] args)
        {
            string jsonArgs = JsonConvert.SerializeObject(args);
            bool executeLocally = false;
            switch (target)
            {
                case RpcTarget.All: executeLocally = true; break;
                case RpcTarget.MasterClient: if (ArcNetClient.Instance.IsMasterClient) executeLocally = true; break;
            }

            if (executeLocally) InvokeRPC(methodName, jsonArgs);
            if (target == RpcTarget.MasterClient && executeLocally) return;

            var payload = new { viewId = this.viewId, methodName = methodName, parameters = jsonArgs, target = (int)target };
            ArcNetClient.Instance.SendOp(6, new { code = 104, data = payload });
        }

        public void InvokeRPC(string methodName, string jsonArgs)
        {
            MonoBehaviour[] monos = GetComponents<MonoBehaviour>();
            object[] rawParameters = JsonConvert.DeserializeObject<object[]>(jsonArgs);

            foreach (var mono in monos)
            {
                MethodInfo method = mono.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (method != null && method.GetCustomAttribute<ArcRPCAttribute>() != null)
                {
                    ParameterInfo[] methodParams = method.GetParameters();
                    if (methodParams.Length != rawParameters.Length) continue;

                    object[] convertedParams = new object[methodParams.Length];
                    try
                    {
                        for (int i = 0; i < methodParams.Length; i++)
                        {
                            if (rawParameters[i] == null) { convertedParams[i] = null; continue; }
                            convertedParams[i] = Convert.ChangeType(rawParameters[i], methodParams[i].ParameterType);
                        }
                        method.Invoke(mono, convertedParams);
                    }
                    catch (Exception e) { Debug.LogError($"[ArcNet] RPC '{methodName}' Fail: {e.Message}"); }
                    return;
                }
            }
        }
    }
}