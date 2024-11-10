using UnityEngine;

public class ForceInteraction : MonoBehaviour
{
    public Transform HandDirectionTransform;
    public Camera XREyes;

    public GameObject InteractionArea;

    public float RayDistance;
    public bool useEyeOferHand = true;

    public void Update()
    {
        Ray ray;
        if (useEyeOferHand)
        {
            ray = new Ray(HandDirectionTransform.position, (HandDirectionTransform.position - XREyes.transform.position).normalized);
        }
        else
        {
            ray = new Ray(HandDirectionTransform.position, HandDirectionTransform.forward);
        }

        RaycastHit hit;
        bool hasHit = Physics.Raycast(ray, out hit, RayDistance);
        if (hasHit)
        {
            InteractionArea.transform.position = hit.point;
        }
    }
}