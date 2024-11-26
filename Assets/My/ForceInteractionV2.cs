using System;
using System.Collections.Generic;
using System.Drawing;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Foce Interaction based on "the one formula to rule them all". Cutting out the middle man of Phsere trigger zones and instead applying force to ALL ridgidbodyies dependent on there relation to the rays
/// </summary>
public class ForceInteractionV2 : MonoBehaviour
{
    [Tooltip("This Transform should be a child of the force interaction hand. The forward direction should be the way the hand is facting and the position the hand center")]
    /// <summary>
    /// This Transform should be a child of the force interaction hand. The forward direction should be the way the hand is facting and the position the hand center
    /// </summary>
    public Transform HandForceInteractionTransform;
    public Camera XREyes;

    public float minAcceleration = 0.01f;
    public float baseMaxForce = 1.0f;

    [Tooltip("basically the influence sphere radius per peter distance")]
    public float fallOffDistance = 0.1f;//!!temporary string cutoff after distance

    List<RigidbodyVelocityStabilizer> allRigidbodyHelpers = new List<RigidbodyVelocityStabilizer>(); //!!!! stabalizer is probably not needed anymore!
    Vector3? savedLastHandPos = null;

    private void Start()
    {
        Rigidbody[] allRigidbodies = FindObjectsByType<Rigidbody>(FindObjectsSortMode.None);
        foreach (Rigidbody rigidbody in allRigidbodies)
        {
            rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
            RigidbodyVelocityStabilizer newHelper = (rigidbody.AddComponent<RigidbodyVelocityStabilizer>());
            newHelper.historySize = 5;
            allRigidbodyHelpers.Add(newHelper);
        }
    }
    private void FixedUpdate()
    {
        if (savedLastHandPos.HasValue && allRigidbodyHelpers != null)
        {
            foreach (var rigidbodyHelper in allRigidbodyHelpers)
            {
                AddForceInteractionForce(rigidbodyHelper, HandForceInteractionTransform.position, savedLastHandPos.Value, XREyes.transform.position);
            }
        }

        savedLastHandPos = HandForceInteractionTransform.position;
    }
    public void AddForceInteractionForce(RigidbodyVelocityStabilizer rigidbodyHelper, Vector3 handPos, Vector3 lastHandPos, Vector3 eyePos/*, Vector3 handDir*//*, Vector3 eyeDir*/)
    {
        Rigidbody rigidbody = rigidbodyHelper.Rigidbody;
        Vector3 objectPos = rigidbody.position; //!! should use center of mass relative to position

        DebugTester.stringFloatLogger.CollectLog("linearVelocity: ", rigidbodyHelper.linearVelocity.magnitude.ToReadableFloat());
        if (rigidbodyHelper.linearVelocity.magnitude > 200f)
            DebugTester.stringFloatLogger.CollectLog("!!!Warning Velocity: ", rigidbodyHelper.linearVelocity.magnitude.ToReadableFloat());

    Vector3 force = CaculateForceInteractionForce(objectPos, rigidbodyHelper.linearVelocity, handPos, lastHandPos, eyePos, () => ApproximateDragCoefficient(rigidbody), (windDir) => ApproximateExposedArea(rigidbody, windDir), rigidbodyHelper.gameObject/*, handDir*//*, eyeDir*/);

        if (force.magnitude / rigidbody.mass >= minAcceleration)
        {
            //DebugTester.stringFloatLogger.CollectLog("dragForce: ", force.magnitude.ToReadableFloat());
            rigidbody.AddForce(force);
        }
    }
    public Vector3 CaculateForceInteractionForce(Vector3 objectPos, Vector3 objectVelocity, Vector3 handPos, Vector3 lastHandPos, Vector3 eyePos, Func<float> GetDragCoefficient, Func<Vector3, float> GetExposedArea, GameObject debugGameObject/*, Vector3 handDir*//*, Vector3 eyeDir*/)
    {

        Vector3 eyeToHand = handPos - eyePos;
        Vector3 eyeTolastHand = lastHandPos - eyePos;

        Vector3 eyeToObject = eyePos - objectPos;

        float handMovementScaleFactor = eyeToObject.magnitude / eyeToHand.magnitude;

        Vector3 lastHandToHand = handPos - lastHandPos;
        Vector3 windVelocity = lastHandToHand / Time.fixedDeltaTime * handMovementScaleFactor;

        //Vector3 objectVelocity = Vector3.zero; //!!TODO stabalize rigidbody velocity so it becomes usable
        Vector3 relativeVelocity = objectVelocity - windVelocity;
        if (relativeVelocity.sqrMagnitude <= 0.001) return Vector3.zero;

        float objectDistanceEyeToHandTriangle = DistanceToEyeToHandTriangle(objectPos, handPos, lastHandPos, eyePos);
        float focusFromDistance = objectDistanceEyeToHandTriangle < fallOffDistance * eyeToObject.magnitude ? 1f : 0f; //!!temporary implementation

        if(focusFromDistance > 0.5)
        {
            debugGameObject.layer = 9;//debug Layer
        }
        else
        {
            debugGameObject.layer = 0;//default layer //!!! quick and dirty
        }

        float focus = focusFromDistance;//!!missing other components like eyeDir
        if (focus <= 0.001) return Vector3.zero;

        float maxForce = baseMaxForce * focus;

        float areaUnderDistanceCurve = fallOffDistance * 2f;//!!temporary
        //volume dir towards Eye: is infitisimal small
        float volumeX = 1f;
        //volume dir in windDir: is 1 for the entire length
        float volumeY = windVelocity.magnitude + areaUnderDistanceCurve * eyeToObject.magnitude;
        //volume dir perpendicular: is soly defined usng the fallOff to the side (which scales with distance)
        float volumeZ = areaUnderDistanceCurve * eyeToObject.magnitude;
        //aproximating  and ignoring that at the corners we would have a curcular effect, !could be imporved relatively easially
        float volume = volumeX * volumeY * volumeZ;

        //the target velocity is fixed but we dont want unliimed forces applied which is why we decrease the density
        //Density = (Force × Time) / (Speed × Volume)
        float airDensity =  (maxForce * Time.fixedDeltaTime) / (windVelocity.magnitude * volume); //not shure if Time.fixedDeltaTime shouldn't be removed
        if (airDensity <= 0.001) return Vector3.zero;

        float dragCoefficient = GetDragCoefficient(); ;
        float exposedArea = GetExposedArea(windVelocity);
        Vector3 windDragForce = -0.5f * airDensity * relativeVelocity.sqrMagnitude * dragCoefficient * exposedArea * relativeVelocity.normalized;

        return windDragForce;
    }

    private static float DistanceToEyeToHandTriangle(Vector3 objectPos, Vector3 handPos, Vector3 lastHandPos, Vector3 eyePos)
    {
        //cheapest implementeation I could think of.
        float maxDistance = 1000f;

        Vector3 eyeToHand = handPos - eyePos;
        Vector3 eyeTolastHand = lastHandPos - eyePos;

        return DistancePointToTriangle(objectPos, eyePos, eyePos + eyeToHand.normalized * maxDistance, eyePos + eyeTolastHand.normalized * maxDistance);
    }

    private static float DistancePointToTriangle(Vector3 point, Vector3 v0, Vector3 v1, Vector3 v2)
    {
        Vector3 edge0 = v1 - v0, edge1 = v2 - v0, v0ToPoint = point - v0;
        float a = Vector3.Dot(edge0, edge0), b = Vector3.Dot(edge0, edge1);
        float c = Vector3.Dot(edge1, edge1), d = Vector3.Dot(edge0, v0ToPoint);
        float e = Vector3.Dot(edge1, v0ToPoint), det = a * c - b * b;

        float s = Mathf.Clamp01((b * e - c * d) / det);
        float t = Mathf.Clamp01((a * e - b * d) / det);

        if (s + t > 1) { s = 1 - t; t = 1 - s; }
        Vector3 projection = v0 + s * edge0 + t * edge1;
        return Vector3.Distance(point, projection);
    }

    /// <param name="windDir">does not have to be normalied</param>
    private static float ApproximateExposedArea(Rigidbody rigidbody, Vector3 windDir)
    {
        if (rigidbody.TryGetComponent<Collider>(out Collider collider))
        {
            windDir = windDir.normalized;

            Bounds bounds = collider.bounds;

            // Simplified approach: use cross-sectional area perpendicular to the wind direction
            float projectedArea = Mathf.Abs(Vector3.Dot(bounds.size, windDir));
            return projectedArea;
        }
        else
        {
            Debug.Log("!could Not calucate exposed area", rigidbody);
            return 0f;
        }
    }

    private static float ApproximateDragCoefficient(Rigidbody rigidbody)
    {
        return 1.0f; // A drag coefficient for typical non-streamlined objects (0.47 would be sphere - like)
    }
}
