using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

namespace ArcNet
{
    public class ArcNetSceneManager : ArcNetCallbacks
    {
        public static ArcNetSceneManager Instance;

        private const string SCENE_PROP_KEY = "curScn";

        // FIX: Add state tracking
        private bool _isLoading = false;
        private string _targetScene = "";

        private void Awake()
        {
            if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
            else Destroy(gameObject);
        }

        public void LoadScene(string sceneName)
        {
            if (!ArcNetClient.Instance.IsMasterClient)
            {
                Debug.LogWarning("[ArcNet] Only Master Client can switch scenes.");
                return;
            }
            ArcNetClient.Instance.SetRoomProperty(SCENE_PROP_KEY, sceneName);
        }

        public override void OnRoomPropertiesUpdate(Dictionary<string, object> changedProps)
        {
            if (changedProps.ContainsKey(SCENE_PROP_KEY))
            {
                string newScene = changedProps[SCENE_PROP_KEY].ToString();
                CheckAndLoad(newScene);
            }
        }

        public override void OnJoinedRoom(ArcNetRoom room)
        {
            if (room.CustomProperties.TryGetValue(SCENE_PROP_KEY, out object sceneObj))
            {
                string serverScene = sceneObj.ToString();
                CheckAndLoad(serverScene);
            }
        }

        private void CheckAndLoad(string sceneName)
        {
            // Basic Check: Are we already there?
            if (SceneManager.GetActiveScene().name == sceneName) return;

            // LOCK CHECK: Are we already loading THIS scene?
            // If we are loading "Game" and get asked to load "Game" again, ignore it.
            if (_isLoading && _targetScene == sceneName) 
            {
                // Debug.Log($"[ArcNet] Already loading {sceneName}, ignoring duplicate request.");
                return;
            }

            Debug.Log($"[ArcNet] Syncing Scene: {sceneName}");
            
            // Set Lock
            _isLoading = true;
            _targetScene = sceneName;
            
            StartCoroutine(LoadAsync(sceneName));
        }

        private IEnumerator LoadAsync(string sceneName)
        {
            // You can add a UI Loading Screen trigger here
            yield return SceneManager.LoadSceneAsync(sceneName);
            
            // Release Lock
            _isLoading = false;
            _targetScene = ""; 
        }
    }
}