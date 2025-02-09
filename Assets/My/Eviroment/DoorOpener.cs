using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DoorOpener : MonoBehaviour
{
    [SerializeField] private LayerMask targetLayerMask;            // Layer mask to trigger the door opening
    [SerializeField] private List<Transform> objectsToMove;        // List of objects to move
    [SerializeField] private List<Vector3> targetPositions;        // List of target local rotations (Euler angles)
    [SerializeField] private float moveTime = 2f;                  // Time to move the objects
    [SerializeField] private Collider colliderToDisable;           // Collider to disable
    [SerializeField] private MeshRenderer meshRendererToChange;    // MeshRenderer to change material
    [SerializeField] private Material newMaterial;                 // New material for the MeshRenderer
    [SerializeField] private AudioSource audioSourceToPlay;        // Audio source to play

    private bool isMoving = false;
    private float elapsedTime = 0f;

    private void OnTriggerEnter(Collider other)
    {
        // Check if the collider triggering the event is on the target layer
        if (!isMoving && ((1 << other.gameObject.layer) & targetLayerMask) != 0)
        {
            colliderToDisable.enabled = false;
            meshRendererToChange.material = newMaterial;
            audioSourceToPlay.Play();
            StartCoroutine(MoveObjects());
        }
    }

    private IEnumerator MoveObjects()
    {
        // Ensure we have as many target rotations as we do objects
        if (objectsToMove.Count != targetPositions.Count)
        {
            Debug.LogError("Objects and target positions count mismatch.");
            yield break;
        }

        isMoving = true;
        elapsedTime = 0f;

        // Store each object's initial local rotation
        List<Quaternion> initialRotations = new List<Quaternion>();
        foreach (Transform obj in objectsToMove)
        {
            initialRotations.Add(obj.localRotation);
        }

        // Interpolate from the initial rotation to the target rotation over 'moveTime'
        while (elapsedTime < moveTime)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / moveTime;
            t = Mathf.SmoothStep(0, 1, t);  // Smooth the interpolation

            for (int i = 0; i < objectsToMove.Count; i++)
            {
                Quaternion targetRotation = Quaternion.Euler(targetPositions[i]);
                objectsToMove[i].localRotation = Quaternion.Lerp(initialRotations[i], targetRotation, t);
            }

            yield return null;
        }

        // Ensure the final rotation is exactly the target rotation
        for (int i = 0; i < objectsToMove.Count; i++)
        {
            objectsToMove[i].localRotation = Quaternion.Euler(targetPositions[i]);
        }

        isMoving = false;
    }
}
