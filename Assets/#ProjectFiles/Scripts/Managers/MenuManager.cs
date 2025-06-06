using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using System.Runtime.InteropServices;
using Bera;
using UnityEngine.UI;
using TMPro;
using System;

public class MenuManager : MonoBehaviour, IPointerUpHandler
{
    [DllImport("__Internal")]
    private static extern bool IsMobile();
    public static MenuManager Instance;//Instance Menu Script
    public SettingItem[] settingItems;
    [SerializeField] Menu[] menus;//Ref All Menus in the Scene
    [SerializeField] Menu currentMenu;
    [SerializeField] GameObject[] controls;

    void Awake()
    {
        Instance = this;//Make this an Instance
        menus = FindObjectsOfType<Menu>();
        Time.timeScale = 1;
        foreach (var menu in menus)
        {
            if (!menu.open) menu.gameObject.SetActive(false);
        }

        NetworkAPIManager.Instance.GetUser();
    }

    void Start()
    {
        UpdateSettings();

        if (!SaveManager.Instance.state.hasConnectedCred
            && SceneManager.GetActiveScene().name.Contains("Menu"))
            OpenMenu("playAs-Overlay");

        SaveManager.Instance.state.hasConnectedCred = true;
        SaveManager.Instance.Save();

        if (controls == null) return;

        if (controls.Length > 0)
        {
            controls[0].SetActive(!IsWebMobile());
            controls[1].SetActive(IsWebMobile());
        }
    }

    public void OpenMenu(string menuName)//To Open Menu
    {
        for (int i = 0; i < menus.Length; i++)
        {
            if (menus[i].menuName == menuName)
            {
                AudioManager.Instance.PlayUISoundFX(UISoundFx.Confirm);
                menus[i].Open();
            }
            else if (menus[i].open)
            {
                if (!menuName.Contains("Overlay")) CloseMenu(menus[i]);
            }
        }
    }

    public void OpenMenu(Menu menu)//Confirm Menu Opened
    {
        for (int i = 0; i < menus.Length; i++)
        {
            if (menus[i].open)
            {
                CloseMenu(menus[i]);//Close every othe menu
            }
        }
        menu.Open();//Call the Menu Open Function
    }

    public void CloseMenu(Menu menu)//Close the Menu
    {
        menu.Close();
    }

    public void UpdateVolumeSettings(int settingId)
    {
        for (int i = 0; i < settingItems.Length; i++)
        {
            if (i == settingId)
            {
                SaveManager.Instance.state.volumeSettings[i] = settingItems[i].volumeSlider.value;
                SaveManager.Instance.Save();
            }
        }

        UpdateSettings();
    }

    void UpdateSettings()
    {
        if (settingItems.Length <= 0) return;

        for (int i = 0; i < settingItems.Length; i++)
        {
            settingItems[i].volumeSlider.value = SaveManager.Instance.state.volumeSettings[i];
            settingItems[i].volumeText.text = $"{Mathf.FloorToInt(SaveManager.Instance.state.volumeSettings[i] * 100)}";
        }
    }

    public void Play(string Game)
    {
        SceneManager.LoadScene(Game);
    }

    public void MainMenu()//Got to Main Menu Scene
    {
        SceneManager.LoadScene("MenuScene");
    }

    public void Pause() => Time.timeScale = 0f;

    public void Resume() => Time.timeScale = 1f;

    public void Restart()//Restart the Game
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);//Activate the Game Scene Again
    }

    public void OpenUrlLink(string url)
    {
        Application.OpenURL(url);
    }

    public void Quit()//Quit the Game App
    {
        Application.Quit();
    }

    public void ResetSave()
    {
        SaveManager.Instance?.ResetSave();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        throw new System.NotImplementedException();
    }

    public bool IsWebMobile()
    {
#if !UNITY_EDITOR && UNITY_WEBGL
            return IsMobile();
#endif
        return false;
    }
}


[System.Serializable]
public class SettingItem
{
    public Slider volumeSlider;
    public TMP_Text volumeText;
}