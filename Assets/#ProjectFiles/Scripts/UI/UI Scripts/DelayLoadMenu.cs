using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class DelayLoadMenu : MonoBehaviour
{
    public GameObject menuToLoad;
    public float loadSpeed = 0.5f;
    bool startScene;
    float a;

    void OnEnable() => a = 0;

    void Update()
    {
        a += Time.deltaTime * loadSpeed;

        if (a >= 1f)
        {
            menuToLoad.SetActive(true);
            gameObject.SetActive(false);
        }
    }
}