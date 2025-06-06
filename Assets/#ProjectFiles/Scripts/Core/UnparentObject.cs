using UnityEngine;

public class UnparentObject : MonoBehaviour
{
    void Start() => transform.SetParent(null);
}