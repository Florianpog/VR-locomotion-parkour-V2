using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DoorOpener : MonoBehaviour
{
    [SerializeField] private LayerMask targetLayerMask;                 // Layer mask to trigger the door opening
    [SerializeField] private List<Transform> objectsToMove;             // List of objects to move
    [SerializeField] private List<Vector3> targetPositions;             // List of target world positions
    [SerializeField] private float moveTime = 2f;                      // Time to move the objects
    [SerializeField] private Collider colliderToDisable;               // Collider to disable
    [SerializeField] private MeshRenderer meshRendererToChange;         // MeshRenderer to change material
    [SerializeField] private Material newMaterial;                     // New material for the MeshRenderer
    [SerializeField] private AudioSource audioSourceToPlay;            // Audio source to play

    private bool isMoving = false;
    private float elapsedTime = 0f;

    private void OnTriggerEnter(Collider other)
    {
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
        if (objectsToMove.Count != targetPositions.Count)
        {
            Debug.LogError("Objects and target positions count mismatch.");
            yield break;
        }

        isMoving = true;
        elapsedTime = 0f;
        List<Vector3> initialPositions = new List<Vector3>();

        foreach (Transform obj in objectsToMove)
        {
            initialPositions.Add(obj.position);
        }

        while (elapsedTime < moveTime)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / moveTime;
            t = Mathf.SmoothStep(0, 1, t);

            for (int i = 0; i < objectsToMove.Count; i++)
            {
                objectsToMove[i].position = Vector3.Lerp(initialPositions[i], targetPositions[i], t);
            }

            yield return null;
        }

        for (int i = 0; i < objectsToMove.Count; i++)
        {
            objectsToMove[i].position = targetPositions[i];
        }

        isMoving = false;
    }
}
