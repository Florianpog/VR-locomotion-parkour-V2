using System.Security.Cryptography;
using UnityEngine;

public class ForceInteraction : MonoBehaviour
{
    public Transform HandDirectionTransform;
    public Camera XREyes;

    public GameObject InteractionArea;

    [Tooltip("0 beeing using HandDir, 1 being using the EyeOVerHand")]
    public AnimationCurve HandRayTransition_vs_distance;
    public float MaxRayDistance = 1000;
    public bool useEyeOverHand = true;
    public float InteractionAreaDistanceScaleFactor = 0.1f;

    private float logBase = 10f;

    public void Update()
    {
        Vector3 eyeOverHand = (HandDirectionTransform.position - XREyes.transform.position).normalized;
        Vector3 handDir = HandDirectionTransform.forward;
        Vector3 averageDirection = (eyeOverHand + handDir).normalized;
        Ray averageRay = new Ray(HandDirectionTransform.position, averageDirection);
        RaycastHit averageRayHit;
        float fullyTransitionedLastValue = HandRayTransition_vs_distance.keys[HandRayTransition_vs_distance.length - 1].time;
        float fullyTransitionedDistance = Mathf.Pow(logBase, fullyTransitionedLastValue);
        bool hasAverageRayHit = Physics.Raycast(averageRay, out averageRayHit, fullyTransitionedDistance);

        float averageRayDistance = hasAverageRayHit? (averageRayHit.point - XREyes.transform.position).magnitude : fullyTransitionedDistance;
        Debug.Log("averageRayDistance: " + averageRayDistance);
        float logAverageRayDistance = Mathf.Log(averageRayDistance, logBase);
        Debug.Log("logAverageRayDistance: " + logAverageRayDistance);
        float combineValue = HandRayTransition_vs_distance.Evaluate(logAverageRayDistance);
        Debug.Log("combineValue: " + combineValue);

        Vector3 combinedDirection = (eyeOverHand * combineValue + handDir * (1 - combineValue)).normalized;
        Ray combinedRay = new Ray(HandDirectionTransform.position, combinedDirection);

        RaycastHit hit;
        bool hasHit = Physics.Raycast(combinedRay, out hit, MaxRayDistance);
        if (hasHit)
        {
            InteractionArea.transform.position = hit.point;

            float distance = (hit.point - XREyes.transform.position).magnitude;
            float scale = distance * InteractionAreaDistanceScaleFactor;
            InteractionArea.transform.localScale = Vector3.one * scale;
        }

    }
}