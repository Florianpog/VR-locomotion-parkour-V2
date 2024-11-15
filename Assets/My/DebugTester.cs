using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class DebugTester : MonoBehaviour
{
    public static DebugCollector<string, float> DebugCollector = new DebugCollector<string, float>();
    public float debugInterval = 0.5f; // Set the interval time in seconds

    private void Start()
    {
        StartCoroutine(DebugCoroutine());
    }

    private System.Collections.IEnumerator DebugCoroutine()
    {
        while (true)
        {
            DebugCollector.SendDebug("dragForce: ", values => Mathf.Max(values.ToArray()));
            yield return new WaitForSeconds(debugInterval);
        }
    }
    private void FixedUpdate()
    {
        //DebugCollector.SendDebug("dragForce: ", values => Mathf.Max(values.ToArray()));
    }
}
