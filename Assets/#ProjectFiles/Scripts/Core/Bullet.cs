using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Bullet : MonoBehaviour
{
    public int team;  // Team tag for bullet (Red or Blue)
    public int playerId;
    public float damage = 10;
    public float lifeTime;
    new Rigidbody rigidbody;
    Weapon ownerWeapon;
    [SerializeField] GameObject flashHitEffectPrefab;
    [SerializeField] GameObject bulletHitEffect;

    bool started;
    public void InitBullet()
    {
        started = true;
        lifeTime = Time.fixedTime + (ownerWeapon.bulletMaxDistance / ownerWeapon.bulletSpeed);

        Instantiate(flashHitEffectPrefab, transform.position, transform.rotation);
        AudioManager.Instance.PlaySfx(SoundEffect.Shoot, transform.position);
    }

    public void SetBullet(int _team, int _playerId, Weapon weapon)
    {
        team = _team;
        playerId = _playerId;
        ownerWeapon = weapon;

        InitBullet();

        rigidbody = GetComponent<Rigidbody>();
        rigidbody.linearVelocity = rigidbody.transform.forward * weapon.bulletSpeed;
    }

    void FixedUpdate()
    {
        if (!started) return;

        if (lifeTime < Time.time)
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnTriggerEnter(Collider collider)
    {
        // Only deal damage to objects of opposite teams
        if (collider.CompareTag("Player") || collider.CompareTag("AI"))
        {
            HealthSystem targetHealth = collider.GetComponent<HealthSystem>();
            if (targetHealth != null && targetHealth.characterTeam != team)
            {
                targetHealth.TakeDamage(damage, team, playerId);  // Damage if opposite team

                Instantiate(bulletHitEffect, transform.position, transform.rotation);

                rigidbody.linearVelocity = Vector3.zero;
                lifeTime = Time.time - 1f;

                AudioManager.Instance.PlaySfx(SoundEffect.BulletDrop, transform.position);
            }
        }
    }
}