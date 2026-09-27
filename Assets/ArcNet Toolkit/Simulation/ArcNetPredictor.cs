using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Linq; // Required for FindIndex

namespace ArcNet
{
    // The Bridge Interface (Non-Generic)
    public interface IArcNetPredictorInvoker
    {
        void InvokeServerInput(int tick, JToken inputData);
        void InvokeReconciliation(int tick, JToken stateData);
    }

    [RequireComponent(typeof(ArcNetView))]
    public abstract class ArcNetPredictor<TInput, TState> : ArcNetBehaviour, IArcNetPredictorInvoker
        where TInput : struct
        where TState : struct
    {
        // --- CONFIGURATION ---
        [Header("Prediction Settings")]
        [Tooltip("Max frames to predict ahead. High = smooth but risky.")]
        public int maxPredictionBufferSize = 128; // Increased for safety

        // --- INTERNAL BUFFERS ---
        private struct Frame { public int tick; public TInput input; public TState state; }
        private List<Frame> _history = new List<Frame>();
        private int _currentTick = 0;

        // --- INTERFACE REFERENCE ---
        private IArcNetSimulation<TInput, TState> _simulation;

        protected virtual void Awake()
        {
            _simulation = GetComponent<IArcNetSimulation<TInput, TState>>();
            if (_simulation == null)
                Debug.LogError($"[ArcNet] {name} is missing IArcNetSimulation implementation!");
        }

        private void FixedUpdate()
        {
            if (!ArcNetClient.Instance.IsConnected) return;

            // SERVER (AUTHORITY)
            // Server does not predict. It reacts to InvokeServerInput.
            if (ArcNetClient.Instance.IsMasterClient) return;

            // CLIENT (PREDICTION)
            if (IsMine)
            {
                _currentTick++;

                // Collect Input
                TInput input = GetInput(); 

                // Run Logic Immediately (Prediction)
                _simulation.ProcessInput(input, Time.fixedDeltaTime);

                // Store History
                TState resultingState = _simulation.CaptureState();

                _history.Add(new Frame
                {
                    tick = _currentTick,
                    input = input,
                    state = resultingState
                });

                // Trim Buffer (Prevent memory leaks)
                if (_history.Count > maxPredictionBufferSize) _history.RemoveAt(0);

                // Send to Server (Reliable)
                SendInputToServer(_currentTick, input);
            }
        }

        // --- ABSTRACT: Developer must implement how to read hardware input ---
        protected abstract TInput GetInput();

        // --- BRIDGE: Called by EventManager via Interface ---
        public void InvokeServerInput(int tick, JToken inputData)
        {
            // Deserialize Generic Input
            TInput input = inputData.ToObject<TInput>();
            OnServerInputReceived(tick, input);
        }

        public void InvokeReconciliation(int tick, JToken stateData)
        {
            // Deserialize Generic State
            TState state = stateData.ToObject<TState>();
            OnServerStateReceived(tick, state);
        }

        // --- SERVER SIDE LOGIC ---
        private void OnServerInputReceived(int tick, TInput input)
        {
            if (!ArcNetClient.Instance.IsMasterClient) return;

            // Run the logic with the Client's input
            _simulation.ProcessInput(input, Time.fixedDeltaTime);

            // Capture Result
            TState serverState = _simulation.CaptureState();

            // Send "Truth" back to Client
            SendStateToClient(tick, serverState);
        }

        // --- RECONCILIATION LOGIC ---
        private void OnServerStateReceived(int serverTick, TState serverState)
        {
            if (!IsMine || ArcNetClient.Instance.IsMasterClient) return;

            // 1. Find the local history frame matching this server tick
            int frameIndex = _history.FindIndex(x => x.tick == serverTick);

            if (frameIndex == -1) return; // Too old or not found

            Frame localFrame = _history[frameIndex];

            // COMPARE: Did we predict correctly?
            if (StatesMatch(localFrame.state, serverState))
            {
                // Prediction was perfect!
                // We can discard history up to this point as it is confirmed.
                _history.RemoveRange(0, frameIndex + 1);
                return;
            }

            // MISMATCH DETECTED -> RECONCILE
            // Debug.Log($"[ArcNet] Reconciling at Tick {serverTick}");

            // Rewind: Snap to Server Truth
            _simulation.RestoreState(serverState);

            // Replay: Re-run inputs from ServerTick + 1 to CurrentTick
            for (int i = frameIndex + 1; i < _history.Count; i++)
            {
                // Re-process stored input
                _simulation.ProcessInput(_history[i].input, Time.fixedDeltaTime);

                // Update stored state with new corrected result
                Frame correctedFrame = _history[i];
                correctedFrame.state = _simulation.CaptureState();
                _history[i] = correctedFrame;
            }
            
            // Clean up old history
            _history.RemoveRange(0, frameIndex + 1);
        }

        protected virtual bool StatesMatch(TState a, TState b) => a.Equals(b);

        // --- NETWORK HELPERS ---
        private void SendInputToServer(int tick, TInput input)
        {
            var data = new { tick, input };
            ArcNetClient.Instance.SendOp(6, new { code = 110, data }); // Code 110 = PredictedInput
        }

        private void SendStateToClient(int tick, TState state)
        {
            var data = new { tick, state, viewId = ViewId }; // Send ViewId so Client knows who to correct
            ArcNetClient.Instance.SendOp(6, new { code = 112, data }); // Code 112 = Reconciliation
        }
    }
}