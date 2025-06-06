using UnityEngine;

public class CharacterSelectSequence : MonoBehaviour
{
    [SerializeField] Animator animator;
    [SerializeField] Weapon[] weapons;
    int currentWeaponid;

    void OnEnable()
    {
        SetWeapon(SaveManager.Instance.state.charId);
    }

    void Update()
    {
        animator.SetFloat("charId", currentWeaponid);
    }

    void SetWeapon(int weaponId)
    {
        for (int i = 0; i < weapons.Length; i++)
        {
            if (i == weaponId)
            {
                ShowWeaponModels(weapons[i].WeaponGameObject, true);
                weapons[i].WeaponAimVfx.SetActive(true);
            }
            else
            {
                ShowWeaponModels(weapons[i].WeaponGameObject, false);
                weapons[i].WeaponAimVfx.SetActive(false);
            }
        }

        currentWeaponid = weaponId;
    }

    void ShowWeaponModels(GameObject[] weaponModels, bool show)
    {
        foreach (var model in weaponModels)
        {
            model.SetActive(show);
        }
    }
}