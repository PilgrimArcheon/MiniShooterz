using System;
using Unity.Netcode;
using UnityEngine;

public class ItemBox : NetworkBehaviour
{
    public int itemId;
    public int itemValue;
    public ItemType itemType;

    void OnTriggerEnter(Collider collider)
    {
        if (collider.CompareTag("Player"))
            GiveItemValue(collider.gameObject);
        else if (collider.CompareTag("AI")) SetInteractAIInput(collider.gameObject);
    }

    void GiveItemValue(GameObject character)
    {
        bool pickUp = false;

        if (itemType == ItemType.Health)
        {
            HealthSystem healthSys = character.GetComponent<HealthSystem>();
            if (healthSys.currentHealth < healthSys.maxHealth)
            {
                healthSys.IncHealthValue(itemValue);
                int playerId = healthSys.isAI ? character.GetComponent<AICharacterController>().id : character.GetComponent<PlayerCharacterController>().id;
                int team = healthSys.isAI ? character.GetComponent<AICharacterController>().aiTeam : character.GetComponent<PlayerCharacterController>().playerTeam;
                GameManager.Instance.RegisterXP(team, playerId, UnityEngine.Random.Range(50, 100));//XP per pickUp
                pickUp = true;
            }
        }
        else if (itemType == ItemType.Weapon)
        {
            CharacterShooter shooter = character.GetComponent<CharacterShooter>();
            if (shooter.currentWeaponId != itemId)
            {
                shooter.SwitchWeapon(itemId);
                int playerId = shooter.isAI ? character.GetComponent<AICharacterController>().id : character.GetComponent<PlayerCharacterController>().id;
                int team = shooter.isAI ? character.GetComponent<AICharacterController>().aiTeam : character.GetComponent<PlayerCharacterController>().playerTeam;
                GameManager.Instance.RegisterXP(team, playerId, UnityEngine.Random.Range(50, 100));//XP per pickUp
                pickUp = true;
            }
        }

        if (pickUp)
        {
            ItemSpawner.Instance.RemoveItemBox(this);
        }
    }

    void SetInteractAIInput(GameObject interactGO)
    {
        // This is called on the client when the player enters the trigger.
        float pickUpDecision = UnityEngine.Random.Range(0, 1f);
        if (pickUpDecision > 0.75f) GiveItemValue(interactGO);
    }
}

[Serializable]
public enum ItemType
{
    Health,
    Weapon
}