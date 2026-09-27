using UnityEngine;
using ArcNet;
using System;

namespace ArcNet.Demo
{
    [Serializable]
    public struct PlayerInput
    {
        public float h, v;
    }

    [Serializable]
    public struct PlayerState
    {
        public Vector3 pos;
        public bool isMoving;
    }

    public class PredictedPlayer : ArcNetPredictor<PlayerInput, PlayerState>, IArcNetSimulation<PlayerInput, PlayerState>, IArcNetObservable
    {
        public float speed = 5f;
        private Animator _anim;
        private Vector3 _serverVelocity;
        private Rigidbody _rb;

        protected override void Awake()
        {
            base.Awake();
            _anim = GetComponentInChildren<Animator>();
            _rb = GetComponent<Rigidbody>();
        }

        // --- Map Hardware to Struct ---
        protected override PlayerInput GetInput()
        {
            return new PlayerInput
            {
                h = Input.GetAxisRaw("Horizontal"),
                v = Input.GetAxisRaw("Vertical")
            };
        }

        // --- THE SIMULATION ---
        public void ProcessInput(PlayerInput input, float deltaTime)
        {
            _serverVelocity = new Vector3(input.h, 0, input.v);

            _rb.MovePosition(transform.position + speed * deltaTime * _serverVelocity);

            if (_serverVelocity.magnitude > 0.1f)
            {
                Quaternion targetRot = Quaternion.LookRotation(_serverVelocity);
                _rb.MoveRotation(Quaternion.Lerp(transform.rotation, targetRot, 10f * deltaTime));
            }

            _anim.SetBool("move", _serverVelocity.magnitude > 0);
        }

        // --- Snapshot for History ---
        public PlayerState CaptureState()
        {
            return new PlayerState
            {
                pos = transform.position,
                isMoving = _anim != null && _anim.GetBool("move")
            };
        }

        // --- Rewind to Snapshot --- //
        public void RestoreState(PlayerState state)
        {
            transform.position = state.pos;
            if (_anim) _anim.SetBool("move", state.isMoving);
        }

        public void OnSerializeArcNetView(ArcNetStream stream, ArcNetMessageInfo info)
        {
            if (stream.IsWriting)
            {
                stream.SendNext("move_anim", _serverVelocity.magnitude > 0);
            }
            else
            {
                bool netMove = Convert.ToBoolean(stream.ReceiveNext("move_anim"));
                _anim?.SetBool("move", netMove);
            }
        }
    }
}