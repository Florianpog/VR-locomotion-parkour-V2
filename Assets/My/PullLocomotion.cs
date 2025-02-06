using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;

public class PullLocomotion : LocomotionProvider
{
    public Transform LeftHandForceInteractionTransform;
    public Transform RightHandForceInteractionTransform;
    public Camera XREyes;

    public AnimationCurve movementSpeed_vs_handSpeed;

    public float fallingSpeed = 2f; // Exposed falling speed


    public XROriginMovement transformation { get; set; } = new XROriginMovement();

    private CharacterController characterController;
    private bool attemptedGetCharacterController;

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
        Vector3 movement = Vector3.zero;
        for (int i = 0; i < 2; i++)
        {
            bool handIsLeft = i == 0 ? true : false;

            movement += CalculateMovement();
        }

        Vector3 motion = (movement) + (verticalVelocity * Time.deltaTime);
        transformation.motion = motion;
        TryQueueTransformation(transformation);

        TryEndLocomotion();
    }

    private Vector3 CalculateMovement()
    {
        return Vector3.zero;
    }

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
