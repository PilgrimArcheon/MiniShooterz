using UnityEngine;
using UnityEngine.SceneManagement;

public class NetManagerCheck : MonoBehaviour
{
    void OnEnable()
    {
        SceneManager.activeSceneChanged += OnSceneChanged;
    }

    private void OnSceneChanged(Scene prevScene, Scene newScene)
    {
        if (newScene.name.Contains("Menu")) Destroy(gameObject);
        Debug.Log("SceneChange: " + newScene.name);
    }
}