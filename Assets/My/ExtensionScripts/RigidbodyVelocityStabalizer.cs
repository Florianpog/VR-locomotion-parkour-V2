using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using static ForceInteractionV2;
using UnityEditor;
using System.Drawing;

[RequireComponent(typeof(Rigidbody))]
public class RigidbodyVelocityStabilizer : MonoBehaviour //!!!! rename to ____Helper
{
    //[Tooltip("Number of frames to keep for velocity history")]
    //public int historySize = 10;

    //private Queue<Vector3> velocityHistory;

    public Rigidbody Rigidbody { get; private set; }

    public HandData<float> strengthAtGrabTime = new HandData<float>(float.NaN, float.NaN);
    public HandData<Vector3> localPosAtGrabTime = new HandData<Vector3>(Vector3.zero, Vector3.zero);

    //public Vector3 linearVelocity { get; private set; }

    void Start()
    {
        Rigidbody = GetComponent<Rigidbody>();
        //velocityHistory = new Queue<Vector3>(historySize);
    }

    public void StartGrab(bool handIsLeft, Vector3 handPos, Vector3 objectPos, Vector3 eyePos, Transform rayTransform, float effortBasedHandSpeed) //!!sending information twice
    {
        Rigidbody.useGravity = false;

        float strengthAtGrabTime = ForceInteractionV2.instance.CalculateStrengthFromDistance(objectPos, handPos, handPos, eyePos, effortBasedHandSpeed, ForceInteractionV2.ApproximateObjectSphericalSize(this.Rigidbody));
        Vector3 localPos = rayTransform.InverseTransformPoint(objectPos);//CalculateRelativePos(eyePos, handPos, objectPos);

        if (handIsLeft)
        {
            this.strengthAtGrabTime.Left = strengthAtGrabTime;
            this.localPosAtGrabTime.Left = localPos;
        }
        else
        {
            this.strengthAtGrabTime.Right = strengthAtGrabTime;
            this.localPosAtGrabTime.Right = localPos;
        }
    }

    public void StopGrab()
    {
        Rigidbody.useGravity = true;
    }

    public Vector3 GetTartgetPosition(bool handIsLeft, Transform rayTransform)
    {
        //return GetTartgetPosition(rayOrigin, rayDirection, handIsLeft ? localPosAtGrabTime.Left : localPosAtGrabTime.Right);
        return rayTransform.TransformPoint(handIsLeft ? localPosAtGrabTime.Left : localPosAtGrabTime.Right);
    }

    /*static Vector3 CalculateRelativePos(Vector3 rayOrigin, Vector3 rayDirection, Vector3 point)
    {
        Vector3 rayDir = rayDirection.normalized;
        float projectionLength = Vector3.Dot(point - rayOrigin, rayDir);
        return point - (rayOrigin + rayDir * projectionLength);
    }

    static Vector3 GetTartgetPosition(Vector3 rayOrigin, Vector3 rayDirection, Vector3 relativePos)
    {
        Vector3 rayDir = rayDirection.normalized;
        float projectionLength = Vector3.Dot(relativePos + rayOrigin, rayDir);
        return rayOrigin + rayDir * projectionLength + relativePos;
    }*/

    /*
    void FixedUpdate()
    {
        // Record current velocity
        if (velocityHistory.Count >= historySize)
        {
            velocityHistory.Dequeue(); // Remove the oldest velocity
        }
        velocityHistory.Enqueue(Rigidbody.linearVelocity);

        // Stabilize velocity
        linearVelocity = MedianFilter(velocityHistory.ToList());
    }

    private Vector3 MedianFilter(List<Vector3> velocities)
    {
        return new Vector3(
            velocities.Select(v => v.x).Median(),
            velocities.Select(v => v.y).Median(),
            velocities.Select(v => v.z).Median()
        );
    }*/
}
