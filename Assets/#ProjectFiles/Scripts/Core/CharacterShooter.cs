using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CharacterShooter : MonoBehaviour
{
    public Weapon[] weapons;
    public Ability[] characterAbilities;
    public Ability ability;
    public int currentWeaponId;

    [Header("Shooting Settings")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float maxDistance;
    public int fireRate = 1;
    public float shootCooldown;
    public int ammoCount = 1;
    float lastShotTime;
    [Header("Ability Settings")]
    [SerializeField] Image abilityCoolDown;
    float lastAbilityTime;
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
        ability = characterAbilities[SaveManager.Instance.state.charId];
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
        OnSwitchWeapons?.Invoke();
        SetWeapon(weaponId); // Switch the weapon
    }

    public void TryShoot()
    {
        if (CanShoot)
        {
            StartCoroutine(Shoot());
            lastShotTime = Time.time;
        }
    }

    public void TryUseAbility()
    {
        if (CanUseAbility)
        {
            StartCoroutine(PerformAbility());
            lastAbilityTime = Time.time;
            OnUseAbility(lastAbilityTime, ability.coolDown);
        }
    }

    // Shoot a bullet in the given direction
    private IEnumerator Shoot()
    {
        gameObject.GetComponent<ICombat>().PerformShoot(fireRate * 0.1f);

        for (int i = 0; i < fireRate; i++)
        {
            SpawnBullet();
            yield return new WaitForSeconds(0.1f);
        }

        ammoCount--;
        UpdateAmmoCount(ammoCount);
        TryStartReloading();
    }

    private IEnumerator PerformAbility()
    {
        yield return new WaitForSeconds(ability.spawnTime);
        gameObject.GetComponent<ICombat>().PerformAbility();
        Transform spawnPoint = ability.AbilitySpawnPoint;
        AbilityAOE abilityAOE = Instantiate(ability.AbilityPrefab, spawnPoint.position, Quaternion.identity).GetComponent<AbilityAOE>();
        abilityAOE.SetAbilityAOE(characterTeam, characterId, ability.abilityDamage);
    }

    void Update()
    {
        HandleReloadAmmo();
        UpdateAbilityUseCoolDown();
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
            UpdateAmmoCount(ammoCount);
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

    public bool CanUseAbility { get { return Time.time - lastAbilityTime >= ability.coolDown; } }
    public bool CanShoot { get { return ammoCount > 0 && Time.time - lastShotTime >= shootCooldown; } }
    public Weapon GetCurWeapon { get { return weapons[currentWeaponId]; } }

    private void SpawnBullet()
    {
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        Bullet bulletScript = bullet.GetComponent<Bullet>();
        bulletScript.SetBullet(characterTeam, characterId, weapons[currentWeaponId]);
    }

    bool abilityCoolingDown;
    float abilityUsedTime;
    float abilityCoolDownTime;
    void OnUseAbility(float time, float totalTime)
    {
        abilityUsedTime = time;
        abilityCoolDownTime = totalTime;
        abilityCoolingDown = true;
    }

    void UpdateAbilityUseCoolDown()
    {
        if (!abilityCoolingDown) return;

        float fillAmount = 0;
        if (!isAI || abilityCoolDown)
        {
            fillAmount = (Time.time - abilityUsedTime) / abilityCoolDownTime;
            abilityCoolDown.fillAmount = fillAmount;
        }

        if (fillAmount >= 1)
        {
            abilityCoolDown.fillAmount = 1;
            abilityCoolingDown = false;
        }
    }


    private void UpdateAmmoCount(int _ammoCount) => ammoCount = _ammoCount;
}