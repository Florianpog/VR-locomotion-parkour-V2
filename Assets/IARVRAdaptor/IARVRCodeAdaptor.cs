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
    public enum TriggerType
    {
        IndexFinger,
        MiddleFinger
    }

    public static IARVRCodeAdaptor instance;

    public InputActionReference leftIndexTriggerAction;
    public InputActionReference righIndextTriggerAction;
    public InputActionReference leftMiddleTriggerAction;
    public InputActionReference righMiddleTriggerAction;

    public void Awake()
    {
        instance = this;
    }

    public static float GetTriggerValue(ControlerType controlerType, TriggerType triggerType)
    {
#if USING_OVR
        OVRInput.Controller controller = isRightController? OVRInput.Controller.RHand :  OVRInput.Controller.LHand;
        
        if(triggerType == TriggerType.IndexFinger)
            return OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, controller);
        else
            return OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, controller);

#else
        //using XR Toolkit verion
        InputActionReference triggerAction = (triggerType == TriggerType.IndexFinger)? ((controlerType == ControlerType.RightControler)? instance.righIndextTriggerAction : instance.leftIndexTriggerAction) : ((controlerType == ControlerType.RightControler) ? instance.righMiddleTriggerAction : instance.leftMiddleTriggerAction);
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