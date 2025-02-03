using UnityEngine;

public class RotationTester : MonoBehaviour
{
    public Transform source;
    [Range(0f, 1f)]
    public float factor = 0.5f;

    // We’ll store the target’s orientation as a 3×3 matrix each frame:
    private Matrix4x4 oldBasis;

    private void Start()
    {
        oldBasis = Matrix4x4.Rotate(source.rotation);
    }

    private void Update()
    {
        // 1. Build the new orientation matrix for the target
        Matrix4x4 newBasis = Matrix4x4.Rotate(source.rotation);

        // 2. Compute the "difference" matrix: diff = new * old⁻¹
        //    For orthonormal 3×3 rotation matrices, old⁻¹ is oldᵀ (its transpose).
        Matrix4x4 diff = newBasis * oldBasis.transpose;

        // 3. Extract the rotation axis and angle from 'diff'
        diff.rotation.ToAngleAxis(out float angleDegrees, out Vector3 axis);

        // 4. Scale that angle by factor, apply to the follower (in world space)
        float scaledAngle = angleDegrees * factor;
        if (Mathf.Abs(scaledAngle) > 0.0001f && axis.sqrMagnitude > 0.000001f)
        {
            transform.Rotate(axis, scaledAngle, Space.World);
        }

        // 5. Update oldBasis for the next frame
        oldBasis = newBasis;
    }
}
