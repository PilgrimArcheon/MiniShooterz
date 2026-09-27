using UnityEngine;

namespace ArcNet
{
    public class ArcNetDestroy : ArcNetBehaviour
    {
        [SerializeField] float lifeTime = 1f;

        private void Start()
        {
            if (!IsMine) return;
            Invoke(nameof(Dest), lifeTime);
        }

        void Dest() => ArcNetClient.Instance.Destroy(gameObject);
    }
}