using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;

public class VibrationTest1 : MonoBehaviour
{
    public bool isOn = false;
    [Range(0,1)]
    public float amplitude = 0.5f;
    [Range(0,40000)]
    public float frequency = 0f;
    public int index = 0;

    private void Update()
    {
        HapticsUtility.SendHapticImpulse(amplitude, duration: 1.0f, HapticsUtility.Controller.Right, frequency);
    }
}
