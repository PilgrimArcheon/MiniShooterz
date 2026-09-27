using UnityEngine;
using ArcNet;
using System;

namespace ArcNet.Demo
{
    public class CollectorPlayer : ArcNetBehaviour, IArcNetObservable
    {
        public float moveSpeed = 8f;
        private Rigidbody _rb;
        private Animator _anim;
        private int _myScore = 0;
        private Vector3 movement;

        void Start()
        {
            _rb = GetComponent<Rigidbody>();
            _anim = GetComponentInChildren<Animator>();

            if (IsMine)
            {
                GetComponentInChildren<Renderer>().material.color = Color.green;
                _rb.isKinematic = false;
                ArcNetClient.Instance.SetPlayerProperty("score", 0);
            }
            else
            {
                GetComponentInChildren<Renderer>().material.color = Color.red;
                _rb.isKinematic = true;
            }
        }

        void FixedUpdate()
        {
            if (!IsMine) return;

            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");

            movement = new Vector3(h, 0, v);
            _rb.MovePosition(transform.position + movement * moveSpeed * Time.fixedDeltaTime);

            if (movement.magnitude > 0.1f)
            {
                Quaternion targetRot = Quaternion.LookRotation(movement);
                _rb.MoveRotation(Quaternion.Lerp(transform.rotation, targetRot, 10f * Time.fixedDeltaTime));
            }

            _anim.SetBool("move", movement.magnitude > 0);
        }

        public void OnSerializeArcNetView(ArcNetStream stream, ArcNetMessageInfo info)
        {
            // In Client Hosted Mode, the Owner writes.
            if (stream.IsWriting)
            {
                stream.SendNext("move_anim", movement.magnitude > 0);
            }
            else
            {
                bool netMove = Convert.ToBoolean(stream.ReceiveNext("move_anim"));
                _anim?.SetBool("move", netMove);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsMine) return;

            if (other.CompareTag("Ball"))
            {
                ArcNetClient.Instance.Destroy(other.gameObject);
                _myScore++;
                ArcNetClient.Instance.SetPlayerProperty("score", _myScore);
            }
        }
    }
}