using System;
using Unity.Netcode;
using UnityEngine;

public class HealthSystem : NetworkBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth;

    [SerializeField] GameObject healthBuffPrefab;
    [SerializeField] GameObject deathPrefab;

    public Action<bool> OnStateChange;

    [Header("Respawn Settings")]
    public float respawnTime = 3f;
    public int id;
    public int characterTeam;  // Team reference
    public bool isAI;

    private void Start()
    {
        currentHealth = maxHealth;
        transform.parent = null;
    }

    public void SetUpHealth(int _id, int _team)
    {
        id = _id;
        characterTeam = _team;
    }

    // Increment health by a certain amount
    public void IncHealthValue(float incHealth)
    {
        currentHealth += incHealth; // Add health to current health
        if (currentHealth > maxHealth) currentHealth = maxHealth; // Clamp health to maxHealth
        if (IsOwner || !NetcodeManager.Instance)
        {
            UpdateHealthRpc(currentHealth);
            SpawnHealthBuffServerRpc();
        }// Update health on client

        Debug.Log("Inc Health");
    }

    // Take damage and handle death (with team check)
    public void TakeDamage(float amount, int damageTeam, int playerId)
    {
        if (damageTeam != characterTeam)  // Only take damage from the opposite team
        {
            currentHealth -= amount;
            // gameObject.GetComponent<ICombat>().TakeDamage();
            if (IsOwner || !NetcodeManager.Instance) { UpdateHealthRpc(currentHealth); }// Update health on client
            if (currentHealth <= 0) Die(damageTeam, playerId);
        }
    }

    // Handle the character's death
    private void Die(int damageTeam, int playerId)
    {
        if (IsOwner || !NetcodeManager.Instance) DoDeathServerRpc(); // Spawn death on server

        GameManager.Instance.RegisterKill(damageTeam, playerId); // Register kill for team
        GameManager.Instance.RegisterDeath(characterTeam, id); // Register kill for team
        GameManager.Instance.RegisterXP(damageTeam, playerId, UnityEngine.Random.Range(120, 200));//XP per pickUp
        GameManager.Instance.Respawn(transform, characterTeam); // Respawn character

        if (IsOwner || !NetcodeManager.Instance) Invoke(nameof(Respawn), respawnTime);  // Respawn after delay
    }

    // Respawn character at the spawn point
    private void Respawn() => DoRespawnServerRpc();

    [Rpc(SendTo.Everyone)]
    void UpdateHealthRpc(float health) => currentHealth = health;

    [Rpc(SendTo.Everyone)]
    void ShowCharRpc(bool show) => gameObject.SetActive(show);

    [ServerRpc]
    private void DoDeathServerRpc()
    {
        gameObject.SetActive(false);  // Disable character

        OnStateChange.Invoke(false);

        //GameObject deathEffect = Instantiate(deathPrefab, transform.position, transform.rotation);
        NetworkObject deathEffect = NetworkObjectPool.Instance.GetObject(deathPrefab, transform.position, transform.rotation);
        deathEffect.Spawn();

        ShowCharRpc(false);

        AudioManager.Instance.PlaySfx(SoundEffect.Death, transform.position);

        // if (NetcodeManager.Instance)
        //     NetcodeManager.Instance.SpawnNetObject(deathEffect);
    }

    [ServerRpc]
    private void SpawnHealthBuffServerRpc()
    {
        //GameObject healthBuffEffect = Instantiate(healthBuffPrefab, transform.position, transform.rotation);
        NetworkObject healthBuffEffect = NetworkObjectPool.Instance.GetObject(healthBuffPrefab, transform.position, transform.rotation);
        healthBuffEffect.Spawn();

        AudioManager.Instance.PlaySfx(SoundEffect.Health, transform.position);

        // if (NetcodeManager.Instance)
        //     NetcodeManager.Instance.SpawnNetObject(healthBuffEffect);
    }

    [ServerRpc]
    private void DoRespawnServerRpc()
    {
        currentHealth = maxHealth;
        OnStateChange.Invoke(true);

        if (IsOwner) ShowCharRpc(true);

        gameObject.SetActive(true);  // Reactivate character
        transform.parent = null;
    }
}