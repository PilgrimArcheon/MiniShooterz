using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;

public class PlayerDataCheck : MonoBehaviour
{
    [SerializeField] DataType dataToCheck;
    bool dataIsCorrect
    { get { return dataToCheck == DataType.socialMedia; } }
    [SerializeField] string dataValue;
    [SerializeField] bool addDataToCheck;
    [SerializeField] Indexing indexingType;
    [SerializeField] string dataToCheckString;
    [SerializeField] Button[] socialMediaButtons;

    UserData userData;
    TMP_Text dataText;
    string dataString;


    void OnEnable()
    {
        userData = SaveManager.Instance.state.userData;

        if (dataToCheck != DataType.socialMedia)
            dataText = GetComponent<TMP_Text>();
    }

    void Update()
    {
        dataText.text = "--------------";
        
        if (!NetworkAPIManager.Instance.isLoggedIn) return;
        
        if (dataToCheck == DataType.socialMedia)
        {
            // socialMediaButtons[0].interactable = !SaveManager.Instance.state.refShared.facebookShared; //!string.IsNullOrEmpty(userData.referral_links.facebook) 
            // socialMediaButtons[1].interactable = !SaveManager.Instance.state.refShared.x_Shared; //!string.IsNullOrEmpty(userData.referral_links.X)
            // socialMediaButtons[2].interactable = !SaveManager.Instance.state.refShared.discordShared; //!string.IsNullOrEmpty(userData.referral_links.discord)
            return;
        }

        if (gameObject.activeInHierarchy && dataText)
        {
            if (dataToCheck == DataType.agentName)
            {
                dataText.text = SaveManager.Instance?.state.charId.ToString();
                return;
            }
            string dataJson = JsonUtility.ToJson(userData);

            IDictionary dataDictionary = (IDictionary)TextTool.ConvertJsonToDictionary(dataJson);

            string dataCheck = dataToCheck.ToString();
            dataString = dataDictionary[dataCheck].ToString();
            string indexingString = indexingType == Indexing.Paragraph ? "\n": " ";
            string addDataCheck = addDataToCheck ? $"{dataToCheckString}:{indexingString}" : "";
            // if (dataCheck.Contains("link"))
            //     dataString = userData.referral_link = "https://t.me/CaskGuardTestBot?startapp=" + userData.referral_code;
            dataText.text = $"{addDataCheck}{dataString} {dataValue}";
        }
    }

    public void CopyDataText() => TextTool.CopyTextToClipboard(dataString);

    // public void OpenFacebookUrlLink() { Application.OpenURL("https://www.facebook.com/share/1KTQGuyNhV/?mibextid=LQQJ4d"); GetReward("facebook"); NetworkAPIManager.Instance?.GetUser(); } //userData.referral_links.facebook

    // public void OpenDiscordUrlLink() { Application.OpenURL("https://linktr.ee/digicaskfinance"); GetReward("discord"); NetworkAPIManager.Instance?.GetUser(); } // userData.referral_links.discord

    // public void OpenTwitterUrlLink() { Application.OpenURL("https://x.com/digicaskfinance?s=21"); GetReward("X"); NetworkAPIManager.Instance?.GetUser(); } // userData.referral_links.X 

    // public void GetReward(string platform)
    // {
    //     NetworkAPIManager.Instance?.UpdateStats("total_tokens", 500);
    //     switch (platform)
    //     {
    //         case "facebook":
    //             SaveManager.Instance.state.refShared.facebookShared = true;
    //             break;
    //         case "X":
    //             SaveManager.Instance.state.refShared.x_Shared = true;
    //             break;
    //         case "discord":
    //             SaveManager.Instance.state.refShared.discordShared = true;
    //             break;
    //         default:
    //             break;
    //     }
    //     SaveManager.Instance?.Save();
    // }
}

public enum Indexing
{
    StraightLine,
    Paragraph
}

public enum DataType
{
    id,
    username,
    agentName,
    total_kills,
    total_sessions,
    wallet_address,
    total_tokens,
    total_xp,
    onchain_shares,
    referrer_id,
    banned,
    created_at,
    socialMedia
}