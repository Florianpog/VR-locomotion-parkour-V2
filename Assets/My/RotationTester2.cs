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
        // 1. Get the source’s current orientation matrix
        Matrix4x4 newBasis = BuildRotationMatrix(source);

        // 2. Compute the difference: diff = newBasis * oldBasisᵀ
        Matrix4x4 diff = newBasis * oldBasis.transpose;

        // 3. Extract rotation axis and angle (in degrees) from diff
        ExtractAxisAngle(diff, out Vector3 axis, out float angleDegrees);

        // 4. Scale the angle and, if valid, compute desired angular velocity
        float scaledAngle = angleDegrees * factor;
        if (Mathf.Abs(scaledAngle) > 0.0001f && axis.sqrMagnitude > 0.000001f)
        {
            // Convert the incremental angle to radians per second
            float dt = Time.fixedDeltaTime;
            float desiredAngleRad = scaledAngle * Mathf.Deg2Rad;
            Vector3 desiredAngularVelocity = axis * (desiredAngleRad / dt);

            // 5. Compute error between desired and current angular velocity
            Vector3 angularError = desiredAngularVelocity - rb.angularVelocity;

            // 6. Compute and apply torque (a simple proportional controller)
            Vector3 torque = angularError * torqueGain;
            rb.AddTorque(torque, ForceMode.Acceleration);
        }

        // 7. Save current orientation for the next frame
        oldBasis = newBasis;
    }

    /// <summary>
    /// Builds a 3×3 rotation matrix (stored in a 4×4 for convenience)
    /// from a Transform’s current right/up/forward vectors.
    /// </summary>
    private Matrix4x4 BuildRotationMatrix(Transform t)
    {
        // We’ll store the basis vectors in columns 0,1,2 of an identity matrix.
        Matrix4x4 m = Matrix4x4.identity;
        m.SetColumn(0, t.right);
        m.SetColumn(1, t.up);
        m.SetColumn(2, t.forward);
        return m;
    }

    /// <summary>
    /// Extracts the axis (normalized) and angle (in degrees) from a 3D rotation matrix.
    /// </summary>
    private void ExtractAxisAngle(Matrix4x4 m, out Vector3 axis, out float angleDegrees)
    {
        // For a rotation matrix R, the angle θ is given by:
        //     cos(θ) = (trace(R) - 1) / 2
        float trace = m[0, 0] + m[1, 1] + m[2, 2];
        float cosTheta = (trace - 1f) * 0.5f;

        // Clamp to avoid floating‐point out‐of‐range errors
        cosTheta = Mathf.Clamp(cosTheta, -1f, 1f);

        // The angle in radians:
        float angleRad = Mathf.Acos(cosTheta);
        angleDegrees = angleRad * Mathf.Rad2Deg;

        // If the angle is extremely small, treat it as zero rotation
        if (Mathf.Abs(angleRad) < 1e-6f)
        {
            axis = Vector3.zero;
            angleDegrees = 0f;
            return;
        }

        // For a rotation matrix R and angle θ:
        //     axis = (1 / (2 sin(θ))) * [ R[2,1] - R[1,2],
        //                                R[0,2] - R[2,0],
        //                                R[1,0] - R[0,1] ]
        float denominator = 2f * Mathf.Sin(angleRad);
        if (Mathf.Abs(denominator) < 1e-6f)
        {
            // If we're near 180° or sin(θ) ~ 0, we can degrade gracefully
            // using a fallback that at least picks a direction
            // (though for angles ~180°, there can be ambiguities).
            axis = new Vector3(
                m[2, 1] - m[1, 2],
                m[0, 2] - m[2, 0],
                m[1, 0] - m[0, 1]
            );
            axis.Normalize();
        }
        else
        {
            axis = new Vector3(
                (m[2, 1] - m[1, 2]) / denominator,
                (m[0, 2] - m[2, 0]) / denominator,
                (m[1, 0] - m[0, 1]) / denominator
            );
        }
    }
}