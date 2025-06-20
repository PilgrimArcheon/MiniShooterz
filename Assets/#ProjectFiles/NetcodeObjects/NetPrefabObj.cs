using UnityEngine;
using Unity.Netcode;

public class NetPrefabObj : NetworkBehaviour
{
    public new ulong OwnerClientId { get; private set; }

    public float lifeTime = 3f; // Automatically destroy after this time

    public void Initialize(ulong ownerClientId)
    {
        OwnerClientId = ownerClientId;
        ApplyOwnershipVisuals();
        Invoke(nameof(Despawn), lifeTime);
    }

    private void ApplyOwnershipVisuals()
    {
        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            // Color based on who triggered the effect
            Color color = (OwnerClientId == NetworkManager.Singleton.LocalClientId) ? Color.green : Color.red;
            rend.material.color = color;
        }
    }

    private void Despawn()
    {
        Destroy(gameObject); // Replace with pooling logic if needed
    }
}
