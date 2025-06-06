using UnityEngine;
using Cinemachine;

public class Confiner : MonoBehaviour
{
    CinemachineConfiner cinemachineConfiner;

    void OnEnable()
    {
        cinemachineConfiner = GetComponent<CinemachineConfiner>();
        cinemachineConfiner.m_BoundingVolume = GameObject.Find("CamBoundingVolume").GetComponent<Collider>();
    }
}
