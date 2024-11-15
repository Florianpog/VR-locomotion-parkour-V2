using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.UIElements;

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

    [Space(10)]
    [Tooltip("The percentage of push force strength in the movement direction dependent on the angle between the movement direction and the HandDirection (from 0 to 180°)")]
    public AnimationCurve PushStrength_vs_angle;
    public float accelerationMovementSpeedFactor = 5f;
    public GameObject InteractionAreaPrefab;
    public float WindDuration = 0.5f;
    public int maxSubsteps = 5;

    private float logBase = 10f;
    private Vector3 lastHandPosition;
    private Vector3 lastTargetPosition;

    public void Update()
    {
        Vector3 eyeOverHand = (HandDirectionTransform.position - XREyes.transform.position).normalized;
        Vector3 handDir = HandDirectionTransform.forward;
        Vector3 averageDirection = (eyeOverHand + handDir).normalized;
        Ray averageRay = new Ray(HandDirectionTransform.position, averageDirection);
        RaycastHit averageRayHit;
        float fullyTransitionedLastValue = HandRayTransition_vs_distance.keys[HandRayTransition_vs_distance.length - 1].time;
        float fullyTransitionedDistance = Mathf.Pow(logBase, fullyTransitionedLastValue);
        bool hasAverageRayHit = Physics.Raycast(averageRay, out averageRayHit, fullyTransitionedDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

        float averageRayDistance = hasAverageRayHit? (averageRayHit.point - XREyes.transform.position).magnitude : fullyTransitionedDistance;
        //Debug.Log("averageRayDistance: " + averageRayDistance);
        float logAverageRayDistance = Mathf.Log(averageRayDistance, logBase);
        //Debug.Log("logAverageRayDistance: " + logAverageRayDistance);
        float combineValue = HandRayTransition_vs_distance.Evaluate(logAverageRayDistance);
        //Debug.Log("combineValue: " + combineValue);

        Vector3 combinedDirection = (eyeOverHand * combineValue + handDir * (1 - combineValue)).normalized;
        Ray combinedRay = new Ray(HandDirectionTransform.position, combinedDirection);

        RaycastHit hit;
        bool hasHit = Physics.Raycast(combinedRay, out hit, MaxRayDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        if (hasHit)
        {
            Vector3 targetPos = hit.point;

            InteractionArea.transform.position = targetPos;

            InteractionArea.transform.localScale = Vector3.one * CalculateZoneScale(targetPos);

            if(lastTargetPosition != null)
            {
                Vector3 handMovement = HandDirectionTransform.position - lastHandPosition;

                Quaternion windDirection = handMovement != Vector3.zero ? Quaternion.LookRotation(handMovement) : Quaternion.identity;

                float handMovementDirAngle = Vector3.Angle(handDir, handMovement);

                Vector3 targetMovement = targetPos - lastTargetPosition;
                float maxTargetDistance = 1f;
                int numberOfSubsteps = Mathf.Min((int) Mathf.Floor(targetMovement.magnitude / maxTargetDistance), maxSubsteps);
                for (int i = 0; i < numberOfSubsteps; i++)
                {
                    Vector3 windZoneSpawnPos = lastTargetPosition + targetMovement.normalized * (targetMovement.magnitude / numberOfSubsteps + 1) * i;
                    SetupWindZone(windZoneSpawnPos, windDirection, CalculateWindStrength(handMovement.magnitude, windZoneSpawnPos, PushStrength_vs_angle.Evaluate(handMovementDirAngle)));
                }
                SetupWindZone(targetPos, windDirection, CalculateWindStrength(handMovement.magnitude, targetPos, PushStrength_vs_angle.Evaluate(handMovementDirAngle)));
            }

            lastTargetPosition = targetPos;
        }

        lastHandPosition = HandDirectionTransform.position;
    }

    private void SetupWindZone(Vector3 position, Quaternion rotation, float windStrength)
    {
        //Debug.Log($"windStrength: {windStrength.ToReadableFormat()}");
        //Debug.Log("windStrength: " + windStrength);

        float scale = CalculateZoneScale(position);

        GameObject newlySpawnedWindZone = Instantiate(InteractionAreaPrefab);
        newlySpawnedWindZone.transform.position = position;
        newlySpawnedWindZone.transform.rotation = rotation;
        newlySpawnedWindZone.transform.localScale = Vector3.one * scale;

        LocalWindZone localWindZone = newlySpawnedWindZone.GetComponent<LocalWindZone>();

        localWindZone.WindStrength = windStrength;
        Destroy(newlySpawnedWindZone, WindDuration);
    }

    private float CalculateWindStrength(float handMovementMagnitude, Vector3 targetPos, float pushStrengthFromAngle)
    {
        float handToEyeDistance = (HandDirectionTransform.position - XREyes.transform.position).magnitude;
        float targetToEyeDistance = (targetPos - XREyes.transform.position).magnitude;
        return handMovementMagnitude / handToEyeDistance * targetToEyeDistance * accelerationMovementSpeedFactor * pushStrengthFromAngle;
    }

    private float CalculateZoneScale(Vector3 targetPos)
    {
        float distance = (targetPos - XREyes.transform.position).magnitude;
        return distance * InteractionAreaDistanceScaleFactor;
    }
}