using UnityEngine;

public class TeleportOnEnter : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("The specific collider that can trigger the teleport.")]
    public Collider triggerCollider;

    [Tooltip("The object that will be teleported.")]
    public GameObject objectToTeleport;

    [Tooltip("The location to teleport the object to.")]
    public Transform teleportLocation;

    private void OnTriggerEnter(Collider other)
    {
        // Check if the colliding object is the specified collider
        if (other == triggerCollider)
        {
            if (objectToTeleport != null && teleportLocation != null)
            {
                objectToTeleport.transform.position = teleportLocation.position;
                objectToTeleport.transform.rotation = teleportLocation.rotation;
            }
            else
            {
                Debug.LogWarning("Object to teleport or teleport location is not assigned.");
            }
        }
    }
}
