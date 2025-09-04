using UnityEngine;

public class Ability : MonoBehaviour
{
    public GameObject AbilityAimVfx;
    public GameObject AbilityPrefab;
    public Transform AbilitySpawnPoint;
    public float areaOfEffectMaxDistance = 15f;
    public float spawnTime;
    public float coolDown = 1f;
    public float abilityDamage = 10f;
}