using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[RequireComponent(typeof(Rigidbody))]
public class RigidbodyVelocityStabilizer : MonoBehaviour
{
    [Tooltip("Number of frames to keep for velocity history")]
    public int historySize = 10;

    private Queue<Vector3> velocityHistory;

    public Rigidbody Rigidbody { get; private set; }
    public Vector3 linearVelocity { get; private set; }

    void Start()
    {
        Rigidbody = GetComponent<Rigidbody>();
        velocityHistory = new Queue<Vector3>(historySize);
    }

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
    }
}
