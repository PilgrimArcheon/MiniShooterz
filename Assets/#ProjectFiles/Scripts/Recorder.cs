using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.Playables;

public class Recorder : MonoBehaviour
{
    [SerializeField] PlayableDirector playableDirector;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
}
