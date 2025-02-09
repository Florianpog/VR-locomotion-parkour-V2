using System.Collections;
using UnityEngine;

public class LightBlinker : MonoBehaviour
{
    public Light lightSource;          // Light component to blink
    public float blinkInterval = 0.5f; // Time between each blink in seconds
    public float blinkDuration = 0.2f; // Duration of each blink
    public bool startBlinkingOnAwake = true;
    private bool isBlinking = false;

    void Awake()
    {
        if (lightSource == null)
        {
            lightSource = GetComponent<Light>();
        }

        if (startBlinkingOnAwake)
        {
            StartBlinking();
        }
    }

    public void StartBlinking()
    {
        if (!isBlinking)
        {
            isBlinking = true;
            StartCoroutine(Blink());
        }
    }

    public void StopBlinking()
    {
        isBlinking = false;
        StopCoroutine(Blink());
        if (lightSource != null) lightSource.enabled = true; // Ensure light stays on when stopped
    }

    IEnumerator Blink()
    {
        while (isBlinking)
        {
            if (lightSource != null)
            {
                lightSource.enabled = false;
                yield return new WaitForSeconds(blinkDuration);
                lightSource.enabled = true;
            }
            yield return new WaitForSeconds(blinkInterval);
        }
    }
}
