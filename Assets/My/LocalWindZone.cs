using UnityEngine;

/// <summary>
/// A script to apply local wind forces to objects within a trigger zone.
/// </summary>
public class LocalWindZone : TriggerManager
{
    public float WindStrength = 10f;
    [Tooltip("Wind direction as a vector (does not need to be normalized)")]
    /// <summary>
    /// Wind direction as a vector (does not need to be normalized)
    /// </summary>
    public Vector3 WindDirection;

    [Tooltip("in kg/m^2. on earth its 1.225 at sea level")]
    public float airDensity = 1.225f;


    private void OnValidate()
    {
        try
        {
            OnColliderFixedUpdate.RemoveAllListeners();
            OnColliderFixedUpdate.AddListener((_, collider) => ApplyForce(collider));
        }
        catch (System.Exception)
        {

            throw;
        }
    }

    private void Awake()
    {
        OnValidate();
    }

    private void ApplyForce(Collider objectCollider)
    {
        Rigidbody objectRigidbody = objectCollider.attachedRigidbody; //!!problem with multiple Colliders sharing the same Ridgedbody

        if (objectRigidbody == null)
            return;

        Vector3 windVelocity = WindDirection.normalized * WindStrength;
        Vector3 objectVelocity = objectRigidbody.linearVelocity;
        Vector3 relativeVelocity = objectVelocity - windVelocity;
        float exposedArea = ApproximateExposedArea(objectCollider);
        float dragCoefficient = ApproximateDragCoefficient(objectCollider);

        // Drag Force Formula
        Vector3 dragForce = -0.5f * airDensity * relativeVelocity.sqrMagnitude * dragCoefficient * exposedArea * relativeVelocity.normalized;

        objectRigidbody.AddForce(dragForce);
    }

    /// <summary>
    /// Approximates the exposed area of the collider for drag calculation.
    /// </summary>
    private float ApproximateExposedArea(Collider collider)
    {
        Bounds bounds = collider.bounds;
        Vector3 windDir = WindDirection.normalized;

        // Calculate the projected area based on the bounds and wind direction
        float projectedWidth = Vector3.Dot(bounds.size, new Vector3(Mathf.Abs(windDir.x), 0, 0));
        float projectedHeight = Vector3.Dot(bounds.size, new Vector3(0, Mathf.Abs(windDir.y), 0));
        float projectedDepth = Vector3.Dot(bounds.size, new Vector3(0, 0, Mathf.Abs(windDir.z)));

        // Approximate the exposed area as the maximum projection (facing the wind)
        float exposedAreaMaximumProjection = Mathf.Max(projectedWidth * projectedHeight, projectedWidth * projectedDepth, projectedHeight * projectedDepth);

        // Approximating by using the allways overestimating maximum projection and multiplying with 50%
        return exposedAreaMaximumProjection * 0.5f;
    }

    private float ApproximateDragCoefficient(Collider collider)
    {
        return 1.0f; // A drag coefficient for typical non-streamlined objects (0.47 would be sphere - like)
    }
}
