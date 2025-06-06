using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Leaderboard : MonoBehaviour
{
    [SerializeField] GameObject playerUIDataPrefab;
    [SerializeField] Transform leaderboardParent;
    [SerializeField] Section[] lb_sections;
    [SerializeField] Color activeColor;
    [SerializeField] Color inactiveColor;

    void OnEnable()
    {
        ResetBoard();
        //if (!NetworkAPIManager.Instance.isLoggedIn) return;
        //StartCoroutine(NetworkAPIManager.Instance?.GetLeaderboardInfo(this)); 
        NetworkAPIManager.Instance.GetLeaderboard(this);
    }

    void ResetBoard()
    {
        foreach (Transform child in leaderboardParent)
        {
            Destroy(child.gameObject);
        }
    }
    
    public void OnSelectSection(string sectionName)
    {
        foreach (var section in lb_sections)
        {
            if (section.sectionName == sectionName)
            {
                section.active = true;
                section.UpdateSectionColor(activeColor, section.sectionText.color);
            }
            else
            {
                section.active = false;
                section.UpdateSectionColor(inactiveColor, section.sectionText.color);
            }
        }
    }

    public void UpdateLeaderBoard(List<PlayerDetails> playerData)
    {
        ResetBoard();
        
        for (int i = 0; i < playerData.Count; i++)
        {
            PlayerUI playerUI = Instantiate(playerUIDataPrefab, leaderboardParent).GetComponent<PlayerUI>();
            playerUI.SetUIInfo(playerData[i]);
        }
    }
}

[Serializable]
public class Section
{
    public string sectionName;
    public Image sectionImage;
    public TMP_Text sectionText;
    public bool active;

    public void UpdateSectionColor(Color imageColor, Color textColor)
    {
        if (sectionText) sectionText.color = textColor;
        sectionImage.color = imageColor;
    }
}