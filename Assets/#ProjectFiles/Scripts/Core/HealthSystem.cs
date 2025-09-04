using System;
using UnityEngine;

public class HealthSystem : MonoBehaviour
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
        if (!GameManager.Instance.IsMultiplayer)
        {
            UpdateHealth(currentHealth);
            SpawnHealthBuff();
        }
    }

    // Take damage and handle death (with team check)
    public void TakeDamage(float amount, int damageTeam, int playerId)
    {
        if (damageTeam != characterTeam)  // Only take damage from the opposite team
        {
            currentHealth -= amount;
            if (!GameManager.Instance.IsMultiplayer) { UpdateHealth(currentHealth); }// Update health on client
            if (currentHealth <= 0) Die(damageTeam, playerId);
        }
    }

    // Handle the character's death
    private void Die(int damageTeam, int playerId)
    {
        DoDeathServer(); 

        GameManager.Instance.RegisterKill(damageTeam, playerId); // Register kill for team
        GameManager.Instance.RegisterDeath(characterTeam, id); // Register kill for team
        GameManager.Instance.RegisterXP(damageTeam, playerId, UnityEngine.Random.Range(120, 200));//XP per pickUp
        GameManager.Instance.Respawn(transform, characterTeam); // Respawn character

        Invoke(nameof(Respawn), respawnTime);
    }

    // Respawn character at the spawn point
    private void Respawn()
    {
        currentHealth = maxHealth;
        OnStateChange.Invoke(true);

        ShowChar(true);

        gameObject.SetActive(true);  // Reactivate character
        transform.parent = null;
    }


    void UpdateHealth(float health) => currentHealth = health;

    void ShowChar(bool show) => gameObject.SetActive(show);

    private void DoDeathServer()
    {
        gameObject.SetActive(false);  // Disable character

        OnStateChange.Invoke(false);

        Instantiate(deathPrefab, transform.position, transform.rotation);

        ShowChar(false);

        AudioManager.Instance.PlaySfx(SoundEffect.Death, transform.position);
    }

    private void SpawnHealthBuff()
    {
        Instantiate(healthBuffPrefab, transform.position, transform.rotation);

        AudioManager.Instance.PlaySfx(SoundEffect.Health, transform.position);
    }
}