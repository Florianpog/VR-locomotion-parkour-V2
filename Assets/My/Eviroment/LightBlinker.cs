using UnityEngine;

public class BlinkLight : MonoBehaviour
{
    public float onDuration = 0.2f;  // Duration the light stays on
    public float offDuration = 0.5f; // Duration the light stays off
    public bool startOn = true;      // Should the light start as on

    public Light Light;
    private float timer;

    void Awake()
    {
        if (Light == null)
        {
            Debug.LogError("No Light component found on this GameObject.");
            enabled = false;
        }
    }

    void Start()
    {
        Light.enabled = startOn;
        timer = 0f;
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (Light.enabled && timer >= onDuration)
        {
            Light.enabled = false;
            timer = 0f;
        }
        else if (!Light.enabled && timer >= offDuration)
        {
            Light.enabled = true;
            timer = 0f;
        }
    }
}