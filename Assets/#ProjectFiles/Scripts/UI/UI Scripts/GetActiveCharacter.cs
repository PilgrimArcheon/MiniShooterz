using UnityEngine;

public class GetActiveCharacter : MonoBehaviour
{
    [SerializeField] GameObject[] Characters;
    int current;
    void OnEnable()
    {
        current = SaveManager.Instance.state.charId;
        NetworkAPIManager.Instance.UpdateProfileId();
        SetPlayerCharacter();
    }

    void SetPlayerCharacter()
    {
        foreach (var character in Characters)
        {
            character.SetActive(false);
        }

        Characters[current].SetActive(true);
    }
}