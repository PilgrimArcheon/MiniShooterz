using UnityEngine;
using ArcNet;
using System;
using Newtonsoft.Json;

namespace ArcNet.Demo
{
    [Serializable]
    public struct DemoInput
    {
        public float h;
        public float v;
    }

    public class ServerAuthPlayer : ArcNetBehaviour, IArcNetObservable
    {
        public float speed = 5f;
        private Rigidbody _rb;
        private Animator _anim;
        private Vector3 _serverVelocity;

        void Start()
        {
            _anim = GetComponentInChildren<Animator>();
            _rb = GetComponent<Rigidbody>();

            // Color code: Green = Me, Red = Others
            if (IsMine)
            {
                GetComponentInChildren<Renderer>().material.color = Color.green;
                _rb.isKinematic = true;
            }
            else
            {
                GetComponentInChildren<Renderer>().material.color = Color.red;
                _rb.isKinematic = false;
            }

            ArcNetInput.Register<DemoInput>(this, OnServerInput);
        }

        private void OnDisable() { ArcNetInput.Unregister(this); }

        // --- CORE MOVEMENT LOGIC (Run by Server) --- // 
        private void OnServerInput(DemoInput input)
        {
            _serverVelocity = new Vector3(input.h, 0, input.v);
        }

        void FixedUpdate()
        {
            if (!ArcNetClient.Instance.IsMasterClient) return;

            _rb.MovePosition(transform.position + speed * Time.fixedDeltaTime * _serverVelocity);

            if (_serverVelocity.magnitude > 0.1f)
            {
                Quaternion targetRot = Quaternion.LookRotation(_serverVelocity);
                _rb.MoveRotation(Quaternion.Lerp(transform.rotation, targetRot, 10f * Time.fixedDeltaTime));
            }

            _anim.SetBool("move", _serverVelocity.magnitude > 0);
        }

        // --- CLIENT SIDE INPUT --- //
        void Update()
        {
            if (!IsMine) return;

            // Capture Input
            var input = new DemoInput
            {
                h = Input.GetAxis("Horizontal"),
                v = Input.GetAxis("Vertical")
            };

            var data = new { input };

            ArcNetClient.Instance.SendOp(6, new { code = 110, data });
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