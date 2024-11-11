using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// TriggerManager tracks colliders inside the trigger area and invokes UnityEvents for each collider in FixedUpdate, OnTriggerEnter, and OnTriggerExit.
/// </summary>
public class TriggerManager : MonoBehaviour
{
    [System.Serializable]
    public class ColliderEvent : UnityEvent<TriggerManager, Collider> { }

    private List<Collider> collidersInside = new List<Collider>();

    /// <summary>
    /// UnityEvent that can be assigned from the Inspector or via code
    /// </summary>
    public ColliderEvent OnColliderFixedUpdate;
    public ColliderEvent OnColliderEnter;
    public ColliderEvent OnColliderExit;

    private void OnTriggerEnter(Collider other)
    {
        if (!collidersInside.Contains(other))
        {
            collidersInside.Add(other);
            OnColliderEnter?.Invoke(this, other);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (collidersInside.Remove(other))
        {
            OnColliderExit?.Invoke(this, other);
        }
    }

    private void FixedUpdate()
    {
        collidersInside.RemoveAll(collider => collider == null || !collider.gameObject.activeInHierarchy);

        foreach (var collider in collidersInside)
        {
            OnColliderFixedUpdate?.Invoke(this, collider);
        }
    }

    private void OnDisable() => collidersInside.Clear();

    private void OnDestroy() => collidersInside.Clear();
}
