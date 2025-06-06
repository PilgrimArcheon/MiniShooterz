using UnityEngine;
using TMPro;

public class PlayerTokenUI : MonoBehaviour
{
    [SerializeField] TMP_Text positionText;
    [SerializeField] TMP_Text nameText;
    [SerializeField] TMP_Text tokensText;

    public void SetUIInfo(PlayerInfo player)
    {
        positionText.text = $"{player.position}.";
        nameText.text = player.username;
        tokensText.text = $"{player.total_tokens}";
    }
}