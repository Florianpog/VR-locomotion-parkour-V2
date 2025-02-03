using UnityEngine;
using static Unity.VisualScripting.Member;

public class RotationTester2 : MonoBehaviour
{
    public Transform source;
    private Rigidbody rb;
    public float factor = 1f;
    public float torqueGain = 10f; // Tweak this to tune responsiveness
    private Matrix4x4 oldBasis;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        oldBasis = Matrix4x4.Rotate(source.rotation);
    }

    private void FixedUpdate()
    {
        Matrix4x4 newBasis = Matrix4x4.Rotate(source.rotation);

        Matrix4x4 diff = newBasis * oldBasis.transpose;

        diff.rotation.ToAngleAxis(out float angleDegrees, out Vector3 axis);

        // Scale the angle and, if valid, compute desired angular velocity
        float scaledAngle = angleDegrees * factor;

        // Convert the incremental angle to radians per second
        float dt = Time.fixedDeltaTime;
        float desiredAngleRad = scaledAngle * Mathf.Deg2Rad;
        Vector3 desiredAngularVelocity = axis * (desiredAngleRad / dt);

        // Compute error between desired and current angular velocity
        Vector3 relativeAngularVelocity = desiredAngularVelocity - rb.angularVelocity;

        // Compute and apply torque (a simple proportional controller)
        Vector3 torque = relativeAngularVelocity * torqueGain;
        rb.AddTorque(torque, ForceMode.Acceleration);

        oldBasis = newBasis;
    }
}