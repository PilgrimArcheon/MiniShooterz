using UnityEngine;

public class DestroyOnLifeTime : MonoBehaviour
{
    [SerializeField] float lifeTime = 1f;
    float timeToStay;

    bool started;
    public void OnEnable()
    {
        started = true;
        timeToStay = Time.time + lifeTime;
    }

    void Update()
    {
        if (!started) return;

        if (timeToStay < Time.time)
        {
            // Time to return to the pool from whence it came.
            gameObject.SetActive(false);
            return;
        }
    }
}