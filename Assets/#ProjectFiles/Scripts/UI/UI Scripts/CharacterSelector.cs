using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class CharacterSelector : MonoBehaviour
{
    public Transform turntable;                  // Rotating parent object
    public List<Transform> characterTrans;           // Child characters
    public float rotationAngle = 120f;           // 360 / totalCharacters
    public float rotationDuration = 0.5f;        // Animation duration
    [SerializeReference] Transform referenceTrans;
    [SerializeField] GameObject[] Characters;
    [SerializeField] GameObject[] CharacterWeapons;
    [SerializeField] GameObject[] CharactersUIshow;
    [SerializeField] CharacterButton[] characterButtons;
    [SerializeField] Animator[] characterAnimators;
    [SerializeField] Button selectedButton;

    public Color normalColor = new(0.3f, 0.6f, 1f);   // Blue
    public Color selectedColor = new(1f, 0.2f, 0.6f); // Pink

    //string[] Names = { "Alien", "Ninja", "Hazmat" };
    // int[] available = { 1, 1, 1, };
    int current;

    void Start()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 50;

        current = SaveManager.Instance.state.charId;

        for (int i = 0; i < characterButtons.Length; i++)
        {
            int index = i; // Cache for lambda
            characterButtons[i].button.onClick.AddListener(() => OnCharacterSelected(index));
        }

        selectedButton.onClick.AddListener(() =>
        {
            SaveManager.Instance.state.charId = current;
            PersistentGameData.characterChoice = current;
            AudioManager.Instance.PlayUISoundFX(UISoundFx.Click);
            SaveManager.Instance.Save();
        });

        // Initialize first selection
        OnCharacterSelected(current);
    }
    void OnEnable()
    {
        CharactersUIshow[0].SetActive(true);
        CharactersUIshow[1].SetActive(false);
    }

    void OnDisable()
    {
        CharactersUIshow[1].SetActive(true);
        CharactersUIshow[0].SetActive(false);
    }

    void OnCharacterSelected(int index)
    {
        current = index;
        ApplySelectionVisuals(index);
        AudioManager.Instance.PlayUISoundFX(UISoundFx.Click);
        Choose();
    }

    void Update()
    {
        foreach (var character in Characters)
        {
            character.transform.forward = referenceTrans.forward;
        }
    }

    void Choose()
    {
        SelectCharacterByIndex();
    }

    public void SelectCharacterByIndex(bool instant = false)
    {
        float targetY = -current * rotationAngle;

        if (instant)
            turntable.localRotation = Quaternion.Euler(0f, targetY, 0f);
        else
            turntable.DOLocalRotate(new Vector3(0f, targetY, 0f), rotationDuration).SetEase(Ease.OutBack);

        PlaySelectAnimation();
        HighlightSelectedCharacter();
    }

    private void PlaySelectAnimation()
    {
        for (int i = 0; i < characterAnimators.Length; i++)
        {
            if (i == current)
            {
                CharacterWeapons[i].SetActive(false);
                characterAnimators[i].SetFloat("charId", current);
                characterAnimators[i].Play("Selected");
            }
            else
            {
                characterAnimators[i].SetFloat("charId", 0);
                characterAnimators[i].Play("Idle", 0);
                CharacterWeapons[i].SetActive(true);
            }
        }
    }

    private void HighlightSelectedCharacter()
    {
        for (int i = 0; i < characterTrans.Count; i++)
        {
            float targetScale = (i == current) ? 1.2f : 1f;
            characterTrans[i].DOScale(Vector3.one * targetScale, 0.3f);
        }
    }

    void ApplySelectionVisuals(int selectedIndex, bool instant = false)
    {
        for (int i = 0; i < characterButtons.Length; i++)
        {
            bool isSelected = i == selectedIndex;

            var btn = characterButtons[i];
            var targetScale = isSelected ? 1.25f : 1f;
            var targetColor = isSelected ? selectedColor : normalColor;

            if (instant)
            {
                btn.backgroundTransform.localScale = Vector3.one * targetScale;
                btn.backgroundImage.color = targetColor;
            }
            else
            {
                btn.backgroundTransform.DOScale(Vector3.one * targetScale, 0.3f).SetEase(Ease.OutBack);
                btn.backgroundImage.DOColor(targetColor, 0.3f);
            }
        }
    }

    // int mod(int x, int m)
    // {
    //     int r = x % m;
    //     return r < 0 ? r + m : r;
    // }
}

public static class PersistentGameData
{
    public static int characterChoice;
}

[System.Serializable]
public class CharacterButton
{
    public Button button;
    public RectTransform backgroundTransform; // For scale
    public Image backgroundImage;             // For color
}
