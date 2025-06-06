using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerDetailsUI : MonoBehaviour
{
    [SerializeField] TMP_Text[] xpTexts;
    [SerializeField] TMP_Text[] nameTexts;
    [SerializeField] TMP_Text[] tokensTexts;
    [SerializeField] TMP_Text[] killsTexts;
    [SerializeField] TMP_Text[] walletAddressTexts;
    [SerializeField] Image[] charImages;
    [SerializeField] Sprite[] charImageSprites;

    void Update()
    {
        if (!NetworkAPIManager.Instance.isLoggedIn) return;

        if (xpTexts.Length > 0) TextToShow(xpTexts, "XP: " + SaveManager.Instance.state.totalXP.ToString());
        if (nameTexts.Length > 0) TextToShow(nameTexts, SaveManager.Instance.state.userName.ToString());
        if (tokensTexts.Length > 0) TextToShow(tokensTexts, "Tokens: " + SaveManager.Instance.state.tokens.ToString());
        if (walletAddressTexts.Length > 0) TextToShow(walletAddressTexts, "Wallet Address: " + SaveManager.Instance.state.walletAddress.ToString());
        if (killsTexts.Length > 0) TextToShow(killsTexts, SaveManager.Instance.state.kills.ToString());
        if (charImages.Length > 0) ImageToShow(charImages, SaveManager.Instance.state.charId);
    }

    void TextToShow(TMP_Text[] textArray, string text)
    {
        foreach (var textToShow in textArray)
        {
            textToShow.text = text;
        }
    }

    void ImageToShow(Image[] images, int charId)
    {
        foreach (var image in images)
        {
            image.sprite = charImageSprites[charId];
        }
    }
}