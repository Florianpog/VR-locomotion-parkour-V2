using UnityEngine;
using UnityEngine.VFX;

public class ForceInteractionVFXGraph : MonoBehaviour
{
    public VisualEffect vfxGraph;
    public ForceInteractionV2 forceInteraction;

    private Vector3 lastHandPos = Vector3.zero;
    private void Start()
    {
        vfxGraph.SetFloat("fallOffDistance", forceInteraction.baseFallOffDistance);
        vfxGraph.SetFloat("baseMaxForce", forceInteraction.baseForce);
    }

    private void Update()
    {
        Vector3 handPos = forceInteraction.LeftHandForceInteractionTransform.position;

        vfxGraph.SetVector3("eyePos", forceInteraction.XREyes.transform.position);
        vfxGraph.SetVector3("handPos", handPos);
        vfxGraph.SetVector3("lastHandPos", lastHandPos);

        lastHandPos = handPos;
    }
}
