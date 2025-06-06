using UnityEngine;

public class Weapon : MonoBehaviour
{
    public GameObject[] WeaponGameObject;
    public GameObject WeaponAimVfx;
    public GameObject BulletPrefab;
    public float bulletMaxDistance = 15f;
    public float bulletSpeed = 20f;
    public int ammoCount = 3;
    public float reloadTime = 1f;
    public int fireRate = 3;
    public float coolDown = 1f;
}