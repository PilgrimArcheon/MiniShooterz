using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    [SerializeField] TMP_Text positionText;
    [SerializeField] TMP_Text nameText;
    [SerializeField] TMP_Text xpText;
    [SerializeField] TMP_Text kdText;
    [SerializeField] Image characterImage;
    [SerializeField] Sprite[] charImageSprites;

    public void SetUIInfo(PlayerDetails player)
    {
        positionText.text = $"{player.PlayerPosition}.";
        nameText.text = player.PlayerName;
        xpText.text = player.PlayerXP.ToString();
        kdText.text = $"{player.PlayerKills}/{player.PlayerDeaths}";
        characterImage.sprite = charImageSprites[player.PlayerCharId];
    }
}