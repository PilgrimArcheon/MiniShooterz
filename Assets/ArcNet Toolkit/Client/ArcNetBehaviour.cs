using UnityEngine;

namespace ArcNet
{
    /// <summary>
    /// Base class for scripts that require an ArcNetView.
    /// Replaces MonoBehaviour. Provides instant access to 'arcNetView' and 'IsMine'.
    /// </summary>
    [RequireComponent(typeof(ArcNetView))]
    public class ArcNetBehaviour : MonoBehaviour
    {
        private ArcNetView _view;

        /// <summary>
        /// Cached reference to the ArcNetView on this GameObject.
        /// </summary>
        public ArcNetView arcNetView
        {
            get
            {
                if (_view == null)
                { 
                    _view = GetComponent<ArcNetView>();
                }
                return _view;
            }
        }

        /// <summary>
        /// True if the local player owns this object.
        /// </summary>
        public bool IsMine => arcNetView.IsMine;

        /// <summary>
        /// The unique ID of this networked object.
        /// </summary>
        public int ViewId => arcNetView.viewId;

        /// <summary>
        /// The Actor Number of the owner.
        /// </summary>
        public int OwnerId => arcNetView.OwnerActorNr;
    }
}