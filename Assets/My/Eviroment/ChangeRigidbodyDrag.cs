using System.Collections;
using UnityEngine;

public class ChangeRigidbodyDrag : MonoBehaviour
{
    public GameObject parentObject; // The parent object containing the Rigidbody children
    public float delayInSeconds = 5f; // Time to wait before setting drag to 0

    private Rigidbody[] rigidbodies;

    void Awake()
    {
        if (parentObject == null)
        {
            parentObject = gameObject; // Default to this game object if none specified
        }

        // Find all Rigidbody components in children of the parent object
        rigidbodies = parentObject.GetComponentsInChildren<Rigidbody>();

        StartCoroutine(SetDragToZeroAfterDelay());
    }

    IEnumerator SetDragToZeroAfterDelay()
    {
        yield return new WaitForSeconds(delayInSeconds);

        foreach (Rigidbody rb in rigidbodies)
        {
            if (rb != null)
            {
                rb.linearDamping = 0f;
            }
        }
    }
}
