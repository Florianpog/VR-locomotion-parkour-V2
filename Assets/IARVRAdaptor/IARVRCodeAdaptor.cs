using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

public class IARVRCodeAdaptor : MonoBehaviour
{
    public enum ControlerType
    {
        LeftControler,
        RightControler
    }

    public static IARVRCodeAdaptor instance;

    public InputActionReference leftTriggerAction;
    public InputActionReference rightTriggerAction;

    public void Awake()
    {
        instance = this;
    }

    public static float GetTriggerValue(ControlerType controlerType)
    {
#if USING_OVR
        OVRInput.Controller controller = isRightController? OVRInput.Controller.RHand :  OVRInput.Controller.LHand;
        
        return OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, controller);

#else
        //using XR Toolkit verion
        InputActionReference triggerAction = (controlerType == ControlerType.RightControler)? instance.rightTriggerAction : instance.leftTriggerAction;
        return triggerAction.action.ReadValue<float>();
#endif
    }

    public static Vector3 GetLocalControllerPosition(GameObject controller)
    {

#if USING_OVR
        OVRInput.GetLocalControllerPosition(leftController)
#else
        return controller.transform.localPosition;
#endif
    }

    public static bool IsRespawnButtonPressed(GameObject controller)
    {
#if USING_OVR
        return OVRInput.Get(OVRInput.Button.Two) || OVRInput.Get(OVRInput.Button.Four);
#else
        /*InputDevice device = InputDevices.GetDeviceAtXRNode(controller.GetComponent<XRNode>());

        if (device.isValid)
        {
            bool buttonTwoPressed = device.TryGetFeatureValue(CommonUsages.secondaryButton, out bool isPressedTwo) && isPressedTwo;
            bool buttonFourPressed = device.TryGetFeatureValue(CommonUsages.menuButton, out bool isPressedFour) && isPressedFour;
            return buttonTwoPressed || buttonFourPressed;
        }*/
        return false;
#endif
    }
}