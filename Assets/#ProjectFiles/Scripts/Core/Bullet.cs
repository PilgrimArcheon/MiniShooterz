using System;
using System.Collections;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Bullet : NetworkBehaviour
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
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            started = true;
            lifeTime = Time.fixedTime + (ownerWeapon.bulletMaxDistance / ownerWeapon.bulletSpeed);

            NetworkObject flashHitEfx = NetworkObjectPool.Instance.GetObject(flashHitEffectPrefab, transform.position, transform.rotation);
            flashHitEfx.Spawn();
        }

        if (IsClient)
        {
            AudioManager.Instance.PlaySfx(SoundEffect.Shoot, transform.position);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer) started = false;// Reset started flag
    }

    public void SetBullet(int _team, int _playerId, Weapon weapon)
    {
        team = _team;
        playerId = _playerId;
        ownerWeapon = weapon;

        rigidbody = GetComponent<Rigidbody>();
        rigidbody.velocity = rigidbody.transform.forward * weapon.bulletSpeed;
    }

    void FixedUpdate()
    {
        if (!started) return;

        if (lifeTime < Time.time)
        {
            // Time to return to the pool from whence it came.
            var networkObject = gameObject.GetComponent<NetworkObject>();
            networkObject.Despawn();
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

                if (IsServer && IsSpawned)
                {
                    NetworkObject bulletHitEfx = NetworkObjectPool.Instance.GetObject(bulletHitEffect, transform.position, transform.rotation);
                    bulletHitEfx.Spawn();

                    rigidbody.velocity = Vector3.zero;
                    lifeTime = Time.time - 1f;
                }

                AudioManager.Instance.PlaySfx(SoundEffect.BulletDrop, transform.position);
            }
        }
    }
}