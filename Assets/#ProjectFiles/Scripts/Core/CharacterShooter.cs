using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class CharacterShooter : NetworkBehaviour
{
    public Weapon[] weapons;
    public int currentWeaponId;

    [Header("Shooting Settings")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public GameObject aimObjectVfx;

    public float maxDistance;
    public int fireRate = 1;
    public float shootCooldown;
    public int ammoCount = 1;
    float lastShotTime;
    public bool isAI;
    public Action OnSwitchWeapons;
    public Action<float, float> OnLoadBullets;

    int characterTeam;  // Reference to the team (Red or Blue)
    int characterId;

    public void SetCharacterShooter(int weaponId, int id, int team)
    {
        SetWeapon(weaponId);
        characterId = id;
        characterTeam = team;
    }

    void SetWeapon(int weaponId)
    {
        for (int i = 0; i < weapons.Length; i++)
        {
            if (i == weaponId)
            {
                ShowWeaponModels(weapons[i].WeaponGameObject, true);
                weapons[i].WeaponAimVfx.SetActive(true);
                bulletPrefab = weapons[i].BulletPrefab;
                maxDistance = weapons[i].bulletMaxDistance;
                ammoCount = weapons[i].ammoCount;
                shootCooldown = weapons[i].coolDown;
                fireRate = weapons[i].fireRate;
                currentWeaponId = weaponId;
            }
            else
            {
                ShowWeaponModels(weapons[i].WeaponGameObject, false);
                weapons[i].WeaponAimVfx.SetActive(false);
            }
        }
    }

    void ShowWeaponModels(GameObject[] weaponModels, bool show)
    {
        foreach (var model in weaponModels)
        {
            model.SetActive(show);
        }
    }

    public void SwitchWeapon(int weaponId)
    {
        if (IsOwner || NetcodeManager.Instance) SwitchWeaponRpc(weaponId);
    }

    public void TryShoot()
    {
        if (CanShoot)
        {
            StartCoroutine(Shoot());
            lastShotTime = Time.time;
        }
    }

    // Shoot a bullet in the given direction
    private IEnumerator Shoot()
    {
        gameObject.GetComponent<ICombat>().PerformShoot(fireRate * 0.1f);

        for (int i = 0; i < fireRate; i++)
        {
            if (IsOwner || !NetcodeManager.Instance) SpawnBulletServerRpc();
            yield return new WaitForSeconds(0.1f);
        }

        ammoCount--;
        UpdateAmmoCountRpc(ammoCount);
        TryStartReloading();
    }

    void Update()
    {
        HandleReloadAmmo();
    }

    private bool isReloading = false;
    private float reloadTimer = 0f;
    private void HandleReloadAmmo()
    {
        if (!isReloading || ammoCount >= weapons[currentWeaponId].ammoCount)
            return;

        reloadTimer += Time.deltaTime;
        OnLoadBullets.Invoke(reloadTimer, weapons[currentWeaponId].reloadTime);

        if (reloadTimer >= weapons[currentWeaponId].reloadTime)
        {
            ammoCount++;
            UpdateAmmoCountRpc(ammoCount);
            reloadTimer = 0f;

            if (ammoCount >= weapons[currentWeaponId].ammoCount)
            {
                ammoCount = weapons[currentWeaponId].ammoCount;
                isReloading = false;
            }
        }
    }

    public void TryStartReloading()
    {
        if (ammoCount < weapons[currentWeaponId].ammoCount)
        {
            isReloading = true;
            reloadTimer = 0f;
        }
    }

    public bool CanShoot { get { return ammoCount > 0 && Time.time - lastShotTime >= shootCooldown; } }
    public Weapon GetCurWeapon { get { return weapons[currentWeaponId]; } }

    [ServerRpc]
    private void SpawnBulletServerRpc() // Spawn Bullet Only on server
    {
        NetworkObject bullet = NetworkObjectPool.Instance.GetNetworkObject(bulletPrefab, firePoint.position, firePoint.rotation);

        Bullet bulletScript = bullet.GetComponent<Bullet>();
        bulletScript.SetBullet(characterTeam, characterId, weapons[currentWeaponId]);  // Assign weaponId and team to the bullet

        bullet.Spawn();
    }

    [Rpc(SendTo.Everyone)]
    private void SwitchWeaponRpc(int weaponId)
    {
        OnSwitchWeapons?.Invoke();
        SetWeapon(weaponId); // Switch the weapon
    }

    [Rpc(SendTo.Everyone)]
    private void UpdateAmmoCountRpc(int _ammoCount) => ammoCount = _ammoCount; // Update ammo count for all clients
}