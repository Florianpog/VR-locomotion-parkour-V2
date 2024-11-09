using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

public class ScaleUpMovement : MonoBehaviour
{
    public InputActionReference ActivatScalePlayerUp;
    public InputActionReference ActivateScaleSpeedUp;


    public GameObject XROrigin;
    public Camera XRCamera;
    public float ScaleFactor = 5f;

    private bool ScalePlayerUpActive = false;
    private bool ScaleSpeedUpActive = false;
    private Vector3? lastLocalCameraPos = null;

    private void Awake()
    {
        //XRSettings.gameViewRenderMode = GameViewRenderMode.BothEyes;
    }

    public void Start()
    {
        ActivatScalePlayerUp.action.started += (_) => ScalePlayerUp();
        ActivatScalePlayerUp.action.canceled += (_) => UndoScalePlayerUp();

        ActivateScaleSpeedUp.action.started += (_) => ScaleSpeedUp();
        ActivateScaleSpeedUp.action.canceled += (_) => UndoScaleSpeedUp();
    }

    public void ScalePlayerUp()
    {
        ScalePlayerUpActive = true;

        Vector3 cameraPosBeforeChange = XRCamera.transform.position;
        XROrigin.transform.localScale *= ScaleFactor;
        XROrigin.transform.position += cameraPosBeforeChange - XRCamera.transform.position;

        if(ScaleSpeedUpActive)
            UndoScaleSpeedUp();
    }

    public void UndoScalePlayerUp()
    {
        ScalePlayerUpActive = false;

        Vector3 cameraPosBeforeChange = XRCamera.transform.position;
        XROrigin.transform.localScale *= 1f / ScaleFactor;
        XROrigin.transform.position += cameraPosBeforeChange - XRCamera.transform.position;
    }

    public void ScaleSpeedUp()
    {
        ScaleSpeedUpActive = true;

        if(ScalePlayerUpActive)
            UndoScalePlayerUp();
    }

    public void UndoScaleSpeedUp() 
    {
        ScaleSpeedUpActive = false;
    }

    private void LateUpdate()
    {
        if (ScaleSpeedUpActive)
        {
            if (lastLocalCameraPos.HasValue)
            {
                XROrigin.transform.position += (CalcualteLocalCameraPos() - lastLocalCameraPos.Value) * (ScaleFactor - 1); //!!missing transformation back to the localXROriginTransfrom
            }
        }

        lastLocalCameraPos = CalcualteLocalCameraPos();
    }

    private Vector3 CalcualteLocalCameraPos()
    {
        return XROrigin.transform.InverseTransformPoint(XRCamera.transform.position);
    }
}
