using UnityEngine;

/// <summary>
/// A script to apply local wind forces to objects within a trigger zone.
/// </summary>
public class LocalWindZone : TriggerManager
{
    public float WindStrength = 10f;

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
        Rigidbody objectRigidbody = objectCollider.gameObject.GetComponent<Rigidbody>(); //!!problem with multiple Colliders sharing the same Ridgedbody

        if (objectRigidbody == null)
            return;

        Vector3 windVelocity = transform.forward.normalized * WindStrength;
        Vector3 objectVelocity = objectRigidbody.linearVelocity;
        Vector3 relativeVelocity = objectVelocity - windVelocity;
        float exposedArea = ApproximateExposedArea(objectCollider);
        float dragCoefficient = ApproximateDragCoefficient(objectCollider);

        // Drag Force Formula
        Vector3 dragForce = -0.5f * airDensity * relativeVelocity.sqrMagnitude * dragCoefficient * exposedArea * relativeVelocity.normalized;

        Debug.Log($"dragForce: {dragForce.magnitude.ToReadableFormat()}");

        objectRigidbody.AddForce(dragForce);
    }

    /// <summary>
    /// Approximates the exposed area of the collider for drag calculation.
    /// </summary>
    private float ApproximateExposedArea(Collider collider)
    {
        Bounds bounds = collider.bounds;
        Vector3 windDir = transform.forward.normalized;

        // Simplified approach: use cross-sectional area perpendicular to the wind direction
        float projectedArea = Mathf.Abs(Vector3.Dot(bounds.size, windDir));
        return projectedArea;
    }

    private float ApproximateDragCoefficient(Collider collider)
    {
        return 1.0f; // A drag coefficient for typical non-streamlined objects (0.47 would be sphere - like)
    }
}
