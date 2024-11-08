using UnityEngine;

public class LocomotionTechnique : MonoBehaviour
{
    // Please implement your locomotion technique in this script. 
    public GameObject leftController;
    public GameObject rightController;
    [Range(0, 10)] public float translationGain = 0.5f;
    public GameObject hmd;
    public GameObject vrOrigin;
    [SerializeField] private float leftTriggerValue;    
    [SerializeField] private float rightTriggerValue;
    [SerializeField] private Vector3 startPos;
    [SerializeField] private Vector3 offset;
    [SerializeField] private bool isIndexTriggerDown;

    [SerializeField]
    private ParkourCounter parkourCounter;

    /////////////////////////////////////////////////////////
    // These are for the game mechanism.
    void Start()
    {
        
    }

    void Update()
    {
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        // Please implement your LOCOMOTION TECHNIQUE in this script :D.
        leftTriggerValue = IARVRCodeAdaptor.GetTriggerValue(IARVRCodeAdaptor.ControlerType.LeftControler, IARVRCodeAdaptor.TriggerType.IndexFinger);
        rightTriggerValue = IARVRCodeAdaptor.GetTriggerValue(IARVRCodeAdaptor.ControlerType.RightControler, IARVRCodeAdaptor.TriggerType.IndexFinger); 

        if (leftTriggerValue > 0.95f && rightTriggerValue > 0.95f)
        {
            if (!isIndexTriggerDown)
            {
                isIndexTriggerDown = true;
                startPos = (IARVRCodeAdaptor.GetLocalControllerPosition(leftController) + IARVRCodeAdaptor.GetLocalControllerPosition(rightController)) / 2;
            }
            offset = hmd.transform.forward.normalized *
                    (IARVRCodeAdaptor.GetLocalControllerPosition(leftController) - startPos +
                    (IARVRCodeAdaptor.GetLocalControllerPosition(rightController) - startPos)).magnitude;
            Debug.DrawRay(startPos, offset, Color.red, 0.2f);
        }
        else if (leftTriggerValue > 0.95f && rightTriggerValue < 0.95f)
        {
            if (!isIndexTriggerDown)
            {
                isIndexTriggerDown = true;
                startPos = IARVRCodeAdaptor.GetLocalControllerPosition(leftController);
            }
            offset = hmd.transform.forward.normalized *
                     (IARVRCodeAdaptor.GetLocalControllerPosition(leftController) - startPos).magnitude;
            Debug.DrawRay(startPos, offset, Color.red, 0.2f);
        }
        else if (leftTriggerValue < 0.95f && rightTriggerValue > 0.95f)
        {
            if (!isIndexTriggerDown)
            {
                isIndexTriggerDown = true;
                startPos = IARVRCodeAdaptor.GetLocalControllerPosition(rightController);
            }
           offset = hmd.transform.forward.normalized *
                    (IARVRCodeAdaptor.GetLocalControllerPosition(rightController) - startPos).magnitude;
            Debug.DrawRay(startPos, offset, Color.red, 0.2f);
        }
        else
        {
            if (isIndexTriggerDown)
            {
                isIndexTriggerDown = false;
                offset = Vector3.zero;
            }
        }
        vrOrigin.transform.position = vrOrigin.transform.position + offset * translationGain;


        ////////////////////////////////////////////////////////////////////////////////
        // These are for the game mechanism.
        if (IARVRCodeAdaptor.IsRespawnButtonPressed(leftController))
        {
            if (parkourCounter.parkourStart)
            {
                vrOrigin.transform.position = parkourCounter.currentRespawnPos;
            }
        }
    }
}