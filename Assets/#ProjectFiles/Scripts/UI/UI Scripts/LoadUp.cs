using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using TMPro;
using UnityEngine.SceneManagement;


public class LoadUp : MonoBehaviour
{
    [SerializeField] Image img;// image for loading up
    [SerializeField] TMP_Text loadingText;//Text to show loading Percentage
    public string sceneToLoad = "MenuScene";
    public float loadSpeed = 0.5f;
    [SerializeField] bool IsNetworkCode;
    bool startScene;
    float a;

    void OnEnable()
    {
        startScene = false;
        if (img) a = img.fillAmount;

        IsNetworkCode = NetcodeManager.Instance;

        if (IsNetworkCode) NetcodeManager.Instance.HostPlayer.LoadMenu(sceneToLoad);

    }

    public void UpdateSceneToOpen(string newSceneToOpen)
    {
        sceneToLoad = newSceneToOpen;
    }

    void Update()
    {        
        a += Time.deltaTime * loadSpeed;
        if (img) img.fillAmount = a;
        if (loadingText) loadingText.text = ((int)(img.fillAmount * 100f)).ToString();

        if (a >= 0.95f && !startScene) // When the colour gets to the value 0.1...
        {
            StartCoroutine(LoadAsynchrously(sceneToLoad));
            startScene = true;
        }
    }

    IEnumerator LoadAsynchrously(string _scene)
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(_scene);

        while (!operation.isDone)
        {
            float progress = Mathf.Clamp01(operation.progress / 0.9f);
            yield return null;
        }
    }
}