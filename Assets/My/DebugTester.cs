using System.Linq;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class DebugTester : MonoBehaviour
{
    public static DebugCollector<string, float> stringFloatLogger = new DebugCollector<string, float>();
    public float debugInterval = 0.5f; // Set the interval time in seconds

    private void Start()
    {
        StartCoroutine(DebugCoroutine());
    }

    private System.Collections.IEnumerator DebugCoroutine()
    {
        while (true)
        {
            stringFloatLogger.SendDebug(values => Mathf.Max(values.ToArray()));
            //stringFloatLogger.SendDebug(values => values.Average());
            yield return new WaitForSeconds(debugInterval);
        }
    }
    private void FixedUpdate()
    {
        //DebugCollector.SendDebug("dragForce: ", values => Mathf.Max(values.ToArray()));
    }
}
