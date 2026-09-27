using UnityEngine;

namespace ArcNet
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(ArcNetView))]
    public class ArcNetRigidbodyView : ArcNetBehaviour, IArcNetObservable
    {
        private Rigidbody _rb;
        
        private Vector3 _networkPos;
        private Quaternion _networkRot;
        private Vector3 _networkVel;

        public float lerpSpeed = 10f;
        private float _lag;

        void Awake()
        {
            _rb = GetComponent<Rigidbody>();
        }

        public void OnSerializeArcNetView(ArcNetStream stream, ArcNetMessageInfo info)
        {
            if (stream.IsWriting)
            {
                // Send Position, Rotation, AND Velocity
                stream.SendNext("p", transform.position);
                stream.SendNext("r", transform.rotation.eulerAngles);
                stream.SendNext("v", _rb.linearVelocity); // Unity 6 (use .velocity for older)
            }
            else
            {
                // Receive
                _networkPos = (Vector3)stream.ReceiveNext("p");
                _networkRot = Quaternion.Euler((Vector3)stream.ReceiveNext("r"));
                _networkVel = (Vector3)stream.ReceiveNext("v");

                // Calculate lag (Time since packet was sent)
                _lag = Mathf.Abs((float)(ArcNetClient.Instance.ServerTime - info.timestamp));
            }
        }

        void FixedUpdate()
        {
            if (!IsMine)
            {
                // Account for Lag: Where should the object be NOW based on velocity?
                // Position + (Velocity * Lag)
                Vector3 targetPos = _networkPos + (_networkVel * _lag);

                // Move Rigidbody smoothly towards that target
                float dist = Vector3.Distance(_rb.position, targetPos);

                if (dist > 2f) 
                {
                    // If too far, snap immediately (teleport)
                    _rb.position = targetPos; 
                }
                else 
                {
                    // Smooth move
                    _rb.MovePosition(Vector3.Lerp(_rb.position, targetPos, Time.fixedDeltaTime * lerpSpeed));
                }

                // Apply rotation
                _rb.MoveRotation(Quaternion.Lerp(_rb.rotation, _networkRot, Time.fixedDeltaTime * lerpSpeed));
                
                // Update velocity for consistency
                _rb.linearVelocity = _networkVel;
            }
        }
    }
}