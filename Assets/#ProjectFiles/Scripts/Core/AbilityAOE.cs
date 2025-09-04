using UnityEngine;

public class AbilityAOE : MonoBehaviour
{
    [SerializeField] AudioClip audioClip;
    public int team;  // Team tag for bullet (Red or Blue)
    public int playerId;
    public float damage = 35f;
    public float lifeTime;
    float deathTime;

    public void SetAbilityAOE(int _team, int _playerId, float dmg)
    {
        team = _team;
        playerId = _playerId;
        damage = dmg;

        deathTime = Time.time + lifeTime;
    }

    bool hasCollided;
    void Update()
    {
        if (!hasCollided) return;

        Collider[] colliders = Physics.OverlapSphere(transform.position, 5f);

        foreach (var c in colliders)
        {
            if (c.gameObject.CompareTag("Player") || c.gameObject.CompareTag("AI"))
            {
                HealthSystem targetHealth = c.GetComponent<HealthSystem>();
                if (targetHealth != null && targetHealth.characterTeam != team)
                {
                    Debug.Log("DEALT DAMAGE");
                    targetHealth.TakeDamage(damage, team, playerId);  // Damage if opposite team
                    deathTime = Time.time - 1f;

                    AudioManager.Instance.PlaySoundEfx(audioClip, transform.position);
                }
            }
        }

        if (deathTime < Time.time)
            Destroy(gameObject);
    }

    void OnTriggerEnter(Collider collider) => hasCollided = true;
}