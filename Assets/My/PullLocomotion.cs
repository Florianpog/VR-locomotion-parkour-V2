using System;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using System.Linq;

public class PullLocomotion : LocomotionProvider
{
    [Tooltip("This Transform should be a child of the force interaction hand. The forward direction should be the way the hand is facting and the position the hand center")]
    /// <summary>
    /// This Transform should be a child of the force interaction hand. The forward direction should be the way the hand is facting and the position the hand center
    /// </summary>
    public HandData<Transform> HandsHandForceInteractionTransform;
    public HandData<InputActionReference> HandsPullAction;
    public Camera XREyes;

    public float baseMovementSpeed = 5.0f;
    public AnimationCurve MovementFactor_vs_handSpeed;
    public AnimationCurve MovementFactor_vs_alignmentAngle;
    [Tooltip("Only hand movement that has happend in the past number of seconds defined here is used for stabailing the current hand movement")]
    public float durationOfRelevantPastHandMovement;
    [Tooltip("the persentage of using a direction that alligns with the ground instead of the real movement direction based on the angle between the two (if we read very little movement into the air, we want to disregard that)")]
    public AnimationCurve groundOverride_vs_groundAngle;

    public float fallingSpeed = 1f;

    public XROriginMovement transformation { get; set; } = new XROriginMovement();

    public Transform DebugObject; 

    private HandData<MyQueue<Tuple<Vector3, float>>> handsSavedLastHandLocalPos = new HandData<MyQueue<Tuple<Vector3, float>>>(new MyQueue<Tuple<Vector3, float>>(), new MyQueue<Tuple<Vector3, float>>()); //last pos is saved as local postion, because the player can move after the frame which will require transforming the pos to the new parent pos
    private HandData<bool> handsIsPullingAction = new HandData<bool>(false, false);

    private CharacterController characterController;
    private bool attemptedGetCharacterController;

    private void Start(){
        
        HandsPullAction.Left.action.started += (_) => StartPullAction(true);
        HandsPullAction.Right.action.started += (_) => StartPullAction(false);

        HandsPullAction.Left.action.canceled += (_) => StopPullAction(true);
        HandsPullAction.Right.action.canceled += (_) => StopPullAction(false);

    }

    private void Update()
    {
        var xrOrigin = mediator.xrOrigin?.Origin;
        if (xrOrigin == null)
            return;

        FindCharacterController();
        Vector3 verticalVelocity = Vector3.zero;
        if (characterController != null && characterController.enabled)
        {
            if (!characterController.isGrounded)
            {
                verticalVelocity += Vector3.down * fallingSpeed * Time.deltaTime;
            }
        }

        TryStartLocomotionImmediately();
        if (locomotionState != LocomotionState.Moving)
            return;

        //Actual Movment Caluclation
        Vector3 eyesDir = XREyes.transform.forward;

        Vector3 movement = Vector3.zero;
        for (int i = 0; i < 2; i++)
        {
            bool handIsLeft = i == 0 ? true : false;

            ref Transform handForceInteractionTransform = ref (handIsLeft ? ref HandsHandForceInteractionTransform.Left : ref HandsHandForceInteractionTransform.Right);
            ref MyQueue<Tuple<Vector3, float>> savedLastHandLocalPos = ref (handIsLeft ? ref handsSavedLastHandLocalPos.Left : ref handsSavedLastHandLocalPos.Right);
            ref bool isPullingAction = ref (handIsLeft ? ref handsIsPullingAction.Left : ref handsIsPullingAction.Right);

            if(isPullingAction)
            {
                Vector3 handPos = handForceInteractionTransform.position;
                savedLastHandLocalPos.Enqueue(new Tuple<Vector3, float>(xrOrigin.transform.InverseTransformPoint(handPos), Time.deltaTime));
                while(savedLastHandLocalPos.Count > 0 && savedLastHandLocalPos.Sum(t => t.Item2) > durationOfRelevantPastHandMovement)
                    savedLastHandLocalPos.Dequeue();

                Vector3 stabalizedLocalHandVelocity = (savedLastHandLocalPos.Count > 0)? (savedLastHandLocalPos.Last().Item1 - savedLastHandLocalPos.First().Item1) / savedLastHandLocalPos.Sum(t => t.Item2) : Vector3.zero;
                Vector3 stabalizedHandVelocity = xrOrigin.transform.TransformDirection(stabalizedLocalHandVelocity).normalized * stabalizedLocalHandVelocity.magnitude;
                float factorFromHandSpeed = MovementFactor_vs_handSpeed.Evaluate(stabalizedHandVelocity.magnitude);

                //if (!handIsLeft)
                //    DebugObject.localPosition = new Vector3(DebugObject.localPosition.x, stabalizedHandVelocity.magnitude, DebugObject.localPosition.z);

                float handMovementAlignmentAngle = Vector3.Angle(-eyesDir, stabalizedHandVelocity);
                float factorFromAngle = MovementFactor_vs_alignmentAngle.Evaluate(handMovementAlignmentAngle);

                if (!handIsLeft)
                    DebugObject.localPosition = new Vector3(DebugObject.localPosition.x, handMovementAlignmentAngle / 90f, DebugObject.localPosition.z);

                Vector3 rawMovmentDir = eyesDir;
                Vector3 groundAngle = (new Vector3(rawMovmentDir.x, 0, rawMovmentDir.z).normalized); //!!!should be improved by raycast ground checks
                float groundAlignmentAngle = Vector3.Angle(rawMovmentDir, groundAngle);
                Vector3 movmentDir = Vector3.Lerp(rawMovmentDir, groundAngle, groundOverride_vs_groundAngle.Evaluate(groundAlignmentAngle));

                Vector3 newMovement = movmentDir * (baseMovementSpeed * factorFromAngle * factorFromHandSpeed);

                movement += newMovement;
            }
        }

        Vector3 motion = movement + verticalVelocity;
        transformation.motion = motion;
        TryQueueTransformation(transformation);

        TryEndLocomotion();
    }

    
    private void StartPullAction(bool handIsLeft){
        ref InputActionReference pullAction = ref (handIsLeft ? ref HandsPullAction.Left : ref HandsPullAction.Right);
        ref bool isPullingAction = ref (handIsLeft ? ref handsIsPullingAction.Left : ref handsIsPullingAction.Right);
        isPullingAction = true;

    }

    private void StopPullAction(bool handIsLeft){
        ref InputActionReference pullAction = ref (handIsLeft ? ref HandsPullAction.Left : ref HandsPullAction.Right);
        ref bool isPullingAction = ref (handIsLeft ? ref handsIsPullingAction.Left : ref handsIsPullingAction.Right);
        isPullingAction = false;
    }


    /*private Vector3 CalculateMovement()
    {
        return Vector3.zero;
    }*/

    private void FindCharacterController()
    {
        if (characterController == null && !attemptedGetCharacterController)
        {
            var xrOrigin = mediator.xrOrigin?.Origin;
            if (xrOrigin == null)
                return;

            if (!xrOrigin.TryGetComponent(out characterController) && xrOrigin != mediator.xrOrigin.gameObject)
                mediator.xrOrigin.TryGetComponent(out characterController);

            attemptedGetCharacterController = true;
        }
    }
}
