using UnityEngine;

namespace ArcNet
{
    [RequireComponent(typeof(ArcNetView))]
    [AddComponentMenu("ArcNet/ArcNet Transform View")]
    public class ArcNetTransformView : ArcNetBehaviour, IArcNetObservable
    {
        #region Settings

        [Header("Sync Options")]
        public bool syncPosition = true;
        public bool syncRotation = true;

        [Tooltip("Higher values mean snappier movement, lower values mean smoother lag compensation.")]
        public float smoothingSpeed = 10f;

        #endregion

        #region Internal State

        private Vector3 _networkPosition;
        private Quaternion _networkRotation;
        private bool _firstPacketReceived = false; // Renamed for clarity

        #endregion

        // --- INITIALIZE WITH CURRENT STATE --- //
        private void Awake()
        {
            // Initialize network state to current transform to prevent
            _networkPosition = transform.position;
            _networkRotation = transform.rotation;
        }

        #region IArcNetObservable Implementation

        public void OnSerializeArcNetView(ArcNetStream stream, ArcNetMessageInfo info)
        {
            bool isServerAuth = ArcNetClient.Instance.settings.Mode == AuthorityMode.ServerAuthoritative;
            
            // In Server Auth: Only Master Client writes.
            // In Client Hosted: Owner writes.
            bool canWrite = isServerAuth ? ArcNetClient.Instance.IsMasterClient : IsMine;

            if (stream.IsWriting)
            {
                if (!canWrite) return;

                if (syncPosition) stream.SendNext("p", transform.position);
                if (syncRotation) stream.SendNext("r", transform.rotation);
            }
            else
            {
                // Note: Even the owner reads if we are in Server Auth mode (Client Prediction/Correction)
                // But for now, we only let non-writers read.
                if (canWrite) return; 
                
                if (syncPosition)
                    _networkPosition = stream.ReceiveVector3("p");

                if (syncRotation)
                {
                    Quaternion incomingRot = stream.ReceiveQuaternion("r");
                    
                    // Safety check: Prevent invalid quaternions from network
                    if (incomingRot.w == 0 && incomingRot.x == 0 && incomingRot.y == 0 && incomingRot.z == 0)
                        _networkRotation = Quaternion.identity;
                    else
                        _networkRotation = incomingRot;
                }

                if (!_firstPacketReceived)
                {
                    // Snap immediately on first valid packet to avoid lerping from spawn point if it differs
                    if (syncPosition) transform.position = _networkPosition;
                    if (syncRotation) transform.rotation = _networkRotation;
                    _firstPacketReceived = true;
                }
            }
        }

        #endregion

        #region Unity Lifecycle

        void Update()
        {
            // Wait for first packet before interpolating, 
            // OR just rely on Awake defaults (which are safe now).
            // We use the defaults from Awake so the object stays at its spawn point until moved.
            
            bool isServerAuth = ArcNetClient.Instance.settings.Mode == AuthorityMode.ServerAuthoritative;
            bool isOwnerView = isServerAuth ? ArcNetClient.Instance.IsMasterClient : IsMine;

            if (!isOwnerView)
            {
                if (syncPosition)
                {
                    transform.position = Vector3.Lerp(transform.position, _networkPosition, Time.deltaTime * smoothingSpeed);
                }

                if (syncRotation)
                {
                    // Double safety check before Lerp
                    if (_networkRotation.w == 0 && _networkRotation.x == 0 && _networkRotation.y == 0 && _networkRotation.z == 0)
                        _networkRotation = Quaternion.identity;

                    transform.rotation = Quaternion.Lerp(transform.rotation, _networkRotation, Time.deltaTime * smoothingSpeed);
                }
            }
        }

        #endregion
    }
}