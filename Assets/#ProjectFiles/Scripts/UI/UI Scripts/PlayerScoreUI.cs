using UnityEngine;
using TMPro;

public class PlayerScoreUI : MonoBehaviour
{
    [SerializeField] TMP_Text positionText;
    [SerializeField] TMP_Text nameText;
    [SerializeField] TMP_Text pointsText;

    public void SetUIInfo(PlayerInfo player)
    {
        positionText.text = $"{player.position}.";
        nameText.text = player.username;
        pointsText.text = player.total_score.ToString();
    }
}