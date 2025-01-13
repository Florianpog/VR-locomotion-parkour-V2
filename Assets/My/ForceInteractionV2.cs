using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using static ForceInteractionV2;
using static UnityEngine.GraphicsBuffer;

/// <summary>
/// Foce Interaction based on "the one formula to rule them all". Cutting out the middle man of Phsere trigger zones and instead applying force to ALL ridgidbodyies dependent on there relation to the rays
/// </summary>
public class ForceInteractionV2 : MonoBehaviour
{
    public static ForceInteractionV2 instance;

    [Tooltip("This Transform should be a child of the force interaction hand. The forward direction should be the way the hand is facting and the position the hand center")]
    /// <summary>
    /// This Transform should be a child of the force interaction hand. The forward direction should be the way the hand is facting and the position the hand center
    /// </summary>
    public Transform LeftHandForceInteractionTransform;
    public Transform RightHandForceInteractionTransform;
    public Camera XREyes;

    public float minAccelerationRequired = 0.001f;
    public float baseForce = 1.0f;
    public float baseGrabbedForce = 1.0f;
    public float baseStiffness = 50f;
    public float maxForceDelayTime = 0.5f; //!!!TODO remove with corresponding code, I dont think this actually improves anything
    public float minAccelerationForAnyMass = 0.1f;
    public float minMassForAnyAcceleration = 0.1f;

    [Tooltip("basically the influence sphere radius per peter distance")]
    public float baseFallOffDistance = 0.01f;

    [Tooltip("The fallOffDistance percentage (gets mulitplied with baseFallOffDistance) dependent on the hand movement speed (should never be 0)")]
    public AnimationCurve FallOffDistancePercentage_vs_handVelocity;

    [Tooltip("The percentage of push force strength dependent the percentage of distance / fallOffDistance (>100% is when the distance exceeds the fall of distance) \nShould be 100% at 0 and 0% at 100%")]
    public AnimationCurve PushStrength_vs_fallOffDistance;

    [Tooltip("The percentage of push force strength in the movement direction dependent on the angle between the movement direction and the HandDirection (from 0 to 180°)")]
    public AnimationCurve PushStrength_vs_angle;

    [Tooltip("The multiplication factor for push force strength dependet on the hand movement speed")]
    public AnimationCurve PushStrength_vs_handVelocity;

    [Tooltip("the haptic feedback vibration intensity dependet on the hand movement speed")]
    public AnimationCurve VibrationIntensity_vs_handVelocity;

    [Space(10)]
    // HandVelocity Scaling factors based on your analysis
    private float LeftRightFactor = 1.0f;  // Baseline
    public float DownwardFactor = 1.5f;
    public float UpwardFactor = 2.0f;
    public float ForwardFactor = 1.5f;

    List<RigidbodyVelocityStabilizer> allRigidbodyHelpers = new List<RigidbodyVelocityStabilizer>(); //!!!! stabalizer is probably not needed anymore!

    //public InputActionReference ActivatDebug;
    public GameObject debugLinePrefab;

    public InputActionReference LeftHandGrab;
    public InputActionReference RightHandGrab;

    public HandData<Transform> handsRayTransforms;

    private HandData<Vector3?> handsSavedLastHandPos = new HandData<Vector3?>(null, null);
    private HandData<MyQueue<float>> handsVibrationIntensities = new HandData<MyQueue<float>>(new MyQueue<float>(), new MyQueue<float>());
    private HandData<bool> handsGrabbing = new HandData<bool>(false, false);

    public int numberOfMovingAvg = 5;

    private bool useVibrations = true;

    private void Awake()
    {
        instance = this;
    }

    [System.Serializable]
    public struct HandData<T>
    {
        public T Left;
        public T Right;

        public HandData(T left, T right)
        {
            Left = left;
            Right = right;
        }
    }


    private void Start()
    {
        //ActivatDebug.action.started += (_) => DebugLines(XREyes.transform.position, HandForceInteractionTransform.position, savedLastHandPos.Value);
        //ActivatDebug.action.started += (_) => DebugToggleVibration();

        LeftHandGrab.action.started += (_) => HandsStartGrabbing(true);
        RightHandGrab.action.started += (_) => HandsStartGrabbing(false);

        LeftHandGrab.action.canceled += (_) => HandsStopGrabbing(true);
        RightHandGrab.action.canceled += (_) => HandsStopGrabbing(false);

        Rigidbody[] allRigidbodies = FindObjectsByType<Rigidbody>(FindObjectsSortMode.None);
        foreach (Rigidbody rigidbody in allRigidbodies)
        {
            rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
            RigidbodyVelocityStabilizer newHelper = (rigidbody.AddComponent<RigidbodyVelocityStabilizer>());
            //newHelper.historySize = 5;
            allRigidbodyHelpers.Add(newHelper);
        }
    }

    private void FixedUpdate()
    {
        for (int i = 0; i < 2; i++)
        {
            bool handIsLeft = i == 0 ? true : false;

            bool handIsGrabbing = handIsLeft ? handsGrabbing.Left : handsGrabbing.Right;

            Transform HandForceInteractionTransform = handIsLeft ? LeftHandForceInteractionTransform : RightHandForceInteractionTransform;
            HapticsUtility.Controller hapticsController = handIsLeft ? HapticsUtility.Controller.Left : HapticsUtility.Controller.Right;

            ref Vector3? savedLastHandPos = ref (handIsLeft ? ref handsSavedLastHandPos.Left : ref handsSavedLastHandPos.Right);
            MyQueue<float> vibrationIntensities = handIsLeft ? handsVibrationIntensities.Left: handsVibrationIntensities.Right;

            if (savedLastHandPos.HasValue)
            {
                Vector3 handPos = HandForceInteractionTransform.position;
                Vector3 lastHandPos = savedLastHandPos.Value;
                Vector3 handDir = HandForceInteractionTransform.forward;

                Vector3 eyePos = XREyes.transform.position;

                if (handIsLeft)
                {
                    handsRayTransforms.Left.position = eyePos;
                    handsRayTransforms.Left.LookAt(handPos);
                    handsRayTransforms.Left.localScale = Vector3.one * (eyePos - handPos).magnitude;
                }
                else
                {
                    handsRayTransforms.Right.position = eyePos;
                    handsRayTransforms.Right.LookAt(handPos);
                    handsRayTransforms.Right.localScale = Vector3.one * (eyePos - handPos).magnitude;
                }

                Vector3 lastHandToHand = handPos - lastHandPos;
                Vector3 eyeToHand = handPos - eyePos;

                Vector3 handVelocity = lastHandToHand / Time.fixedDeltaTime;
                DebugTester.stringFloatLogger.CollectLog("handVelocity: ", handVelocity.magnitude.ToReadableFloat());
                float effortBasedHandSpeed = CalculateEffortBasedHandVelocity(handVelocity, eyeToHand).magnitude;
                DebugTester.stringFloatLogger.CollectLog("effortBasedHandSpeed: ", effortBasedHandSpeed.ToReadableFloat());


                float largestStrengthTotal = 0f;
                foreach (var rigidbodyHelper in allRigidbodyHelpers)
                {
                    if (!handIsGrabbing)
                    {
                        Rigidbody rigidbody = rigidbodyHelper.Rigidbody;
                        Vector3 objectPos = rigidbody.position; //!! should use center of mass relative to position

                        float strengthAtGrabTime = handIsLeft ? rigidbodyHelper.strengthAtGrabTime.Left : rigidbodyHelper.strengthAtGrabTime.Right;

                        Tuple<Vector3, float> result = CaculateForceInteractionForce2(handIsGrabbing, objectPos, rigidbodyHelper.Rigidbody.linearVelocity, handPos, lastHandPos, eyePos, handDir, effortBasedHandSpeed, ApproximateObjectSphericalSize(rigidbodyHelper.Rigidbody), rigidbodyHelper.Rigidbody.mass, strengthAtGrabTime, rigidbodyHelper.gameObject/*, handDir*//*, eyeDir*/);
                        Vector3 force = result.Item1;
                        float strengthTotal = result.Item2;

                        largestStrengthTotal = Mathf.Max(largestStrengthTotal, strengthTotal);

                        if (force.magnitude / rigidbody.mass >= minAccelerationRequired)
                            rigidbody.AddForce(force);
                    }
                    else
                    {
                        Rigidbody rigidbody = rigidbodyHelper.Rigidbody;
                        Vector3 objectPos = rigidbody.position; //!! should use center of mass relative to position
                        Vector3 targetObjectPos = rigidbodyHelper.GetTartgetPosition(handIsLeft, (handIsLeft ? handsRayTransforms.Left : handsRayTransforms.Right));

                        rigidbody.AddForce(CaculateForceInteractionForceGrabbed(handIsLeft, objectPos, targetObjectPos, rigidbodyHelper));
                    }
                }

                //HapticsUtility.SendHapticImpulse(VibrationIntensity_vs_handVelocity.Evaluate(effortBasedHandSpeed), duration: 1.0f, HapticsUtility.Controller.Right);
                vibrationIntensities.Enqueue(largestStrengthTotal);
                if (vibrationIntensities.Count > numberOfMovingAvg)
                    vibrationIntensities.Dequeue();
                float averageIntensity = vibrationIntensities.Count > 0 ? Enumerable.Range(0, vibrationIntensities.Count).Average(i => vibrationIntensities[i]) : 0;
                if(useVibrations)
                    HapticsUtility.SendHapticImpulse(VibrationIntensity_vs_handVelocity.Evaluate(effortBasedHandSpeed) * averageIntensity, duration: 1.0f, hapticsController);
            }

            savedLastHandPos = HandForceInteractionTransform.position;
        }
    }

    private void HandsStartGrabbing(bool handIsLeft)
    {
        if (handIsLeft)
            handsGrabbing.Left = true;
        else
            handsGrabbing.Right = true;

        Transform HandForceInteractionTransform = handIsLeft ? LeftHandForceInteractionTransform : RightHandForceInteractionTransform;
        ref Vector3? savedLastHandPos = ref (handIsLeft ? ref handsSavedLastHandPos.Left : ref handsSavedLastHandPos.Right);

        if (savedLastHandPos.HasValue)
        {
            Vector3 handPos = HandForceInteractionTransform.position;
            Vector3 lastHandPos = savedLastHandPos.Value;
            Vector3 handDir = HandForceInteractionTransform.forward;

            Vector3 eyePos = XREyes.transform.position;

            Vector3 lastHandToHand = handPos - lastHandPos;
            Vector3 eyeToHand = handPos - eyePos;

            Vector3 handVelocity = lastHandToHand / Time.fixedDeltaTime;
            DebugTester.stringFloatLogger.CollectLog("handVelocity: ", handVelocity.magnitude.ToReadableFloat());
            float effortBasedHandSpeed = CalculateEffortBasedHandVelocity(handVelocity, eyeToHand).magnitude;

            foreach (var rigidbodyHelper in allRigidbodyHelpers)
            {
                Rigidbody rigidbody = rigidbodyHelper.Rigidbody;
                Vector3 objectPos = rigidbody.position; //!! should use center of mass relative to position

                rigidbodyHelper.StartGrab(handIsLeft, handPos, objectPos, eyePos, (handIsLeft ? handsRayTransforms.Left : handsRayTransforms.Right), effortBasedHandSpeed);
                /*
                float strengthAtGrabTime = CalculateStrengthFromDistance(objectPos, handPos, handPos, eyePos, effortBasedHandSpeed, ApproximateObjectSphericalSize(rigidbodyHelper.Rigidbody));
                
                if(handIsLeft)
                    rigidbodyHelper.strengthAtGrabTime.Left = strengthAtGrabTime;
                else 
                    rigidbodyHelper.strengthAtGrabTime.Right = strengthAtGrabTime;*/
            }
        }
    }

    private void HandsStopGrabbing(bool handIsLeft)
    {
        if (handIsLeft)
            handsGrabbing.Left = false;
        else
            handsGrabbing.Right = false;

        foreach (var rigidbodyHelper in allRigidbodyHelpers)
        {
            rigidbodyHelper.StopGrab();
        }
    }

    public Tuple<Vector3, float> CaculateForceInteractionForce2(bool handIsGrabbing, Vector3 objectPos, Vector3 objectVelocity, Vector3 handPos, Vector3 lastHandPos, Vector3 eyePos, Vector3 handDir, float effortBasedHandSpeed, float objectSphericalSize, float objectMass, float strengthAtGrabTime, GameObject debugGameObject/*, Func<float> GetDragCoefficient, Func<Vector3, float> GetExposedArea, Vector3 eyeDir*/)
    {
        Vector3 relativeTargetVelocity = CalculateRelativeTargetVelocity(objectPos, objectVelocity, handPos, lastHandPos, eyePos);
        if (relativeTargetVelocity.sqrMagnitude <= 0.001) return new Tuple<Vector3, float>(Vector3.zero, 0f);

        //Distance
        float strengthFromDistance = CalculateStrengthFromDistance(objectPos, handPos, lastHandPos, eyePos, effortBasedHandSpeed, objectSphericalSize);
        if (handIsGrabbing)
            strengthFromDistance = Mathf.Min(strengthAtGrabTime, strengthFromDistance);

        if (strengthFromDistance > 0.1)
            debugGameObject.layer = 9;//debug Layer
        else
            debugGameObject.layer = 0;//default layer //!!! quick and dirty

        //HandSpeed
        float strengthFromHandSpeed = PushStrength_vs_handVelocity.Evaluate(effortBasedHandSpeed);
        
        //Angle
        float handForceDirAngle = Vector3.Angle(handDir, relativeTargetVelocity);
        float strengthFromAngle = PushStrength_vs_angle.Evaluate(handForceDirAngle);


        float strengthTotal = strengthFromDistance * strengthFromHandSpeed * strengthFromAngle;


        // DragForce = -0.5f * airDensity * dragCoefficient * exposedArea * relativeVelocity.magnitue^2 * relativeVelocity.normalized;
        float baseForceMultiplier = handIsGrabbing ? baseGrabbedForce : baseForce;
        Vector3 force = baseForceMultiplier * strengthTotal * relativeTargetVelocity.normalized;

        // Replacing Physically accurate "force / objectMass (F/m)" with pseudo physics to allow moving super havy objects and limiting speed of super light
        // F * [(1/ (m + c2)) + c1]
        // force * minAccelerationForAnyMass (F * c1) causes a acceleration for any mass, but only if a force exists
        // 1 / (objectMass + minMassForAnyAcceleration) (1/ (m + c2)) causes objects to be treated like they have at least a minimum mass
        Vector3 accelerationMassCorrected = force * ((1f / (objectMass + minMassForAnyAcceleration)) + minAccelerationForAnyMass);
        Vector3 forceMassCorrected = accelerationMassCorrected * objectMass;

        float maxForceForOneFixedFrame = relativeTargetVelocity.magnitude * objectMass / Time.fixedDeltaTime;
        return new Tuple<Vector3, float>(Vector3.ClampMagnitude(forceMassCorrected, maxForceForOneFixedFrame), strengthTotal);
    }

    public Vector3 CaculateForceInteractionForceGrabbed(bool handIsGrabbing, Vector3 objectPos, Vector3 targetObejctPos, RigidbodyVelocityStabilizer rigidbodyHelper)
    {
        float strengthAtGrabTime = handIsGrabbing ? rigidbodyHelper.strengthAtGrabTime.Left : rigidbodyHelper.strengthAtGrabTime.Right;

        float stiffness = baseStiffness * strengthAtGrabTime;  // Proportional gain (k_p)

        Vector3 objectVelocity = rigidbodyHelper.Rigidbody.linearVelocity;
        float objectMass = rigidbodyHelper.Rigidbody.mass;

        // Calculate position error
        Vector3 positionError = targetObejctPos - objectPos;

        // Calculate damping coefficient for critical damping
        float damping = 2 * Mathf.Sqrt(stiffness * objectMass);

        // Calculate velocity error
        Vector3 velocityError = -objectVelocity;

        // Calculate the force to apply
        Vector3 force = stiffness * positionError + damping * velocityError;

        // Apply force to the Rigidbody

        return force; //!!!! strength missing
    }

    private static Vector3 CalculateRelativeTargetVelocity(Vector3 objectPos, Vector3 objectVelocity, Vector3 handPos, Vector3 lastHandPos, Vector3 eyePos)
    {
        Vector3 eyeToHand = handPos - eyePos;
        Vector3 eyeToObject = eyePos - objectPos;

        float handMovementScaleFactor = eyeToObject.magnitude / eyeToHand.magnitude;

        Vector3 lastHandToHand = handPos - lastHandPos;
        Vector3 targetVelocity = lastHandToHand / Time.fixedDeltaTime * handMovementScaleFactor;

        //Vector3 objectVelocity = Vector3.zero; //!!TODO stabalize rigidbody velocity so it becomes usable

        Vector3 relativeTargetVelocity = targetVelocity - objectVelocity;
        return relativeTargetVelocity;
    }

    public float CalculateStrengthFromDistance(Vector3 objectPos, Vector3 handPos, Vector3 lastHandPos, Vector3 eyePos, float effortBasedHandSpeed, float objectSphericalSize)
    {
        float fallOffDistanceFromHandVelocity = FallOffDistancePercentage_vs_handVelocity.Evaluate(effortBasedHandSpeed);

        Vector3 eyeToObject = eyePos - objectPos;
        float objectFallOffDistance = fallOffDistanceFromHandVelocity * baseFallOffDistance * eyeToObject.magnitude;

        float objectDistanceEyeToHandTriangle = DistanceToEyeToHandTriangle(objectPos, objectSphericalSize, handPos, lastHandPos, eyePos);
        float strengthFromDistance = PushStrength_vs_fallOffDistance.Evaluate(objectDistanceEyeToHandTriangle / objectFallOffDistance);
        return strengthFromDistance;
    }

    /// <summary>
    /// Adjusts hand velocity to reflect the varying physical effort needed for different directions, increasing legth in more difficult ones
    /// </summary>
    public Vector3 CalculateEffortBasedHandVelocity(Vector3 handVelocity, Vector3 eyeToHand)
    {
        // Normalize the eyeToHand vector for directional purposes
        Vector3 forwardDir = eyeToHand.normalized;

        // Compute the upward direction (aligned with Vector3.up, perpendicular to forwardDir)
        Vector3 upwardDir = Vector3.Cross(forwardDir, Vector3.Cross(Vector3.up, forwardDir)).normalized;

        // Compute the right direction (orthogonal to forward and upward)
        Vector3 rightDir = Vector3.Cross(forwardDir, upwardDir).normalized;

        // Decompose hand velocity into directional components
        float forwardComponent = Vector3.Dot(handVelocity, forwardDir);
        float verticalComponent = Vector3.Dot(handVelocity, upwardDir);
        float lateralComponent = Vector3.Dot(handVelocity, rightDir);

        // Apply scaling factors to each component
        float scaledForward = forwardComponent * ForwardFactor;

        // Determine scaling factor based on the direction of vertical movement
        float verticalFactor = verticalComponent > 0 ? UpwardFactor : DownwardFactor;
        float scaledVertical = verticalComponent * verticalFactor;

        float scaledLateral = lateralComponent * LeftRightFactor;

        // Reconstruct the virtual velocity vector using scaled components
        Vector3 virtualVelocity = (scaledForward * forwardDir) +
                                  (scaledVertical * upwardDir) +
                                  (scaledLateral * rightDir);

        return virtualVelocity;
    }

    public Vector3 CaculateForceInteractionForce(Vector3 objectPos, Vector3 objectVelocity, Vector3 handPos, Vector3 lastHandPos, Vector3 eyePos, Vector3 handDir, float objectSphericalSize, float objectMass, Func<float> GetDragCoefficient, Func<Vector3, float> GetExposedArea, GameObject debugGameObject/*, Vector3 eyeDir*/)
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

        float objectDistanceEyeToHandTriangle = DistanceToEyeToHandTriangle(objectPos, objectSphericalSize, handPos, lastHandPos, eyePos);
        float focusFromDistance = objectDistanceEyeToHandTriangle < baseFallOffDistance * eyeToObject.magnitude ? 1f : 0f; //!!temporary implementation

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

        float handForceDirAngle = Vector3.Angle(handDir, -relativeVelocity);

        float maxForce = baseForce * focus * PushStrength_vs_angle.Evaluate(handForceDirAngle) * eyeToObject.magnitude; //!!! testing compensating linear force decrease (with distance) based on the fomulas afterwards

        float areaUnderDistanceCurve = baseFallOffDistance * 2f;//!!temporary
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

    private static float DistanceToEyeToHandTriangle(Vector3 objectPos, float objectSphericalSize, Vector3 handPos, Vector3 lastHandPos, Vector3 eyePos)
    {
        //cheapest implementeation I could think of.
        float maxDistance = 100f;

        Vector3 eyeToHand = handPos - eyePos;
        Vector3 eyeTolastHand = lastHandPos - eyePos;

        /*if(Vector3.Angle(eyeToHand, eyeTolastHand) > 5f)
        {
            DebugLines(eyePos, handPos, lastHandPos);
        }*/

        float pointDistance = DistancePointToTriangle(objectPos, eyePos, eyePos + eyeToHand.normalized * maxDistance, eyePos + eyeTolastHand.normalized * maxDistance);

        return Mathf.Max(0, pointDistance - objectSphericalSize);
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

    public static float ApproximateObjectSphericalSize(Rigidbody rigidbody)
    {
        if (rigidbody.TryGetComponent<Collider>(out Collider collider))
        {
            Bounds bounds = collider.bounds;

            // Apply a scaling factor for more accurate spherical approximation
            float scalingFactor = 0.75f;
            float sphericalSize = bounds.extents.magnitude * scalingFactor;
            return sphericalSize;
        }
        else
        {
            Debug.Log("!Could not calculate spherical size", rigidbody);
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

    private void DebugToggleVibration()
    {
        useVibrations = !useVibrations;
    }
}
