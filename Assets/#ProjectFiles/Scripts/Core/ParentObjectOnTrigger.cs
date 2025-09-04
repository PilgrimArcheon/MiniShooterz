using UnityEngine;

public class ParentObjectOnTrigger : MonoBehaviour
{
    bool hasParent;
    bool started;
    public void OnEnable()
    {
        started = true;// Only the server can start the process
    }

    public void OnDisable()
    {
        started = false;// Reset started flag
        hasParent = false;// Reset parent flag
        parentObject = null;// Reset parent object
    }

    void Update()
    {
        if (!hasParent && !started) return;

        if (parentObject != null)
        {
            Vector3 stay = parentObject.position;
            transform.position = new(stay.x, transform.position.y, stay.z);
        }
    }

    Transform parentObject;
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.CompareTag("AI"))
        {
            if (!hasParent && started)
            {
                parentObject = other.transform;
                hasParent = true;
            }
        }
    }
}