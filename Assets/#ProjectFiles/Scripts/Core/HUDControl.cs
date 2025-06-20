using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class HUDControl : NetworkBehaviour
{
    private bool hasSetUp;
    private GameObject hudUI;
    private GameObject bulletCountHolder;
    private List<Image> bulletCountUi = new();
    private Slider healthSlider;
    private TMP_Text userName;
    public bool isAI;
    public int characterTeam;  // Team reference

    HealthSystem healthSystem;
    CharacterShooter characterShooter;

    void Start()
    {
        healthSystem = GetComponent<HealthSystem>();
        characterShooter = GetComponent<CharacterShooter>();

        healthSystem.OnStateChange += OnStateChange; // Set up State Change Event
        characterShooter.OnLoadBullets += OnLoadBullets;// Set up Load Bullets Event
        characterShooter.OnSwitchWeapons += OnSwitchWeapons; // Set up Weapon Switch Event
    }

    private void OnSwitchWeapons()
    {
        StartCoroutine(DoSwitch());

        IEnumerator DoSwitch()
        {
            bulletCountHolder.GetComponent<HorizontalLayoutGroup>().enabled = true;
            yield return new WaitForSeconds(0.25f);
            bulletCountHolder.GetComponent<HorizontalLayoutGroup>().enabled = false;
        }
    }

    private void OnStateChange(bool show) { hudUI.SetActive(show); Debug.Log("Health State Changed"); }

    public void SetUpHUD(string userId, int _characterTeam)
    {
        characterTeam = _characterTeam;

        if (hasSetUp || GameManager.Instance.gameStarted) return;

        hudUI = Instantiate(Resources.Load<GameObject>("HUD"), MenuManager.Instance.transform);
        hudUI.transform.SetParent(GameObject.Find("GameUI").transform);
        hudUI.name += $"{gameObject.name}";

        if (GameManager.Instance.gameStarted) ClearUI();

        Transform hudTransform = hudUI.transform;

        userName = hudTransform.GetChild(0).GetComponent<TMP_Text>();
        healthSlider = hudTransform.GetChild(1).GetComponent<Slider>();
        bulletCountHolder = hudTransform.GetChild(2).gameObject;
        OnSwitchWeapons();

        foreach (Transform child in bulletCountHolder.transform) { bulletCountUi.Add(child.GetChild(0).GetComponent<Image>()); }

        userName.text = userId;
        hasSetUp = true;
    }

    void Update()
    {
        UpdateHealthDetails();
        UserDetailsUpdate();
        UpdateShooterBulletCount();
        UpdateHUDUIPosition();
    }

    private void UpdateHUDUIPosition()
    {
        if (hudUI == null) return;

        hudUI.transform.position = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 5f);
        hudUI.SetActive(healthSystem.currentHealth > 0);
    }

    private void UpdateHealthDetails()
    {
        if (healthSlider == null) return;

        healthSlider.value = healthSystem.currentHealth / healthSystem.maxHealth;
        healthSlider.transform.GetChild(1).GetChild(0).GetComponent<Image>().color = characterTeam == GameManager.Instance.GetMyTeam ? Color.cyan : Color.red;
        if (IsOwner && !isAI) healthSlider.transform.GetChild(1).GetChild(0).GetComponent<Image>().color = Color.green;
    }

    private void UserDetailsUpdate()
    {
        if (userName == null) return;
        userName.gameObject.SetActive(characterTeam == GameManager.Instance.GetMyTeam);
    }

    float bulletFillAmount = 1f;
    private void OnLoadBullets(float currentValue, float loadValue)
    {
        if (bulletCountHolder == null) return;

        // Current bullet's reload progress (0 to 1)
        bulletFillAmount = Mathf.Clamp01(currentValue / loadValue);
    }

    private void UpdateShooterBulletCount()
    {
        if (bulletCountHolder == null) return;

        bulletCountHolder.SetActive(IsOwner && !isAI);

        int currentAmmo = characterShooter.ammoCount;
        int maxAmmo = characterShooter.GetCurWeapon.ammoCount;

        for (int i = 0; i < bulletCountUi.Count; i++)
        {
            if (i < currentAmmo)
            {
                // Fully loaded bullets
                bulletCountUi[i].fillAmount = 1f;
            }
            else if (i == currentAmmo && currentAmmo < maxAmmo)
            {
                // Currently reloading bullet (partial fill)
                bulletCountUi[i].fillAmount = bulletFillAmount;
            }
            else
            {
                // Empty bullets
                bulletCountUi[i].fillAmount = 0f;
            }
        }
    }

    public void ClearUI()
    {
        if (hudUI != null) Destroy(hudUI);
    }

    public string GetUserName { get { return userName.text; } }
    public int GetCharId { get { return isAI ? GetComponent<AICharacterController>().charId : GetComponent<PlayerCharacterController>().charId; } }
}