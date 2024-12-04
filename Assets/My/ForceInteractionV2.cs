using System;
using System.Collections.Generic;
using System.Drawing;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

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

    public InputActionReference ActivatDebug;
    public GameObject debugLinePrefab;

    private void Start()
    {
        ActivatDebug.action.started += (_) => DebugLines(XREyes.transform.position, HandForceInteractionTransform.position, savedLastHandPos.Value);

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

    Vector3 force = CaculateForceInteractionForce(objectPos, rigidbodyHelper.Rigidbody.linearVelocity, handPos, lastHandPos, eyePos, rigidbodyHelper.Rigidbody.mass, () => ApproximateDragCoefficient(rigidbody), (windDir) => ApproximateExposedArea(rigidbody, windDir), rigidbodyHelper.gameObject/*, handDir*//*, eyeDir*/);

        if (force.magnitude / rigidbody.mass >= minAcceleration)
        {
            //DebugTester.stringFloatLogger.CollectLog("dragForce: ", force.magnitude.ToReadableFloat());
            rigidbody.AddForce(force);
        }
    }
    public Vector3 CaculateForceInteractionForce(Vector3 objectPos, Vector3 objectVelocity, Vector3 handPos, Vector3 lastHandPos, Vector3 eyePos, float objectMass, Func<float> GetDragCoefficient, Func<Vector3, float> GetExposedArea, GameObject debugGameObject/*, Vector3 handDir*//*, Vector3 eyeDir*/)
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

        float maxForceForOneFixedFrame = relativeVelocity.magnitude * objectMass / Time.fixedDeltaTime;

        return Vector3.ClampMagnitude(windDragForce, maxForceForOneFixedFrame);
    }

    private float DistanceToEyeToHandTriangle(Vector3 objectPos, Vector3 handPos, Vector3 lastHandPos, Vector3 eyePos) //!!should be static
    {
        //cheapest implementeation I could think of.
        float maxDistance = 100f;

        Vector3 eyeToHand = handPos - eyePos;
        Vector3 eyeTolastHand = lastHandPos - eyePos;

        /*if(Vector3.Angle(eyeToHand, eyeTolastHand) > 5f)
        {
            DebugLines(eyePos, handPos, lastHandPos);
        }*/

        return DistancePointToTriangle(objectPos, eyePos, eyePos + eyeToHand.normalized * maxDistance, eyePos + eyeTolastHand.normalized * maxDistance);
    }

    private static float DistancePointToTriangle(Vector3 point, Vector3 v0, Vector3 v1, Vector3 v2)
    {
        // Compute plane normal
        Vector3 edge0 = v1 - v0;
        Vector3 edge1 = v2 - v0;
        Vector3 normal = Vector3.Cross(edge0, edge1);
        float area2 = normal.magnitude;

        // Check for degenerate triangle
        if (area2 == 0f)
        {
            if (v0 == v1 && v0 == v2)
            {
                // All vertices are the same point
                return (point - v0).magnitude;
            }
            else if (v0 == v1)
            {
                // Line segment from v0/v1 to v2
                return DistancePointToSegment(point, v0, v2);
            }
            else if (v1 == v2)
            {
                // Line segment from v0 to v1/v2
                return DistancePointToSegment(point, v0, v1);
            }
            else if (v0 == v2)
            {
                // Line segment from v0/v2 to v1
                return DistancePointToSegment(point, v0, v1);
            }
            else
            {
                // All vertices are distinct but colinear
                float dist0 = DistancePointToSegment(point, v0, v1);
                float dist1 = DistancePointToSegment(point, v1, v2);
                float dist2 = DistancePointToSegment(point, v2, v0);
                return Mathf.Min(dist0, dist1, dist2);
            }
        }

        normal /= area2; // Normalize the normal

        // Compute distance from point to triangle plane
        float distanceToPlane = Vector3.Dot(normal, point - v0);
        Vector3 projection = point - distanceToPlane * normal;

        // Compute barycentric coordinates
        Vector3 vp0 = projection - v0;
        float d00 = Vector3.Dot(edge0, edge0);
        float d01 = Vector3.Dot(edge0, edge1);
        float d11 = Vector3.Dot(edge1, edge1);
        float d20 = Vector3.Dot(vp0, edge0);
        float d21 = Vector3.Dot(vp0, edge1);
        float denom = d00 * d11 - d01 * d01;

        float v = (d11 * d20 - d01 * d21) / denom;
        float w = (d00 * d21 - d01 * d20) / denom;
        float u = 1f - v - w;

        // Check if projection is inside triangle
        if (u >= 0f && v >= 0f && w >= 0f)
        {
            return Mathf.Abs(distanceToPlane);
        }
        else
        {
            // Compute distances to triangle edges
            float dist0 = DistancePointToSegment(point, v0, v1);
            float dist1 = DistancePointToSegment(point, v1, v2);
            float dist2 = DistancePointToSegment(point, v2, v0);
            return Mathf.Min(dist0, dist1, dist2);
        }
    }

    private static float DistancePointToSegment(Vector3 point, Vector3 a, Vector3 b)
    {
        Vector3 ab = b - a;
        float t = Mathf.Clamp01(Vector3.Dot(point - a, ab) / Vector3.Dot(ab, ab));
        Vector3 closestPoint = a + t * ab;
        return (closestPoint - point).magnitude;
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

    private void DebugLines(Vector3 eyePos, Vector3 handPos, Vector3 lastHandPos)
    {
        Vector3 eyeToHand = handPos - eyePos;
        Vector3 eyeTolastHand = lastHandPos - eyePos;

        var o1 = Instantiate(debugLinePrefab, eyePos, Quaternion.LookRotation(eyeToHand));
        o1.transform.LookAt(handPos);
        //o1.transform.localScale = new Vector3(o1.transform.localScale.x, o1.transform.localScale.y, o1.transform.localScale.z);
        var o2 = Instantiate(debugLinePrefab, eyePos, Quaternion.LookRotation(eyeTolastHand));
        o1.transform.LookAt(lastHandPos);
    }
}
