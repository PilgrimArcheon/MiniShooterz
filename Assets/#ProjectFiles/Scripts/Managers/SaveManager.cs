using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveManager : MonoBehaviour
{
    public SaveState state;//Ref Save State

    static SaveManager instance;
    public static SaveManager Instance
    {
        get
        {
            if (!instance)
            {
                instance = new GameObject("SaveManager").AddComponent<SaveManager>();
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (instance) Destroy(gameObject);
        else instance = this;//Instance the Script

        DontDestroyOnLoad(gameObject);

        Load();//Load all info on the save state
        Debug.Log(Helper.Serialize<SaveState>(state));

        state.totalSessions = 5;//Change This
        Save();
    }

    // Save the whole state of this saveState script to the player pref
    public void Save()
    {
        PlayerPrefs.SetString("save", Helper.Serialize<SaveState>(state));
    }

    //Load Saved PlayerPrefs
    public void Load()
    {
        if (PlayerPrefs.HasKey("save"))
        {
            state = Helper.Deserialize<SaveState>(PlayerPrefs.GetString("save"));
        }
        else
        {

            state = new SaveState();
            Save();

        }
    }

    //Reset the whole save file
    public void ResetSave()
    {
        PlayerPrefs.DeleteKey("save");
        SceneManager.LoadScene(0);
    }
}
