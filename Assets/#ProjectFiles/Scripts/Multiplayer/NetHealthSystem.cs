using UnityEngine;
using System;
using ArcNet;

[RequireComponent(typeof(ArcNetView))]
public class NetHealthSystem : ArcNetBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    
    // We use ArcNet's NetworkVariable to automatically sync health changes
    private NetworkVariable<float> _currentHealth;

    public float CurrentHealth => _currentHealth.Value;

    [SerializeField] GameObject deathPrefab;
    public Action<bool> OnStateChange;

    public int characterTeam; 
    public float respawnTime = 3f;

    private void Awake()
    {
        // Initialize Network Variable with a stream key "hp"
        _currentHealth = new NetworkVariable<float>("hp", maxHealth);
        _currentHealth.OnValueChanged += HandleHealthChanged;
    }

    public void SetUpHealth(int team)
    {
        characterTeam = team;
    }

    // Sync the Network Variable automatically in ArcNet's stream
    public void OnSerializeArcNetView(ArcNetStream stream, ArcNetMessageInfo info)
    {
        _currentHealth.Sync(stream);
    }

    // Only the OWNER registers the hit and broadcasts the damage
    public void TakeDamage(float amount, int damageTeam, int attackingPlayerId)
    {
        if (!IsMine) return; // Only process hits on my own character

        if (damageTeam != characterTeam && _currentHealth.Value > 0) 
        {
            float newHealth = _currentHealth.Value - amount;
            
            // This updates locally AND triggers the NetworkVariable sync
            _currentHealth.Value = newHealth;

            if (_currentHealth.Value <= 0) 
            {
                // Tell everyone this player died
                arcNetView.RPC(nameof(RPC_Die), RpcTarget.All, damageTeam, attackingPlayerId);
            }
        }
    }

    private void HandleHealthChanged(float prev, float current)
    {
        // Update UI locally based on new health
        // UpdateHealthUI(current);
    }

    [ArcRPC]
    private void RPC_Die(int damageTeam, int attackingPlayerId)
    {
        gameObject.SetActive(false);
        OnStateChange?.Invoke(false);
        
        if (deathPrefab != null) Instantiate(deathPrefab, transform.position, transform.rotation);

        // Only the master client or victim registers stats to prevent double counting
        if (IsMine)
        {
            // Example: NetGameManager.Instance.RegisterKill(damageTeam, attackingPlayerId);
            Invoke(nameof(Respawn), respawnTime);
        }
    }

    private void Respawn()
    {
        if (!IsMine) return;
        
        _currentHealth.Value = maxHealth;
        arcNetView.RPC(nameof(RPC_Respawn), RpcTarget.All);
    }

    [ArcRPC]
    private void RPC_Respawn()
    {
        gameObject.SetActive(true);
        OnStateChange?.Invoke(true);
        
        // Reset position to a spawn point
        if (IsMine && NetGameManager.Instance != null)
        {
            Transform[] spawns = characterTeam == 0 ? NetGameManager.Instance.teamOneSpawnPoints : NetGameManager.Instance.teamTwoSpawnPoints;
            transform.position = spawns[UnityEngine.Random.Range(0, spawns.Length)].position;
        }
    }
}